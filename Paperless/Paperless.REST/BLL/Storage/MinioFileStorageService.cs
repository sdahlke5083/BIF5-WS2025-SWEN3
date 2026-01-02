using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Paperless.REST.BLL.Exceptions;
using Paperless.REST.BLL.Worker;

namespace Paperless.REST.BLL.Storage
{
    /// <summary>
    /// FileStorage-Implementierung, die Dateien in einem MinIO-Bucket speichert.
    /// </summary>
    public sealed class MinioFileStorageService : IFileStorageService
    {
        private readonly IMinioClient _client;
        private readonly MinioStorageOptions _options;
        private readonly ILogger<MinioFileStorageService> _logger;
        private readonly IDocumentEventPublisher _documentEventPublisher;

        public MinioFileStorageService(
            IOptions<MinioStorageOptions> options,
            ILogger<MinioFileStorageService> logger, IDocumentEventPublisher documentEventPublisher)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            _options = options.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentEventPublisher = documentEventPublisher;

            _client = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.UseSSL)
                .Build();
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
                return await _client.BucketExistsAsync(bucketExistsArgs, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        public async Task<Stream> OpenReadStreamAsync(string objectName, CancellationToken cancellationToken = default)
        {
            // MinIO client supports GetObject with a callback; create a PipeStream-like behavior
            // For simplicity, reuse GetFileAsync which returns a MemoryStream, but this is a place
            // to implement streaming without full buffering.
            return await GetFileAsync(objectName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> SaveFileAsync(
            string objectName,
            Stream content,
            long size,
            string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name must not be empty.", nameof(objectName));

            if (content is null)
                throw new ArgumentNullException(nameof(content));

            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_options.BucketName);

                var exists = await _client
                    .BucketExistsAsync(bucketExistsArgs, cancellationToken)
                    .ConfigureAwait(false);

                if (!exists)
                {
                    var mkBucketArgs = new MakeBucketArgs()
                        .WithBucket(_options.BucketName);

                    await _client
                        .MakeBucketAsync(mkBucketArgs, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (content.CanSeek)
                    content.Position = 0;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(content)
                    .WithObjectSize(size)
                    .WithContentType(contentType ?? "application/octet-stream");

                await _client
                    .PutObjectAsync(putObjectArgs, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Uploaded object {ObjectName} ({Size} bytes) to bucket {Bucket}.",
                    objectName, size, _options.BucketName);

                // Wir geben den objectName zurück – der ist der "Key" im Object Store.
                
                // fire up rabbitmq event to process the document
                _documentEventPublisher.PublishDocumentUploadedAsync(objectName).GetAwaiter().GetResult();
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to upload object {ObjectName} to bucket {Bucket}.",
                    objectName, _options.BucketName);

                // Auf BLL-Ebene in FileStorageException mappen
                throw new FileStorageException(
                    $"Failed to upload file '{objectName}' to object storage.", ex);
            }
        }

        
        public async Task<Stream> GetFileAsync(string objectName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name must not be empty.", nameof(objectName));

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName);

            var memory = new MemoryStream();

            try
            {
                // Use dynamic invocation to handle different MinIO client overloads across versions.
                dynamic dyn = _client;
                object taskObj;

                try
                {
                    // try overload with callback and cancellation token
                    taskObj = dyn.GetObjectAsync(getObjectArgs, (Action<Stream>)(s => s.CopyTo(memory)), cancellationToken);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    // fallback to overload without cancellation token
                    taskObj = dyn.GetObjectAsync(getObjectArgs, (Action<Stream>)(s => s.CopyTo(memory)));
                }

                if (taskObj is Task t)
                    await t.ConfigureAwait(false);
                else
                    throw new FileStorageException("Unexpected result from MinIO client GetObjectAsync.");

                memory.Position = 0;
                return memory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve object {ObjectName} from bucket {Bucket}.", objectName, _options.BucketName);
                throw new FileStorageException($"Failed to retrieve file '{objectName}' from object storage.", ex);
            }
        }

        public async Task DeleteFileAsync(string objectName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name must not be empty.", nameof(objectName));

            try
            {
                var removeArgs = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName);

                await _client.RemoveObjectAsync(removeArgs, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object {ObjectName} from bucket {Bucket}.", objectName, _options.BucketName);
                throw new FileStorageException($"Failed to delete file '{objectName}' from object storage.", ex);
            }
        }
    }
}
