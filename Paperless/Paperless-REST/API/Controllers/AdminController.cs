using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Health, readiness, diagnostics, and audit logs.
    /// </summary>
    [ApiController]
    //public class AdminController : ControllerBase
    public class AdminController : BaseApiController
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Read-only diagnostics (no secrets)
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/admin/diagnostics")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual IActionResult Diagnostics()
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }

        /// <summary>
        /// Liveness check
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/health")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual IActionResult Health()
        {
            return Ok(new { status = "OK" });
        }

        /// <summary>
        /// Query audit logs (admin only)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="page"></param>
        /// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/admin/audit-logs")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual IActionResult ListAuditLogs([FromQuery (Name = "level")]string level, [FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize)
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }

        /// <summary>
        /// Readiness check
        /// </summary>
        /// <response code="200">Ready</response>
        [HttpGet]
        [Route("/ready")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual IActionResult Ready()
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }
    }
}
