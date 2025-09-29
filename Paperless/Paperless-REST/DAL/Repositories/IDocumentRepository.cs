using Paperless.REST.DAL.Models;

namespace Paperless.REST.DAL.Repositories
{
    public interface IDocumentRepository
    {
        Task<Guid> CreateWithMetadataAsync(Document doc, DocumentMetadata meta, CancellationToken ct = default);
    }
}
