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
        private readonly DAL.DbContexts.PostgressDbContext _db;

        public StatsController(DAL.DbContexts.PostgressDbContext db)
        {
            _db = db;
        }

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
            // Basic implementation: return zeros for requested range (no analytics store available)
            if (!_db.Documents.Any(d => d.Id == id))
                return NotFound();

            var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var start = from ?? end.AddDays(-6);

            if (start > end)
                return BadRequest("Invalid range");

            var list = new List<object>();
            var d = start;
            while (d <= end)
            {
                list.Add(new { date = d.ToString("yyyy-MM-dd"), views = 0, downloads = 0 });
                d = d.AddDays(1);
            }

            return Ok(new { documentId = id, series = list });
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
            // No analytics backend yet: return empty list
            var lim = Math.Clamp(limit.GetValueOrDefault(10), 1, 100);
            return Ok(new { period = period, metric = metric, items = new List<object>() });
        }
    }
}
