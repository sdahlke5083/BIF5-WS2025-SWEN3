using Microsoft.EntityFrameworkCore;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Exceptions;
using Paperless.REST.DAL.Models;

namespace Paperless.REST.DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly PostgressDbContext _context;
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public DocumentRepository(PostgressDbContext context)
        {
            _context = (PostgressDbContext)context;
        }

        public async Task<Guid> CreateWithMetadataAsync(Document doc, DocumentMetadata meta, CancellationToken ct = default)
        {
            //defaults
            var version = 1;

            // set file and metadata version to 1 if not set
            doc.CurrentMetadataVersion = doc.CurrentMetadataVersion == 0 ? version : doc.CurrentMetadataVersion;
            doc.CurrentFileVersion = doc.CurrentFileVersion == 0 ? version : doc.CurrentFileVersion;

            // start transaction
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            if (transaction is null)
            {
                _logger.Error("Failed to start database transaction for creating document with metadata.");
                throw new DataAccessException("Failed to start database transaction for creating document with metadata.");
            }

            // add document
            await _context.Documents.AddAsync(doc, ct);
            await _context.SaveChangesAsync(ct);

            if(doc.Id == Guid.Empty)
            {
                _logger.Error("Document ID is empty after adding document to database.");
                throw new DataAccessException("Document ID is empty after adding document to database.");
            }

            // add metadata
            meta.DocumentId = doc.Id;
            meta.Version = version;
            await _context.DocumentMetadatas.AddAsync(meta, ct);

            // set processing status
            //var processingStatus = new ProcessingStatus
            //{
            //    DocumentId = doc.Id,
            //    Index = ProcessingState.NotProcessed,
            //    Ocr = ProcessingState.NotProcessed,
            //    Summary = ProcessingState.NotProcessed
            //};
            //await _context.ProcessingStatuses.AddAsync(processingStatus, ct);

            // save all
            try
            {
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error occurred while creating document with metadata.");
                await transaction.RollbackAsync(ct);
                throw new DataAccessException("Error occurred while creating document with metadata.", ex);
            }

            return doc.Id;
        }

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Documents
                .Include(d => d.ProcessingStatus)
                .Include(d => d.MetadataVersions)
                .Include(d => d.FileVersions)
                .Include(d => d.Summaries)
                .Include(d => d.DocumentTags).ThenInclude(dt => dt.Tag)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, ct);
        }

        public Task<List<Document>> GetAllActiveAsync(CancellationToken ct = default)
        {
            return _context.Documents
                .Where(d => d.DeletedAt == null)
                .Include(d => d.ProcessingStatus)
                .Include(d => d.MetadataVersions)
                .Include(d => d.FileVersions)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public Task<List<Document>> GetAllDeleted(CancellationToken ct = default)
        {
            return _context.Documents
                .Where(d => d.DeletedAt != null)
                .Include(d => d.ProcessingStatus)
                .Include(d => d.MetadataVersions)
                .Include(d => d.FileVersions)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public Task UpdateAsync(Document doc, CancellationToken ct = default)
        {
            if (doc is null) throw new ArgumentNullException(nameof(doc));
            _context.Documents.Update(doc);
            return _context.SaveChangesAsync(ct);
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            return SoftDeleteAsync(id, ct);
        }

        public Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            return RestoreInternalAsync(id, ct);
        }

        public Task PermanentlyDeleteAsync(Guid id, CancellationToken ct = default)
        {
            return PermanentlyDeleteInternalAsync(id, ct);
        }

        private async Task SoftDeleteAsync(Guid id, CancellationToken ct)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);
            if (doc is null)
                throw new DataAccessException($"Document with id {id} not found.");

            if (doc.DeletedAt != null)
                return; // already deleted

            doc.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private async Task RestoreInternalAsync(Guid id, CancellationToken ct)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);
            if (doc is null)
                throw new DataAccessException($"Document with id {id} not found.");

            if (doc.DeletedAt == null)
                return; // not deleted

            doc.DeletedAt = null;
            doc.DeletedByUserId = null;
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private async Task PermanentlyDeleteInternalAsync(Guid id, CancellationToken ct)
        {
            var doc = await _context.Documents
                .Include(d => d.MetadataVersions)
                .Include(d => d.FileVersions)
                .Include(d => d.DocumentTags)
                .FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);

            if (doc is null)
                throw new DataAccessException($"Document with id {id} not found.");

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously retrieves the password hash associated with the specified shared document.
        /// </summary>
        /// <remarks>This method does not track changes to the retrieved entity. If no password hash is
        /// set for the specified document, the method returns <see langword="null"/>.</remarks>
        /// <param name="documentId">The unique identifier of the shared document whose password hash is to be retrieved.</param>
        /// <returns>A <see cref="string"/> containing the password hash for the specified document if it exists; otherwise, <see
        /// langword="null"/>.</returns>
        public async Task<string?> GetSharePasswordAsync(Guid documentId)
        {
            // Die AsNoTracking()-Methode darf nur auf IQueryable<Share> angewendet werden, nicht auf IQueryable<string?>.
            // Daher zuerst die Entität abfragen, dann das Feld extrahieren.
            var passwordHash = await _context.Shares
                .Where(s => s.Id == documentId)
                .AsNoTracking()
                .Select(s => s.PasswordHash)
                .FirstOrDefaultAsync();

            return passwordHash;
        }
    }
}