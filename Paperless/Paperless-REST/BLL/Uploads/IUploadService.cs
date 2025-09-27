using BLL.Uploads.Models;

namespace BLL.Uploads
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
    }
}
