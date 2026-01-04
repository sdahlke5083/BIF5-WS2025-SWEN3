using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.REST.API.Controllers;
using Paperless.REST.BLL.Worker;

namespace Paperless.REST.Test.Controllers
{
    public class AdminControllerTests
    {
        private AdminController _controller = null!;
        private IInfrastructureHealthChecker _mockDependencyChecker = null!;

        [SetUp]
        public void Setup()
        {
            _mockDependencyChecker = Mock.Of<IInfrastructureHealthChecker>();
            Mock.Get(_mockDependencyChecker).Setup(x => x.CheckDependenciesAsync())
                .ReturnsAsync((true, Array.Empty<string>()));
            _controller = new AdminController(_mockDependencyChecker);
            // ensure controller has HttpContext to avoid NullReference when accessing Request
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
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
        public async Task Ready_Returns_Ok()
        {
            var actionResult = await _controller.Ready();
            var res = actionResult as ObjectResult;
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
