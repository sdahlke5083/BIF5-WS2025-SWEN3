using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Paperless.REST.BLL.Storage
{
    public sealed class MinioFileStorage : IFileStorageService
    {
        private readonly IMinioClient _minio;
        private readonly string _bucket;

        /// <summary>
        /// Create a MinioFileStorage.
        /// </summary>
        /// <param name="endpoint">MinIO endpoint (host:port).</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="bucket">Bucket name to use.</param>
        /// <param name="useSsl">Use SSL (true) or plain (false).</param>
        public MinioFileStorage(string endpoint, string accessKey, string secretKey, string bucket, bool useSsl = false)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("endpoint required", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(accessKey)) throw new ArgumentException("accessKey required", nameof(accessKey));
            if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentException("secretKey required", nameof(secretKey));
            if (string.IsNullOrWhiteSpace(bucket)) throw new ArgumentException("bucket required", nameof(bucket));

            _bucket = bucket;
            _minio = new MinioClient()
                        .WithEndpoint(endpoint)
                        .WithCredentials(accessKey, secretKey)
                        .WithSSL(useSsl)
                        .Build();
        }

        public async Task<string> SaveFileAsync(string objectName, Stream content, long size, string? contentType = null, CancellationToken cancellationToken = default)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("objectName required", nameof(objectName));

            // Ensure bucket exists
            try
            {
                var existsArgs = new BucketExistsArgs().WithBucket(_bucket);
                bool exists = await _minio.BucketExistsAsync(existsArgs, cancellationToken).ConfigureAwait(false);
                if (!exists)
                {
                    var makeArgs = new MakeBucketArgs().WithBucket(_bucket);
                    await _minio.MakeBucketAsync(makeArgs, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // wrap useful info
                throw new Exception($"Failed to ensure bucket '{_bucket}' exists: {ex.Message}", ex);
            }

            // Ensure stream position
            if (content.CanSeek)
            {
                try { content.Position = 0; } catch { /* ignore */ }
            }

            try
            {
                var putArgs = new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectName)
                    .WithStreamData(content)
                    .WithObjectSize(size);

                if (!string.IsNullOrWhiteSpace(contentType))
                    putArgs = putArgs.WithContentType(contentType);

                await _minio.PutObjectAsync(putArgs, cancellationToken).ConfigureAwait(false);

                // Return the key we used (could be later stored in DB)
                return objectName;
            }
            catch (MinioException mex)
            {
                throw new Exception($"MinIO put object failed: {mex.Message}", mex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload '{objectName}' to MinIO: {ex.Message}", ex);
            }
        }
    }
}