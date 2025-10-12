using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Organize documents into workspaces and manage membership.
    /// </summary>
    [ApiController]
    public class WorkspacesController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Add a member to a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="workspaceMemberCreate"></param>
        /// <response code="201">Added</response>
        [HttpPost]
        [Route("/v1/workspaces/{workspaceId}/members")]
        //[Authorize]
        [Consumes("application/json")]
        //[ProducesResponseType(statusCode: 201, type: typeof(WorkspaceMember))]
        public virtual IActionResult AddWorkspaceMember([FromRoute (Name = "workspaceId")][Required]Guid workspaceId, [FromBody]JsonElement body)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Create a workspace
        /// </summary>
        /// <param name="workspaceCreate"></param>
        /// <response code="201">Created</response>
        [HttpPost]
        [Route("/v1/workspaces")]
        //[Authorize]
        [Consumes("application/json")]
        //[ProducesResponseType(statusCode: 201, type: typeof(Workspace))]
        public virtual IActionResult CreateWorkspace([FromBody]JsonElement body)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Delete a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <response code="204">Deleted</response>
        [HttpDelete]
        [Route("/v1/workspaces/{workspaceId}")]
        //[Authorize]
        public virtual IActionResult DeleteWorkspace([FromRoute (Name = "workspaceId")][Required]Guid workspaceId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Get a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <response code="200">OK</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/workspaces/{workspaceId}")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(Workspace))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult GetWorkspace([FromRoute (Name = "workspaceId")][Required]Guid workspaceId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// List workspace members
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/workspaces/{workspaceId}/members")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ListWorkspaceMembers200Response))]
        public virtual IActionResult ListWorkspaceMembers([FromRoute (Name = "workspaceId")][Required]Guid workspaceId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// List workspaces
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/workspaces")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ListWorkspaces200Response))]
        public virtual IActionResult ListWorkspaces()
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Update workspace (JSON Merge Patch)
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="workspacePatch"></param>
        /// <param name="ifMatch">Strong ETag of the resource to guard against concurrent updates.</param>
        /// <response code="200">Updated</response>
        /// <response code="412">Precondition failed (ETag mismatch)</response>
        [HttpPatch]
        [Route("/v1/workspaces/{workspaceId}")]
        //[Authorize]
        [Consumes("application/merge-patch+json")]
        //[ProducesResponseType(statusCode: 200, type: typeof(Workspace))]
        //[ProducesResponseType(statusCode: 412, type: typeof(Problem))]
        public virtual IActionResult PatchWorkspace([FromRoute (Name = "workspaceId")][Required]Guid workspaceId, [FromBody]JsonElement body, [FromHeader (Name = "If-Match")]string ifMatch)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }

        /// <summary>
        /// Remove a workspace member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <response code="204">Removed</response>
        [HttpDelete]
        [Route("/v1/workspaces/{workspaceId}/members/{userId}")]
        //[Authorize]
        public virtual IActionResult RemoveWorkspaceMember([FromRoute (Name = "workspaceId")][Required]Guid workspaceId, [FromRoute (Name = "userId")][Required]Guid userId)
        {
            //TODO: Implement this
            return StatusCode(501, default);
        }
    }
}
