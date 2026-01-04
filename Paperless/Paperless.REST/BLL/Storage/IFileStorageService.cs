namespace Paperless.REST.BLL.Storage
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Save a stream to storage.
        /// </summary>
        /// <param name="objectName">Object name to store (e.g. filename or key).</param>
        /// <param name="content">Stream with content. Caller should set Position if required.</param>
        /// <param name="size">Length of the stream in bytes.</param>
        /// <param name="contentType">MIME type (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The object identifier (e.g. key) used to store the file.</returns>
        Task<string> SaveFileAsync(string objectName, Stream content, long size, string? contentType = null, CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieve a file stream from storage.
        /// Caller is responsible for disposing the returned stream.
        /// </summary>
        Task<Stream> GetFileAsync(string objectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete an object from storage.
        /// </summary>
        Task DeleteFileAsync(string objectName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to open a read-only stream from storage without copying entire file into memory.
        /// If the underlying client doesn't support streaming ranges, falls back to GetFileAsync.
        /// Caller must dispose the stream.
        /// </summary>
        Task<Stream> OpenReadStreamAsync(string objectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Test connectivity to the underlying storage (e.g., bucket reachable).
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
}