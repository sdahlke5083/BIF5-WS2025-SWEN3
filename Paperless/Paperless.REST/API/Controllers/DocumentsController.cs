using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.API.Models;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Exceptions;
using Paperless.REST.BLL.Search;
using Paperless.REST.BLL.Storage;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Exceptions;
using Paperless.REST.DAL.Models;
using Paperless.REST.DAL.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Retrieve and manage documents and their metadata.
    /// </summary>
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly PostgressDbContext _db;
        private readonly IDocumentRepository _repo;
        private readonly IFileStorageService _fileStorage;
        private readonly MyElasticSearchClient _esclient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class with the specified database
        /// context, document repository, and file storage service.
        /// </summary>
        /// <remarks>Use this constructor to create a <see cref="DocumentsController"/> with custom data
        /// access and storage dependencies, typically for dependency injection scenarios.</remarks>
        /// <param name="db">The database context used for data access operations.</param>
        /// <param name="repo">The document repository that provides access to document data.</param>
        /// <param name="fileStorage">The file storage service used for managing document files.</param>
        /// <param name="esclient">Elastic search client</param>
        public DocumentsController(PostgressDbContext db, IDocumentRepository repo, IFileStorageService fileStorage, MyElasticSearchClient esclient)
        {
            _db = db;
            _repo = repo;
            _fileStorage = fileStorage;
            _esclient = esclient;
        }

        /// <summary>
        /// Soft delete a document (move to recycle bin)
        /// </summary>
        /// <param name="id"></param>
        /// <response code="204">Deleted (soft)</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpDelete]
        [Route("/v1/documents/{id}")]
        [Authorize]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]

        public virtual IActionResult DeleteDocument([FromRoute (Name = "id")][Required]Guid id)
        {
            try
            {
                _repo.DeleteAsync(id).GetAwaiter().GetResult();
                return NoContent();
            }
            catch (DataAccessException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting document {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Download original file (supports Range requests)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="xSharePassword">Required for guest access when using a share token.</param>
        /// <response code="200">File stream</response>
        /// <response code="206">Partial content</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/documents/{id}/file")]
        [Authorize(Policy = "shareToken")]
        [Authorize]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult DownloadFile([FromRoute (Name = "id")][Required]Guid id, [FromHeader (Name = "X-Share-Password")]string? xSharePassword)
        {
            // find latest file version
            var doc = _db.Documents.Include(d => d.FileVersions).FirstOrDefault(d => d.Id == id);
            if (doc is null)
                return NotFound();

            var file = doc.FileVersions.OrderByDescending(f => f.Version).FirstOrDefault();
            if (file is null)
                return NotFound();

            // object key used by uploads: "{documentId}/{originalFileName}" or StoredName
            var objectName = !string.IsNullOrWhiteSpace(file.StoredName) ? file.StoredName : $"{id:D}/{file.OriginalFileName}";

            try
            {
                var stream = _fileStorage.OpenReadStreamAsync(objectName).GetAwaiter().GetResult();
                return File(stream, file.FileType?.MimeType ?? "application/octet-stream", file.OriginalFileName, enableRangeProcessing: true);
            }
            catch (FileStorageException ex)
            {
                _logger.Warn(ex, "File not found in storage: {Object}", objectName);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error downloading file {Object}", objectName);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get a small PNG preview (thumbnail) of the first PDF page.
        /// The OCR worker stores it in MinIO as "{documentId}/thumbnail.png".
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("/v1/documents/{id}/thumbnail")]
        public virtual async Task<IActionResult> GetThumbnail([FromRoute(Name = "id")][Required] Guid id)
        {
            var objectName = $"{id:D}/thumbnail.png";

            try
            {
                var stream = await _fileStorage.OpenReadStreamAsync(objectName);
                return File(stream, "image/png");
            }
            catch (FileStorageException ex)
            {
                _logger.Warn(ex, "Thumbnail not found in storage: {Object}", objectName);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error downloading thumbnail {Object}", objectName);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get a document by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="xSharePassword">Required for guest access when using a share token.</param>
        /// <response code="200">Document</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/documents/{id}")]
        [Authorize(Policy = "shareToken")]
        //[ProducesResponseType(statusCode: 200, type: typeof(Document))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult GetDocument([FromRoute (Name = "id")][Required]Guid id, [FromHeader (Name = "X-Share-Password")]string? xSharePassword)
        {
            var doc = _repo.GetByIdAsync(id).GetAwaiter().GetResult();
            if (doc is null)
                return NotFound();

            // build a simple response
            var latestMeta = doc.MetadataVersions.OrderByDescending(m => m.Version).FirstOrDefault();
            var latestFile = doc.FileVersions.OrderByDescending(f => f.Version).FirstOrDefault();

            return Ok(new
            {
                id = doc.Id,
                currentMetadataVersion = doc.CurrentMetadataVersion,
                currentFileVersion = doc.CurrentFileVersion,
                deletedAt = doc.DeletedAt,
                workspaceId = doc.WorkspaceId,
                metadata = latestMeta is null ? null : new
                {
                    title = latestMeta.Title,
                    description = latestMeta.Description,
                    languageCode = latestMeta.LanguageCode,
                    createdAt = latestMeta.CreatedAt
                },
                file = latestFile is null ? null : new
                {
                    originalFileName = latestFile.OriginalFileName,
                    uploadedAt = latestFile.UploadedAt
                }
            });
        }

        /// <summary>
        /// List soft-deleted documents
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        /// <response code="200">Paged deleted documents</response>
        [HttpGet]
        [Route("/v1/recycle-bin")]
        [Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(DocumentPage))]
        public virtual IActionResult ListDeleted([FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize)
        {
            var p = page.GetValueOrDefault(1);
            var ps = Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var all = _repo.GetAllDeleted().GetAwaiter().GetResult();
            var total = all.Count;
            var items = all.Skip((p - 1) * ps).Take(ps).Select(d => new
            {
                id = d.Id,
                deletedAt = d.DeletedAt,
                title = d.MetadataVersions.OrderByDescending(m => m.Version).FirstOrDefault()?.Title
            }).ToList();

            return Ok(new { page = p, pageSize = ps, total, items });
        }

        /// <summary>
        /// List documents with optional filters (page-based)
        /// </summary>
        /// <param name="q">Free-text query</param>
        /// <param name="page"></param>
        /// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        /// <param name="sort"></param>
        /// <param name="fileType"></param>
        /// <param name="sizeMin"></param>
        /// <param name="sizeMax"></param>
        /// <param name="uploadDateFrom"></param>
        /// <param name="uploadDateTo"></param>
        /// <param name="hasSummary"></param>
        /// <param name="hasError"></param>
        /// <param name="uploaderId"></param>
        /// <param name="workspaceId"></param>
        /// <param name="approvalStatus"></param>
        /// <param name="shared"></param>
        /// <response code="200">Paged result</response>
        /// <response code="400">Bad Request: wrong format or missing data</response>
        [HttpGet]
        [Route("/v1/documents")]
        [Authorize]
        [ProducesResponseType(statusCode: 200)]
        [ProducesResponseType(400)]
        public virtual IActionResult ListDocuments(
            [FromQuery (Name = "q")]string? q, 
            [FromQuery (Name = "page")]int? page, 
            [FromQuery (Name = "pageSize")]int? pageSize, 
            [FromQuery (Name = "sort")]string? sort, 
            [FromQuery (Name = "fileType")]string? fileType, 
            [FromQuery (Name = "sizeMin")]long? sizeMin, 
            [FromQuery (Name = "sizeMax")]long? sizeMax, 
            [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, 
            [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, 
            [FromQuery (Name = "hasSummary")]bool? hasSummary, 
            [FromQuery (Name = "hasError")]bool? hasError, 
            [FromQuery (Name = "uploaderId")]Guid? uploaderId, 
            [FromQuery (Name = "workspaceId")]Guid? workspaceId, 
            [FromQuery (Name = "approvalStatus")]string? approvalStatus, 
            [FromQuery (Name = "shared")]bool? shared)
        {
            var p = page.GetValueOrDefault(1);
            var ps = Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var all = _repo.GetAllActiveAsync().GetAwaiter().GetResult().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                all = all.Where(d => d.MetadataVersions.Any(m => m.Title != null && m.Title.Contains(q)));
            }

            if (workspaceId.HasValue)
                all = all.Where(d => d.WorkspaceId == workspaceId.Value);

            var total = all.Count();

            var items = all.Skip((p - 1) * ps).Take(ps).Select(d => new
            {
                id = d.Id,
                fileName = d.FileVersions
                    .OrderByDescending(f => f.Version)
                    .Select(f => f.OriginalFileName)
                    .FirstOrDefault(),

                title = d.MetadataVersions
                    .OrderByDescending(m => m.Version)
                    .Select(m => m.Title)
                    .FirstOrDefault(),

                uploadedAt = d.FileVersions
                    .OrderByDescending(f => f.Version)
                    .Select(f => (DateTimeOffset?)f.UploadedAt)
                    .FirstOrDefault(),

                size = d.FileVersions
                    .OrderByDescending(f => f.Version)
                    .Select(f => (long?)f.SizeBytes)
                    .FirstOrDefault()
            }).ToList();


            return Ok(new { page = p, pageSize = ps, total, items });
        }

        /// <summary>
        /// List summaries for a document (latest first)
        /// </summary>
        /// <param name="id"></param>
        /// <response code="200">Summaries</response>
        [HttpGet]
        [Route("/v1/documents/{id}/summaries")]
        [Authorize]
        public virtual async Task<IActionResult> ListSummaries([FromRoute (Name = "id")][Required]Guid id)
        {
            var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (doc is null)
                return NotFound();

            var summaries = await _db.DocumentSummaries
                .Where(s => s.DocumentId == id)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new {
                    id = s.Id,
                    createdAt = s.CreatedAt,
                    model = s.Model,
                    lengthPresetId = s.LengthPresetId,
                    content = s.Content
                })
                .ToListAsync();

            return Ok(summaries);
        }

        /// <summary>
        /// Create a summary for a document (used by internal GenAI workers)
        /// </summary>
        [HttpPost]
        [Route("/v1/documents/{id}/summaries")]
        public virtual async Task<IActionResult> CreateSummary([FromRoute(Name = "id")][Required] Guid id, [FromBody] SummaryCreateRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest();

            var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id);
            if (doc is null)
                return NotFound();

            // Determine length preset
            Guid presetId;
            if (request.LengthPresetId.HasValue)
            {
                var exists = await _db.SummaryPresets.AnyAsync(p => p.Id == request.LengthPresetId.Value);
                if (exists)
                    presetId = request.LengthPresetId.Value;
                else
                    presetId = await EnsureDefaultPresetAsync();
            }
            else
            {
                presetId = await EnsureDefaultPresetAsync();
            }

            var summary = new Summary
            {
                DocumentId = id,
                Content = request.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                Model = request.Model ?? "genai",
                LengthPresetId = presetId
            };

            var processingState = await _db.ProcessingStatuses
                .Where(ps => ps.DocumentId == doc.Id).FirstOrDefaultAsync();
            if (processingState is not null)
            {
                processingState.Summary = ProcessingState.Succeeded;
                _db.ProcessingStatuses.Update(processingState);
            }

            await _db.DocumentSummaries.AddAsync(summary);
            await _db.SaveChangesAsync();

            try
            {
                var es = HttpContext.RequestServices.GetService(typeof(BLL.Search.MyElasticSearchClient)) as BLL.Search.MyElasticSearchClient;
                if (es != null)
                {
                    await es.UpdateSummaryAsync(id, summary.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to update summary in Elasticsearch");
            }

            return Created($"/v1/documents/{id}/summaries/{summary.Id}", new { id = summary.Id });
        }

        private async Task<Guid> EnsureDefaultPresetAsync()
        {
            var preset = await _db.SummaryPresets.FirstOrDefaultAsync();
            if (preset is not null)
                return preset.Id;

            preset = new SummaryPreset
            {
                Name = "default",
                Description = "Default summary preset",
                Prompt = "Summarize the given document text in 3 bullet points.",
                MaxTokens = 250
            };

            await _db.SummaryPresets.AddAsync(preset);
            await _db.SaveChangesAsync();
            return preset.Id;
        }

        /// <summary>
        /// Update document metadata (JSON Merge Patch)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="body">JSON Merge Patch document</param>
        /// <param name="ifMatch">Strong ETag of the resource to guard against concurrent updates.</param>
        /// <response code="200">Updated</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        /// <response code="412">Precondition failed (ETag mismatch)</response>
        [HttpPatch]
        [Route("/v1/documents/{id}")]
        //[Authorize]
        [Consumes("application/merge-patch+json")]
        //[ProducesResponseType(statusCode: 200, type: typeof(Document))]
        [ProducesResponseType(statusCode: 400, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 412, type: typeof(ProblemResponse))]
        public virtual IActionResult PatchDocument([FromRoute (Name = "id")][Required]Guid id, [FromBody]JsonElement body, [FromHeader (Name = "If-Match")]string ifMatch)
        {
            try
            {
                var doc = _db.Documents.Include(d => d.MetadataVersions).FirstOrDefault(d => d.Id == id);
                if (doc is null)
                    return NotFound();

                // ETag/If-Match support: If client provides If-Match with base64 of RowVersion, ensure it matches
                if (!string.IsNullOrWhiteSpace(ifMatch))
                {
                    try
                    {
                        var expected = Convert.ToBase64String(doc.RowVersion ?? Array.Empty<byte>());
                        if (ifMatch != expected)
                            return StatusCode(StatusCodes.Status412PreconditionFailed);
                    }
                    catch
                    {
                        return StatusCode(StatusCodes.Status412PreconditionFailed);
                    }
                }

                // apply merge patch for simple fields: title, description, languageCode, workspaceId
                var latestMeta = doc.MetadataVersions.OrderByDescending(m => m.Version).FirstOrDefault();
                var newMeta = new DocumentMetadata
                {
                    DocumentId = doc.Id,
                    Version = (latestMeta?.Version ?? 0) + 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Title = latestMeta?.Title,
                    Description = latestMeta?.Description,
                    LanguageCode = latestMeta?.LanguageCode,
                    CreatedByUserId = latestMeta?.CreatedByUserId
                };

                if (body.ValueKind != JsonValueKind.Object)
                    return BadRequest();

                if (body.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                    newMeta.Title = t.GetString();
                if (body.TryGetProperty("description", out var dsc) && dsc.ValueKind == JsonValueKind.String)
                    newMeta.Description = dsc.GetString();
                if (body.TryGetProperty("languageCode", out var lc) && lc.ValueKind == JsonValueKind.String)
                    newMeta.LanguageCode = lc.GetString();
                if (body.TryGetProperty("workspaceId", out var ws) && ws.ValueKind == JsonValueKind.String && Guid.TryParse(ws.GetString(), out var wsId))
                    doc.WorkspaceId = wsId;

                _db.DocumentMetadatas.Add(newMeta);
                doc.CurrentMetadataVersion = newMeta.Version;
                _db.SaveChanges();

                // Return new ETag
                var etag = Convert.ToBase64String(doc.RowVersion ?? Array.Empty<byte>());
                Response.Headers["ETag"] = etag;

                return Ok(new { id = doc.Id });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error patching document {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Hard-delete a document (irreversible)
        /// </summary>
        /// <param name="id"></param>
        /// <response code="204">Purged</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpDelete]
        [Route("/v1/documents/{id}:purge")]
        //[Authorize]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult PurgeDocument([FromRoute (Name = "id")][Required]Guid id)
        {
            try
            {
                // attempt to remove object(s) from storage if file versions exist
                var doc = _db.Documents.Include(d => d.FileVersions).FirstOrDefault(d => d.Id == id);
                if (doc is null)
                    return NotFound();

                foreach (var fv in doc.FileVersions)
                {
                    var objectName = !string.IsNullOrWhiteSpace(fv.StoredName) ? fv.StoredName : $"{id:D}/{fv.OriginalFileName}";
                    try { _fileStorage.DeleteFileAsync(objectName).GetAwaiter().GetResult(); } catch { /* ignore */ }
                }

                // remove document from Elasticsearch index (best-effort)
                try
                {
                    if (_esclient != null)
                    {
                        _esclient.DeleteDocumentAsync(id).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to remove document {Id} from Elasticsearch", id);
                }

                _repo.PermanentlyDeleteAsync(id).GetAwaiter().GetResult();
                return NoContent();
            }
            catch (DataAccessException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error purging document {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Restore a soft-deleted document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="204">Restored</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpPost]
        [Route("/v1/documents/{id}:restore")]
        //[Authorize]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult RestoreDocument([FromRoute (Name = "id")][Required]Guid id)
        {
            try
            {
                _repo.RestoreAsync(id).GetAwaiter().GetResult();
                return NoContent();
            }
            catch (DataAccessException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error restoring document {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
