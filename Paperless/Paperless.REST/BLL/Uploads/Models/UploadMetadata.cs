namespace Paperless.REST.BLL.Uploads.Models
{
    /// <summary>
    /// Represents metadata associated with an upload.
    /// </summary>
    public sealed class UploadMetadata
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? LanguageCode { get; set; }
    }

    /// <summary>
    /// Represents metadata for multiple files upload.
    /// </summary>
    public sealed class UploadMultiMetadata
    {
        public Dictionary<string, UploadMetadata> Files { get; set; } = new();
    }

}
