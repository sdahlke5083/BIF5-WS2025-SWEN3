using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class WorkspacesControllerTests
    {
        [Test]
        public void ListWorkspaces_Returns_OK()
        {
            var db = TestUtils.CreateInMemoryDb("ws1");
            var controller = new WorkspacesController(db); // No change made

            var res = controller.ListWorkspaces() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
