using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class DocumentsControllerTests
    {
        [Test]
        public void GetDocument_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("docs1");
            var repo = new Paperless.REST.DAL.Repositories.DocumentRepository(db);
            var fileStorageMock = new Moq.Mock<Paperless.REST.BLL.Storage.IFileStorageService>();
            var controller = new DocumentsController(db, repo, fileStorageMock.Object);

            var res = controller.GetDocument(Guid.NewGuid(), String.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }
    }
}
