using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Paperless.Worker.OCR.Connectors
{
    // Einfacher Stub. Ersetzen Sie diesen durch echte MinIO-Interaktion.
    public class MinioConnector
    {
        private readonly IMinioClient _client;
        private readonly MinioStorageOptions _options;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public MinioConnector(
            IOptions<MinioStorageOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _client = new MinioClient()
               .WithEndpoint(_options.Endpoint)
               .WithCredentials(_options.AccessKey, _options.SecretKey)
               .WithSSL(_options.UseSSL)
               .Build();
        }

        // Erwartet Bucket-Name und Objekt-Schlüssel und liefert die rohen Bytes zurück.
        public async Task<byte[]> FetchObjectAsync(string objectKey)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Object key darf nicht leer sein.", nameof(objectKey));

            _logger.Debug("Versuche, Objekt aus MinIO zu holen. Endpoint={0}, Bucket={1}, ObjectKey={2}", _options.Endpoint, _options.BucketName, objectKey);

            using var ms = new MemoryStream();
            try
            {
                // verify object exists:
                StatObjectArgs statArgs = new StatObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey);
                await _client.StatObjectAsync(statArgs);

                GetObjectArgs getArgs = new GetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(ms);
                    });
                await _client.GetObjectAsync(getArgs);

                _logger.Debug("Erfolgreich Objekt heruntergeladen: {0}/{1}", _options.BucketName, objectKey);
                return ms.ToArray();
            }
            catch (ObjectNotFoundException onf)
            {
                // Eindeutliches Logging, damit die Fehlersuche leichter fällt (RequestId ist oft in onf.Message enthalten).
                _logger.Warn(onf, " Objekt nicht gefunden: Bucket='{0}', Object='{1}'. Exception message: {2}", _options.BucketName, objectKey, onf.Message);
                throw new FileNotFoundException($"Object '{objectKey}' not found in bucket '{_options.BucketName}'. See inner exception for details.", onf);
            }
            catch (MinioException mex)
            {
                _logger.Error(mex, $"Fehler beim Abrufen des Objekts '{objectKey}'.");
                throw new InvalidOperationException($"Fehler beim Abrufen des Objekts '{objectKey}': {mex.Message}", mex);
            }
        }

        public async Task SaveObjectAsync(string objectKey, byte[] content, string contentType = "application/octet-stream")
        {
            if (content is null || content.Length == 0)
                throw new ArgumentException("content must not be empty.", nameof(content));

            // Ensure bucket exists
            var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_options.BucketName));
            if (!bucketExists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.BucketName));
            }

            using var ms = new MemoryStream(content);

            var putArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectKey)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType(contentType);

            await _client.PutObjectAsync(putArgs);
        }

    }
}