using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.API.Models.QueryModels;
using Paperless.REST.Attributes;

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
        /// Liveness check
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/health")]
        //[Authorize]
        [ProducesResponseType(statusCode: 200, type: typeof(BasicStatusResponse))]
        public virtual IActionResult Health()
        {
            _logger.Trace($"Health requested from {Request.HttpContext.Connection.RemoteIpAddress}");

            return Ok(new BasicStatusResponse { Status = "OK" });
        }

        /// <summary>
        /// Readiness check
        /// </summary>
        /// <response code="200">Ready</response>
        /// <response code="503">Service unavailable (not ready)</response>
        [HttpGet]
        [Route("/ready")]
        //[Authorize]
        [ProducesResponseType(statusCode: 200, type: typeof(BasicStatusResponse))]
        [ProducesResponseType(statusCode: 503, type: typeof(BasicStatusResponse))]
        public virtual IActionResult Ready()
        {
            _logger.Trace($"Readiness requested from {Request.HttpContext.Connection.RemoteIpAddress}");
            //TODO: check DB connection, other dependencies, ...
            var dbReady = true;

            if (dbReady)
                return Ok(new BasicStatusResponse { Status = "READY" });
            else
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new BasicStatusResponse { Status = "Not ready" });

        }

        /// <summary>
        /// Read-only diagnostics (no secrets)
        /// </summary>
        /// <response code="200">Returns a set of basic diagnostics data.</response>
        /// <response code="403">Forbidden</response>
        [HttpGet]
        [Route("/v1/admin/diagnostics")]
        //[Authorize]
        [ProducesResponseType(statusCode: 200, type: typeof(DiagnosticsResponse))]
        public virtual IActionResult Diagnostics()
        {
            _logger.Trace($"Diagnostics requested from {Request.HttpContext.Connection.RemoteIpAddress}");
            //TODO: get diagnostics info (no secrets) from BLL (z.B. app version, db version, connected services versions, workers connected, queueBackLog, ...)
            var diagnostics = new DiagnosticsResponse
            {
                ApplicationVersion = "",
                DatabaseVersion = "",
                WorkersConnected = false,
                QueueBacklog = 0
            };
            
            return Ok(diagnostics);

        }

        /// <summary>
        /// Query audit logs (admin only)
        /// </summary>
        /// <example>?Level=info&Page=1&PageSize=20</example>
        /// <param name="requestQuery">Query parameters for filtering and pagination</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request (invalid parameters)</response>
        /// <response code="403">Forbidden</response>
        [HttpGet]
        [Route("/v1/admin/audit-logs")]
        //[Authorize]
        [ValidateModelState]
        [ProducesResponseType(statusCode: 200, type: typeof(AuditLogListResponse))]
        public virtual IActionResult ListAuditLogs([FromQuery] AuditRequestQuery requestQuery)
        {
            _logger.Trace($"Audit logs requested from {Request.HttpContext.Connection.RemoteIpAddress}");
            //TODO: fetch actual audit logs from BLL with paging, filtering by level, ...
            var audit = new AuditLogListResponse
            {
                AuditLogs = new List<string> { "Log1", "Log2" }
            };

            return Ok(audit);
        }
    }
}
