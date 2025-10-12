using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Create and manage password-protected share links for guests.
    /// </summary>
    [ApiController]
    public class SharingController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Create a password-protected share link
        /// </summary>
        /// <param name="id"></param>
        /// <param name="shareCreate"></param>
        /// <response code="201">Created</response>
        [HttpPost]
        [Route("/v1/documents/{id}/shares")]
        [Authorize]
        [Consumes("application/json")]
        //[ProducesResponseType(statusCode: 201, type: typeof(Share))]
        public virtual IActionResult CreateShare([FromRoute (Name = "id")][Required]Guid id, [FromBody]JsonElement body)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Revoke a share
        /// </summary>
        /// <param name="shareId"></param>
        /// <response code="204">Revoked</response>
        [HttpDelete]
        [Route("/v1/shares/{shareId}")]
        //[Authorize]
        public virtual IActionResult DeleteShare([FromRoute (Name = "shareId")][Required]Guid shareId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Get a share configuration
        /// </summary>
        /// <param name="shareId"></param>
        /// <response code="200">OK</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/shares/{shareId}")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(Share))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult GetShare([FromRoute (Name = "shareId")][Required]Guid shareId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// List share links for a document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/documents/{id}/shares")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ListShares200Response))]
        public virtual IActionResult ListShares([FromRoute (Name = "id")][Required]Guid id)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }
    }
}
