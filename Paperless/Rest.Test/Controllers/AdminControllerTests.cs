using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class AdminControllerTests
    {
        private AdminController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _controller = new AdminController();
            // ensure controller has HttpContext to avoid NullReference when accessing Request
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
        }

        [Test]
        public void Health_Returns_Ok()
        {
            var res = _controller.Health() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Ready_Returns_Ok()
        {
            var res = _controller.Ready() as ObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode == 200 || res.StatusCode == 503, Is.True);
        }

        [Test]
        public void Diagnostics_Returns_Ok()
        {
            var res = _controller.Diagnostics() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
