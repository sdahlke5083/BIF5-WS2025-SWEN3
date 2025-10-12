using Paperless.REST.BLL.Uploads.Models;

namespace Paperless.REST.BLL.Uploads
{
    /// <summary>
    /// Buissness logic for validating uploaded files and metadata
    /// </summary>
    public interface IUploadService
    {
        /// <summary>
        /// Gets or sets the store path.
        /// </summary>
        public string Path { get; set; }    // file path

        /// <summary>
        /// Validates the provided files and metadata for upload.
        /// </summary>
        /// <remarks>This method performs validation on the provided files and metadata to ensure they
        /// meet the requirements for upload. If the validation fails, the returned <see cref="UploadValidationResult"/>
        /// will contain details about the issues encountered.</remarks>
        /// <param name="files">A collection of files to be validated. Each file in the collection must meet the upload requirements.</param>
        /// <param name="metadataRaw">An optional raw metadata string to be validated. Can be <see langword="null"/> if no metadata is provided.</param>
        /// <param name="cancelToken">An optional token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains an <see
        /// cref="UploadValidationResult"/> indicating the outcome of the validation, including any errors or warnings.</returns>
        Task<UploadValidationResult> ValidateAsync(
            IReadOnlyCollection<UploadFile> files,  // files to be uploaded
            string? metadataRaw, 
            CancellationToken cancelToken = default);   // optional cancellation token

        /// <summary>
        /// Saves the specified collection of files asynchronously, with optional metadata and cancellation support.
        /// </summary>
        /// <remarks>This method performs the save operation asynchronously. If the operation is canceled
        /// via the <paramref name="cancelToken"/>, the returned task will be in the <see cref="TaskStatus.Canceled"/>
        /// state.</remarks>
        /// <param name="files">A collection of files to be saved. The collection must not be null or empty.</param>
        /// <param name="metadataRaw">Optional raw metadata associated with the files. Can be null if no metadata is provided.</param>
        /// <param name="cancelToken">A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/> if not
        /// provided.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the files
        /// were saved successfully; otherwise, <see langword="false"/>.</returns>
        Task<bool> SaveFilesAsync(
            IReadOnlyCollection<UploadFile> files,  // files to be uploaded
            string? metadataRaw, 
            CancellationToken cancelToken = default);   // optional cancellation token
    }
}
