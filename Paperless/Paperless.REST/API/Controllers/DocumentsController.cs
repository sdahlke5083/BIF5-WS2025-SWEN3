using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Models.BaseResponse;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.API.Models;
using Paperless.REST.DAL.Models;
using Microsoft.EntityFrameworkCore;

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

        public DocumentsController(PostgressDbContext db)
        {
            _db = db;
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
        //[Authorize]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]

        public virtual IActionResult DeleteDocument([FromRoute (Name = "id")][Required]Guid id)
        {
            //TODO: Implement this
            return StatusCode(501, default);
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
        //[Authorize(Policy = "shareToken")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(Stream))]
        //[ProducesResponseType(statusCode: 206, type: typeof(Stream))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult DownloadFile([FromRoute (Name = "id")][Required]Guid id, [FromHeader (Name = "X-Share-Password")]string xSharePassword)
        {
            //TODO: Implement this
            return StatusCode(501, default);
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
        //[Authorize(Policy = "shareToken")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(Document))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemResponse))]
        public virtual IActionResult GetDocument([FromRoute (Name = "id")][Required]Guid id, [FromHeader (Name = "X-Share-Password")]string xSharePassword)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// List soft-deleted documents
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        /// <response code="200">Paged deleted documents</response>
        [HttpGet]
        [Route("/v1/recycle-bin")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(DocumentPage))]
        public virtual IActionResult ListDeleted([FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize)
        {
            //TODO: Implement this
            return StatusCode(501, default);
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
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(DocumentPage))]
        [ProducesResponseType(400)]
        public virtual IActionResult ListDocuments([FromQuery (Name = "q")]string q, [FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize, [FromQuery (Name = "sort")]string sort, [FromQuery (Name = "fileType")]string fileType, [FromQuery (Name = "sizeMin")]long? sizeMin, [FromQuery (Name = "sizeMax")]long? sizeMax, [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, [FromQuery (Name = "hasSummary")]bool? hasSummary, [FromQuery (Name = "hasError")]bool? hasError, [FromQuery (Name = "uploaderId")]Guid? uploaderId, [FromQuery (Name = "workspaceId")]Guid? workspaceId, [FromQuery (Name = "approvalStatus")]string approvalStatus, [FromQuery (Name = "shared")]bool? shared)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// List summaries for a document (latest first)
        /// </summary>
        /// <param name="id"></param>
        /// <response code="200">Summaries</response>
        [HttpGet]
        [Route("/v1/documents/{id}/summaries")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ListSummaries200Response))]
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
        /// <param name="documentPatch"></param>
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
            //TODO: Implement this
            return StatusCode(501, default);
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
            //TODO: Implement this
            return StatusCode(501, default);
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
            //TODO: Implement this
            return StatusCode(501, default);
        }
    }
}
