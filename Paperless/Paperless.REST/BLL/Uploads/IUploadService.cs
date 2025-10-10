using Paperless.REST.BLL.Uploads.Models;

namespace Paperless.REST.BLL.Uploads
{
    /// <summary>
    /// Buissness logic for validating uploaded files and metadata
    /// </summary>
    public interface IUploadService
    {
        Task<UploadValidationResult> ValidateAsync(
            IReadOnlyCollection<UploadFile> files,  // files to be uploaded
            string? metadataRaw, 
            CancellationToken cancelToken = default);   // optional cancellation token
        public string Path { get; set; }    // file path
    }
}
