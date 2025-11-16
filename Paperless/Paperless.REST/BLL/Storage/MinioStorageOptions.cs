using System;

namespace Paperless.REST.BLL.Storage
{
    /// <summary>
    /// Konfigurationsoptionen für den MinIO File Storage.
    /// Werte kommen aus appsettings.json (Paperless:Storage:Minio).
    /// </summary>
    public sealed class MinioStorageOptions
    {
        public string Endpoint { get; init; } = "paperless-minio:9000";
        public string AccessKey { get; init; } = "minioadmin";
        public string SecretKey { get; init; } = "minioadmin";
        public string BucketName { get; init; } = "paperless-files";
        public bool UseSSL { get; init; } = false;
    }
}
