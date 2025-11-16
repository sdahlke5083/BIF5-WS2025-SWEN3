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
    }
}