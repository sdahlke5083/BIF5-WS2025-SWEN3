using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Paperless.REST.Test.Controllers
{
    public class SharingControllerTests
    {
        [Test]
        public void GetShare_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("share1");
            var controller = new Paperless.REST.API.Controllers.SharingController(db);

            var res = controller.GetShare(Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }
    }
}
