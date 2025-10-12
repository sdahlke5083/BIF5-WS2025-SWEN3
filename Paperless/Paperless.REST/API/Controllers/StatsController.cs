using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Access statistics (daily aggregates) for documents.
    /// </summary>
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get daily access stats for a document
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <response code="200">Time series</response>
        [HttpGet]
        [Route("/v1/documents/{id}/stats")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(AccessStatsSeries))]
        public virtual IActionResult GetDocumentStats([FromRoute (Name = "id")][Required]Guid id, [FromQuery (Name = "from")]DateOnly? from, [FromQuery (Name = "to")]DateOnly? to)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Get top documents by views/downloads
        /// </summary>
        /// <param name="period"></param>
        /// <param name="metric"></param>
        /// <param name="limit"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/stats/top")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(GetTopStats200Response))]
        public virtual IActionResult GetTopStats([FromQuery (Name = "period")]string period, [FromQuery (Name = "metric")]string metric, [FromQuery (Name = "limit")][Range(1, 100)]int? limit)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }
    }
}
