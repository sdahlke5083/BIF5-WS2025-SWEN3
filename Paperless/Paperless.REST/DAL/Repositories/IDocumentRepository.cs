using Paperless.REST.DAL.Models;

namespace Paperless.REST.DAL.Repositories
{
    public interface IDocumentRepository
    {
        Task<Guid> CreateWithMetadataAsync(Document doc, DocumentMetadata meta, CancellationToken ct = default);
        Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<Document>> GetAllActiveAsync(CancellationToken ct = default);
        Task<List<Document>> GetAllDeleted(CancellationToken ct = default);
        Task UpdateAsync(Document doc, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task RestoreAsync(Guid id, CancellationToken ct = default);
        Task PermanentlyDeleteAsync(Guid id, CancellationToken ct = default);
        Task<string?> GetSharePasswordAsync(Guid documentId);

        // TODO: Idea: Filter management methods - Apply some filters to all queries in this repository
        // somehow pass the filter or have a public property that can be set from outside and the functions activate it
        // e.g. only return documents from a specific workspace or only non-deleted documents
        //Task ApplyFilter (CancellationToken ct = default);
        //Task RemoveFilter (CancellationToken ct = default);
        //Task ClearFilters (CancellationToken ct = default);
    }
}
