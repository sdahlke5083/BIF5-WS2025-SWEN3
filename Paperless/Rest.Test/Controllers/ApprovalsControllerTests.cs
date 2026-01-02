using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class ApprovalsControllerTests
    {
        [Test]
        public void ListApprovals_Returns_OK()
        {
            var controller = new ApprovalsController();
            var res = controller.ListApprovals() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
