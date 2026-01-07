using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.BLL.Search;
using Paperless.REST.DAL.Repositories;
using Paperless.REST.BLL.Storage;

namespace Paperless.REST.Test.Controllers
{
    public class DocumentsControllerTests
    {
        [Test]
        public void GetDocument_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("docs1");
            var repo = new DocumentRepository(db);
            var fileStorageMock = new Moq.Mock<IFileStorageService>();
            var esClientMock = new Moq.Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repo, fileStorageMock.Object, esClientMock.Object);

            var res = controller.GetDocument(Guid.NewGuid(), String.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }
    }
}
