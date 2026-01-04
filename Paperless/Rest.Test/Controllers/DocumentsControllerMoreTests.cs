using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Controllers;
using Paperless.REST.DAL.Repositories;
using Paperless.REST.DAL.Exceptions;
using Paperless.REST.BLL.Storage;

namespace Paperless.REST.Test.Controllers
{
    public class DocumentsControllerMoreTests
    {
        [Test]
        public void DeleteDocument_DataAccessException_Returns_NotFound()
        {
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.DeleteAsync(It.IsAny<System.Guid>(), It.IsAny<System.Threading.CancellationToken>())).ThrowsAsync(new DataAccessException("not found"));
            var db = TestUtils.CreateInMemoryDb("docdel1");
            var fileStorageMock = new Mock<IFileStorageService>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object);

            var res = controller.DeleteDocument(System.Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void DownloadFile_NoDocument_Returns_NotFound()
        {
            var db = TestUtils.CreateInMemoryDb("docdown1");
            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object);

            var res = controller.DownloadFile(System.Guid.NewGuid(), string.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async System.Threading.Tasks.Task ListSummaries_Returns_Ok()
        {
            var db = TestUtils.CreateInMemoryDb("listSumm1");
            // add a document and a summary
            var doc = new Paperless.REST.DAL.Models.Document { Id = System.Guid.NewGuid() };
            db.Documents.Add(doc);
            var summary = new Paperless.REST.DAL.Models.Summary { Id = System.Guid.NewGuid(), DocumentId = doc.Id, Content = "test", CreatedAt = System.DateTimeOffset.UtcNow, LengthPresetId = System.Guid.NewGuid() };
            db.DocumentSummaries.Add(summary);
            db.SaveChanges();

            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object);

            var res = await controller.ListSummaries(doc.Id) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void PatchDocument_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("patch1");
            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object);

            var json = new System.Text.Json.JsonElement();
            // ensure NotFound when body is not an object
            var result = controller.PatchDocument(System.Guid.NewGuid(), json, String.Empty) as NotFoundResult;
            Assert.That(result, Is.Not.Null);
        }
    }
}
