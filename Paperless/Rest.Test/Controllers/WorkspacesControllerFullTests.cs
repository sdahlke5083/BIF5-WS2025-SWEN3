using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.DAL.Models;
using System.Text.Json;
using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Paperless.REST.Test.Controllers
{
    public class WorkspacesControllerFullTests
    {
        [Test]
        public void Create_Get_Delete_Workspace_Flow()
        {
            var db = TestUtils.CreateInMemoryDb("ws_flow");
            var controller = new WorkspacesController(db);

            // Create workspace
            var json = JsonDocument.Parse("{\"name\":\"My WS\", \"description\":\"desc\"}");
            var createRes = controller.CreateWorkspace(json.RootElement) as CreatedResult;
            Assert.That(createRes, Is.Not.Null);
            Assert.That(createRes!.StatusCode, Is.EqualTo(201));

            // created result returns anonymous { id = ws.Id }
            var createdValue = createRes.Value!;
            var idProp = createdValue.GetType().GetProperty("id");
            if (idProp == null) Assert.Fail("Created response does not contain id property");
            var createdId = (Guid)idProp!.GetValue(createdValue)!;

            // Get workspace
            var getRes = controller.GetWorkspace(createdId) as OkObjectResult;
            Assert.That(getRes, Is.Not.Null);
            Assert.That(getRes!.StatusCode, Is.EqualTo(200));

            // List workspaces
            var listRes = controller.ListWorkspaces() as OkObjectResult;
            Assert.That(listRes, Is.Not.Null);

            // Delete workspace
            var delRes = controller.DeleteWorkspace((Guid)createdId) as NoContentResult;
            Assert.That(delRes, Is.Not.Null);
            Assert.That(delRes!.StatusCode, Is.EqualTo(204));
        }

        [Test]
        public void Add_And_Remove_Workspace_Member()
        {
            var db = TestUtils.CreateInMemoryDb("ws_member");
            // create workspace
            var ws = new Workspace { Id = Guid.NewGuid(), Name = "W1" };
            db.Workspaces.Add(ws);
            // create workspace role
            var wr = new WorkspaceRole { Id = Guid.NewGuid(), Name = "Editor" };
            db.WorkspaceRoles.Add(wr);
            db.SaveChanges();

            var controller = new WorkspacesController(db);

            // Build request JSON for AddWorkspaceMember
            var body = JsonDocument.Parse($"{{\"userId\":\"{db.Users.First().Id}\",\"roleId\":\"{wr.Id}\"}}").RootElement;

            var addRes = controller.AddWorkspaceMember(ws.Id, body) as CreatedResult;
            Assert.That(addRes, Is.Not.Null);
            Assert.That(addRes!.StatusCode, Is.EqualTo(201));

            // List members
            var membersRes = controller.ListWorkspaceMembers(ws.Id) as OkObjectResult;
            Assert.That(membersRes, Is.Not.Null);
            var members = membersRes.Value as System.Collections.IEnumerable;
            Assert.That(members, Is.Not.Null);

            // Remove member
            var removeRes = controller.RemoveWorkspaceMember(ws.Id, db.Users.First().Id) as NoContentResult;
            Assert.That(removeRes, Is.Not.Null);
            Assert.That(removeRes!.StatusCode, Is.EqualTo(204));
        }
    }
}
