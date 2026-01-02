using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Approval workflows for documents. (Optional if time)
    /// </summary>
    [ApiController]
    public class ApprovalsController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// List approval requests (optional feature)
        /// </summary>
        /// <remarks>Returns 501 until implemented if approvals feature is disabled.</remarks>
        /// <response code="200">OK</response>
        /// <response code="501">Not Implemented (feature stub)</response>
        [HttpGet]
        [Route("/v1/approvals")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public virtual IActionResult ListApprovals()
        {
            // Return empty list for now (approvals feature not yet implemented)
            _logger.Trace("Approvals requested");
            var approvals = new List<object>();
            return Ok(approvals);
        }
    }
}
