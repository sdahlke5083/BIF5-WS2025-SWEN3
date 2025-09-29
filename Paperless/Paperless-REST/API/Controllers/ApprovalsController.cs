using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Approval workflows for documents. (Optional if time)
    /// </summary>
    [ApiController]
    //public class ApprovalsOptionalController : ControllerBase
    public class ApprovalsController : BaseApiController
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

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Uncomment the next line to return response 501 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(501, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }
    }
}
