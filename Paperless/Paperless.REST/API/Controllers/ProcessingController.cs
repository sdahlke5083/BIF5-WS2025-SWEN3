using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Inspect processing status and trigger re-processing actions.
    /// </summary>
    [ApiController]
    public class ProcessingController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DAL.DbContexts.PostgressDbContext _db;
        private readonly BLL.Worker.IDocumentEventPublisher _publisher;

        public ProcessingController(DAL.DbContexts.PostgressDbContext db, BLL.Worker.IDocumentEventPublisher publisher)
        {
            _db = db;
            _publisher = publisher;
        }

        /// <summary>
        /// Get processing status (OCR, Summary, Index) for a document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="200">Processing status</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/documents/{id}/processing")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ProcessingStatus))]
        //[ProducesResponseType(statusCode: 401, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 403, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult GetProcessingStatus([FromRoute (Name = "id")][Required]Guid id)
        {
            var ps = _db.ProcessingStatuses.FirstOrDefault(p => p.DocumentId == id);
            if (ps is null) return NotFound();
            return Ok(new
            {
                documentId = ps.DocumentId,
                ocr = ps.Ocr.ToString().ToLowerInvariant(),
                summary = ps.Summary.ToString().ToLowerInvariant(),
                index = ps.Index.ToString().ToLowerInvariant(),
                lastError = ps.LastError,
                updatedAt = ps.UpdatedAt
            });
        }

        /// <summary>
        /// Re-trigger OCR for a document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="202">Reprocessing queued</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpPost]
        [Route("/v1/documents/{id}/actions/redo-ocr")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 401, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 403, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult RedoOcr([FromRoute (Name = "id")][Required]Guid id)
        {
            var doc = _db.Documents.FirstOrDefault(d => d.Id == id);
            if (doc is null) return NotFound();

            // publish a message to queue for OCR
            _publisher.PublishDocumentUploadedAsync(id.ToString()).GetAwaiter().GetResult();
            return Accepted();
        }

        /// <summary>
        /// Re-trigger GenAI summary for a document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="202">Reprocessing queued</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        [HttpPost]
        [Route("/v1/documents/{id}/actions/redo-summary")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 401, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 403, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult RedoSummary([FromRoute (Name = "id")][Required]Guid id)
        {
            var doc = _db.Documents.FirstOrDefault(d => d.Id == id);
            if (doc is null) return NotFound();

            // publish a message for summary (reuse same routing key via publisher implementation)
            _publisher.PublishDocumentUploadedAsync(id.ToString()).GetAwaiter().GetResult();
            return Accepted();
        }
    }
}
