namespace Paperless.REST.BLL.Uploads.Models
{
    /// <summary>
    /// Validation result for uploaded files and metadata
    /// </summary>
    public sealed class UploadValidationResult
    {
        public List<string> Errors { get; } = new();
        public bool Success => Errors.Count == 0;   // is true íf Errors is empty
        public int AcceptedCount { get; set; } = 0;
        public List<Guid>? DocumentIds { get; set; }
    }
}
