using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Inspect processing status and trigger re-processing actions.
    /// </summary>
    [ApiController]
    //public class ProcessingController : ControllerBase
    public class ProcessingController : BaseApiController
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

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

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default);
            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default);
            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default);
            //TODO: Change the data returned
            return NotImplementedStub();
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

            //TODO: Uncomment the next line to return response 202 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(202);
            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default);
            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default);
            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default);

            throw new NotImplementedException();
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

            //TODO: Uncomment the next line to return response 202 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(202);
            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default);
            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default);
            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default);

            throw new NotImplementedException();
        }
    }
}
