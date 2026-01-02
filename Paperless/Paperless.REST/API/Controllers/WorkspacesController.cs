using Microsoft.AspNetCore.Mvc;
using Paperless.REST.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;
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
        private readonly PostgressDbContext _db;

        public WorkspacesController(PostgressDbContext db)
        {
            _db = db;
        }
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
            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest();

            if (!_db.Workspaces.Any(w => w.Id == workspaceId))
                return NotFound();

            if (!body.TryGetProperty("userId", out var userIdProp) || !Guid.TryParse(userIdProp.GetString(), out var userId))
                return BadRequest();

            if (!body.TryGetProperty("roleId", out var roleIdProp) || !Guid.TryParse(roleIdProp.GetString(), out var roleId))
                return BadRequest();

            var member = new DAL.Models.WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                WorkspaceRoleId = roleId
            };

            _db.WorkspaceMembers.Add(member);
            _db.SaveChanges();

            return Created($"/v1/workspaces/{workspaceId}/members/{userId}", member);
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
            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest();

            var name = body.GetProperty("name").GetString();
            var description = body.TryGetProperty("description", out var d) ? d.GetString() : null;

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest();

            var ws = new DAL.Models.Workspace
            {
                Name = name,
                Description = description
            };

            _db.Workspaces.Add(ws);
            _db.SaveChanges();

            return Created($"/v1/workspaces/{ws.Id}", new { id = ws.Id });
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
            var ws = _db.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null)
                return NotFound();

            _db.Workspaces.Remove(ws);
            _db.SaveChanges();
            return NoContent();
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
            var ws = _db.Workspaces.Include(w => w.Members).ThenInclude(m => m.User).FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null)
                return NotFound();

            return Ok(new
            {
                id = ws.Id,
                name = ws.Name,
                description = ws.Description,
                members = ws.Members.Select(m => new { userId = m.UserId, roleId = m.WorkspaceRoleId })
            });
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
            var members = _db.WorkspaceMembers.Where(m => m.WorkspaceId == workspaceId)
                .Select(m => new { userId = m.UserId, roleId = m.WorkspaceRoleId })
                .ToList();

            return Ok(members);
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
            var ws = _db.Workspaces.Select(w => new { id = w.Id, name = w.Name, description = w.Description }).ToList();
            return Ok(ws);
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
            var ws = _db.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null)
                return NotFound();

            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest();

            if (body.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                ws.Name = n.GetString()!;
            if (body.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                ws.Description = d.GetString();

            _db.SaveChanges();
            return Ok(new { id = ws.Id });
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
            var m = _db.WorkspaceMembers.FirstOrDefault(w => w.WorkspaceId == workspaceId && w.UserId == userId);
            if (m is null)
                return NotFound();

            _db.WorkspaceMembers.Remove(m);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
