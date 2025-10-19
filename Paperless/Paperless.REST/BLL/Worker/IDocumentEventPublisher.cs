namespace Paperless.REST.BLL.Worker
{
    /// <summary>
    /// Defines a contract for publishing events related to document uploads.
    /// </summary>
    public interface IDocumentEventPublisher
    {
        /// <summary>
        /// Publishes an event indicating that a document has been uploaded.
        /// </summary>
        /// <param name="documentId">The unique identifier of the uploaded document.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishDocumentUploadedAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
