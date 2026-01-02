using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework.Legacy;
using Paperless.REST.API.Controllers;
using Paperless.REST.BLL.Storage;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Uploads.Models;

namespace Paperless.REST.Test.Controllers
{
    [TestFixture]
    public class UploadsControllerTests
    {
        private static IFormFile CreateFormFile(string fileName, string content, string contentType = "text/plain")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, stream.Length, "files", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Test]
        public async Task UploadFiles_WhenValidationSucceeds_SavesFilesAndReturnsOk()
        {
            var uploadService = new Mock<IUploadService>();
            var fileStorage = new Mock<IFileStorageService>();

            var validation = new UploadValidationResult
            {
                AcceptedCount = 2,
                DocumentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            uploadService
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(validation);

            fileStorage
                .Setup(s => s.SaveFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string objectName, Stream _, long _, string? _, CancellationToken _) => objectName);

            var sut = new UploadsController(uploadService.Object, fileStorage.Object);

            var files = new List<IFormFile>
            {
                CreateFormFile("doc1.txt", "Hello 1"),
                CreateFormFile("doc2.txt", "Hello 2")
            };

            var result = await sut.UploadFiles(files, metadata: "{\"key\":\"value\"}");

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Expected Ok on successful validation");

            var payload = ok!.Value!;
            var t = payload.GetType();

            var accepted = (int)t.GetProperty("accepted")!.GetValue(payload)!;
            var saved = ((IEnumerable<string>)t.GetProperty("saved")!.GetValue(payload)!).ToList();
            var guids = (IEnumerable<Guid>?)t.GetProperty("guids")!.GetValue(payload);

            Assert.That(accepted, Is.EqualTo(2));
            Assert.That(saved, Is.EquivalentTo(new[] { "doc1.txt", "doc2.txt" }));
            Assert.That(guids, Is.Not.Null);

            fileStorage.Verify(s => s.SaveFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            uploadService.Verify(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UploadFiles_WithEmptyList_ReturnsOkWithZeroSaved()
        {
            var uploadService = new Mock<IUploadService>();
            var fileStorage = new Mock<IFileStorageService>();

            var validation = new UploadValidationResult
            {
                AcceptedCount = 0,
                DocumentIds = new List<Guid>()
            };

            uploadService
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(validation);

            var sut = new UploadsController(uploadService.Object, fileStorage.Object);

            var files = new List<IFormFile>(); // leere Liste

            var result = await sut.UploadFiles(files, metadata: null);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Expected Ok for empty list when validation succeeds");

            var payload = ok!.Value!;
            var t = payload.GetType();

            var accepted = (int)t.GetProperty("accepted")!.GetValue(payload)!;
            var saved = ((IEnumerable<string>)t.GetProperty("saved")!.GetValue(payload)!).ToList();

            Assert.That(accepted, Is.EqualTo(0));
            Assert.That(saved, Is.Empty);

            fileStorage.Verify(s => s.SaveFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
