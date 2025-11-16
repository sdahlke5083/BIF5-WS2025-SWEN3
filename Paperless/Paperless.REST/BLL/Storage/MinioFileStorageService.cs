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
    }
}
