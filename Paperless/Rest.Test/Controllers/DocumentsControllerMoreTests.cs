using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.REST.API.Controllers;
using Paperless.REST.BLL.Search;
using Paperless.REST.BLL.Storage;
using Paperless.REST.DAL.Exceptions;
using Paperless.REST.DAL.Models;
using Paperless.REST.DAL.Repositories;
using System.Text.Json;

namespace Paperless.REST.Test.Controllers
{
    public class DocumentsControllerMoreTests
    {
        [Test]
        public void DeleteDocument_DataAccessException_Returns_NotFound()
        {
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataAccessException("not found"));
            var db = TestUtils.CreateInMemoryDb("docdel1");
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DeleteDocument(Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void DownloadFile_NoDocument_Returns_NotFound()
        {
            var db = TestUtils.CreateInMemoryDb("docdown1");
            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DownloadFile(Guid.NewGuid(), string.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task ListSummaries_Returns_Ok()
        {
            var db = TestUtils.CreateInMemoryDb("listSumm1");
            // add a document and a summary
            var doc = new Document { Id = Guid.NewGuid() };
            db.Documents.Add(doc);
            var summary = new Summary
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                Content = "test",
                CreatedAt = DateTimeOffset.UtcNow,
                LengthPresetId = Guid.NewGuid()
            };
            db.DocumentSummaries.Add(summary);
            db.SaveChanges();

            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

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
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var json = new JsonElement();
            // ensure NotFound when body is not an object
            var result = controller.PatchDocument(Guid.NewGuid(), json, string.Empty) as NotFoundResult;
            Assert.That(result, Is.Not.Null);
        }
    }
}
