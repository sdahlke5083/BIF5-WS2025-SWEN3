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
    //public class SharingController : ControllerBase
    public class SharingController : BaseApiController
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

            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default);
            //TODO: Change the data returned
            return NotImplementedStub();
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

            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            throw new NotImplementedException();
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

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default);
            //TODO: Change the data returned
            return NotImplementedStub();
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

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }
    }
}
