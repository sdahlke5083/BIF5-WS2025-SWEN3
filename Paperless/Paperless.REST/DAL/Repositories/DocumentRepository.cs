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
            throw new NotImplementedException();
        }

        public Task<List<Document>> GetAllActiveAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Document>> GetAllDeleted(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Document doc, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task PermanentlyDeleteAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
