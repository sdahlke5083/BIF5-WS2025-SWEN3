using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class ProcessingControllerTests
    {
        [Test]
        public void GetProcessingStatus_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("proc1");
            var publisherMock = new Moq.Mock<Paperless.REST.BLL.Worker.IDocumentEventPublisher>();
            var controller = new ProcessingController(db, publisherMock.Object);

            var res = controller.GetProcessingStatus(Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }
    }
}
