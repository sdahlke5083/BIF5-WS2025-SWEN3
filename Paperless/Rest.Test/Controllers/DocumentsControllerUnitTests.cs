using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.REST.API.Controllers;
using Paperless.REST.BLL.Search;
using Paperless.REST.BLL.Storage;
using Paperless.REST.DAL.Exceptions;
using Paperless.REST.DAL.Models;
using Paperless.REST.DAL.Repositories;
using System.Text;

namespace Paperless.REST.Test.Controllers
{
    public class DocumentsControllerUnitTests
    {
        [Test]
        public void DeleteDocument_Success_Returns_NoContent()
        {
            var db = TestUtils.CreateInMemoryDb("del_ok");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DeleteDocument(Guid.NewGuid()) as NoContentResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(204));
        }

        [Test]
        public void DeleteDocument_DataAccessException_Returns_NotFound()
        {
            var db = TestUtils.CreateInMemoryDb("del_notfound");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new DataAccessException("not found"));
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DeleteDocument(Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void DownloadFile_DocumentOrFileMissing_Returns_NotFound()
        {
            var db = TestUtils.CreateInMemoryDb("down_nf");
            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DownloadFile(Guid.NewGuid(), String.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void DownloadFile_FileExists_Returns_FileResult()
        {
            var db = TestUtils.CreateInMemoryDb("down_ok");

            // prepare a document with fileversion and filetype
            var ft = new DocumentFileType { Id = Guid.NewGuid(), DisplayName = "PDF", MimeType = "application/pdf", FileExtension = ".pdf" };
            db.FileTypes.Add(ft);

            var doc = new Document { Id = Guid.NewGuid() };
            db.Documents.Add(doc);

            var fv = new FileVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                Version = 1,
                OriginalFileName = "a.pdf",
                StoredName = String.Empty,
                SizeBytes = 10,
                FileTypeId = ft.Id,
                UploadedAt = DateTimeOffset.UtcNow
            };
            db.FileVersions.Add(fv);
            db.SaveChanges();

            var repoMock = new Mock<IDocumentRepository>();
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var mem = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            fileStorageMock.Setup(f => f.OpenReadStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mem);

            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.DownloadFile(doc.Id, String.Empty) as FileStreamResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.ContentType, Is.EqualTo("application/pdf"));
        }

        [Test]
        public void GetDocument_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("get_nf");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Document?)null);
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.GetDocument(Guid.NewGuid(), String.Empty) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void GetDocument_Found_Returns_Ok()
        {
            var db = TestUtils.CreateInMemoryDb("get_ok");
            var doc = new Document { Id = Guid.NewGuid(), CurrentMetadataVersion = 1, CurrentFileVersion = 1 };
            var meta = new DocumentMetadata { DocumentId = doc.Id, Version = 1, Title = "T1", CreatedAt = DateTimeOffset.UtcNow };
            doc.MetadataVersions.Add(meta);
            db.Documents.Add(doc);
            db.DocumentMetadatas.Add(meta);
            db.SaveChanges();

            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.GetDocument(doc.Id, String.Empty) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void ListDocuments_Returns_Ok()
        {
            var db = TestUtils.CreateInMemoryDb("list_docs");
            var doc = new Document { Id = Guid.NewGuid() };
            db.Documents.Add(doc);
            // ensure metadata and fileversion exist to avoid nulls in projection
            var meta = new DocumentMetadata { DocumentId = doc.Id, Version = 1, Title = "Title1", CreatedAt = DateTimeOffset.UtcNow };
            var ft = new DocumentFileType { Id = Guid.NewGuid(), DisplayName = "PDF", MimeType = "application/pdf", FileExtension = ".pdf" };
            db.DocumentMetadatas.Add(meta);
            db.FileTypes.Add(ft);
            var fv = new FileVersion { Id = Guid.NewGuid(), DocumentId = doc.Id, Version = 1, OriginalFileName = "a.pdf", StoredName = "a.pdf", UploadedAt = DateTimeOffset.UtcNow, SizeBytes = 123, FileTypeId = ft.Id };
            db.FileVersions.Add(fv);
            db.SaveChanges();

            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Document> { doc });
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.ListDocuments(String.Empty, 1, 20, String.Empty, String.Empty, null, null, null, null, null, null, null, null, String.Empty, null) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void ListDeleted_Returns_Ok()
        {
            var db = TestUtils.CreateInMemoryDb("list_del");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.GetAllDeleted(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Document>());
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.ListDeleted(null, null) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void PurgeDocument_NotFound_Returns_404()
        {
            var db = TestUtils.CreateInMemoryDb("purge_nf");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.PermanentlyDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new DataAccessException("no"));
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.PurgeDocument(Guid.NewGuid()) as NotFoundResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void RestoreDocument_Success_Returns_NoContent()
        {
            var db = TestUtils.CreateInMemoryDb("restore_ok");
            var repoMock = new Mock<IDocumentRepository>();
            repoMock.Setup(r => r.RestoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var fileStorageMock = new Mock<IFileStorageService>();
            var esClientMock = new Mock<MyElasticSearchClient>();
            var controller = new DocumentsController(db, repoMock.Object, fileStorageMock.Object, esClientMock.Object);

            var res = controller.RestoreDocument(Guid.NewGuid()) as NoContentResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(204));
        }
    }
}
