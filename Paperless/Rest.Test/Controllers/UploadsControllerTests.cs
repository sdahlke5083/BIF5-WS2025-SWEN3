using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework.Legacy;
using Paperless.REST.API.Controllers;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Uploads.Models;

namespace Rest.Test.Controllers
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
        public async Task UploadFiles_WhenValidationFails_ReturnsBadRequestWithProblemDetails()
        {
            var files = new List<IFormFile>
            {
                CreateFormFile("bad.txt", "oops")
            };

            var uploadService = new Mock<IUploadService>();

            var failedValidation = new UploadValidationResult
            {
                AcceptedCount = 0
            };
            failedValidation.Errors.Add("Invalid file type");
            failedValidation.Errors.Add("Too large");

            uploadService
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedValidation);

            var sut = new UploadsController(uploadService.Object);

            var result = await sut.UploadFiles(files, metadata: null);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null, "Expected BadRequest when validation fails");

            var problem = bad!.Value as ProblemDetails;
            Assert.That(problem, Is.Not.Null);
            Assert.That(problem!.Title, Is.EqualTo("Upload validation failed"));
            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(problem.Detail, Does.Contain("Invalid file type").And.Contain("Too large"));

            uploadService.VerifyGet(s => s.Path, Times.Never);
        }

        [Test]
        public async Task UploadFiles_WhenValidationSucceeds_SavesFilesAndReturnsOk()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "paperless-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var files = new List<IFormFile>
                {
                    CreateFormFile("doc1.txt", "Hello 1"),
                    CreateFormFile("doc2.txt", "Hello 2")
                };

                var uploadService = new Mock<IUploadService>();

                var okValidation = new UploadValidationResult
                {
                    AcceptedCount = 2
                };

                uploadService
                    .SetupGet(s => s.Path)
                    .Returns(tempRoot);

                uploadService
                    .Setup(s => s.ValidateAsync(
                        It.IsAny<IReadOnlyCollection<UploadFile>>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(okValidation);

                var sut = new UploadsController(uploadService.Object);

                var result = await sut.UploadFiles(files, metadata: "{\"key\":\"value\"}");

                var ok = result as OkObjectResult;
                Assert.That(ok, Is.Not.Null, "Expected Ok on successful validation");

                var payload = ok!.Value!;
                var t = payload.GetType();

                var accepted = (int)t.GetProperty("accepted")!.GetValue(payload)!;
                var saved = ((IEnumerable<string>)t.GetProperty("saved")!.GetValue(payload)!).ToList();

                Assert.That(accepted, Is.EqualTo(2));
                CollectionAssert.AreEquivalent(new[] { "doc1.txt", "doc2.txt" }, saved);

                foreach (var fileName in saved)
                {
                    var path = Path.Combine(tempRoot, fileName);
                    Assert.That(File.Exists(path), Is.True, $"Expected written file: {path}");
                }

                uploadService.Verify(s => s.ValidateAsync(
                        It.IsAny<IReadOnlyCollection<UploadFile>>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public async Task UploadFiles_WithEmptyList_ReturnsOkWithZeroSaved()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "paperless-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var files = new List<IFormFile>(); // niemals null

                var uploadService = new Mock<IUploadService>();

                var okValidationEmpty = new UploadValidationResult
                {
                    AcceptedCount = 0
                };

                uploadService.SetupGet(s => s.Path).Returns(tempRoot);
                uploadService
                    .Setup(s => s.ValidateAsync(
                        It.IsAny<IReadOnlyCollection<UploadFile>>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(okValidationEmpty);

                var sut = new UploadsController(uploadService.Object);

                var result = await sut.UploadFiles(files, metadata: null);

                var ok = result as OkObjectResult;
                Assert.That(ok, Is.Not.Null);

                var payload = ok!.Value!;
                var t = payload.GetType();

                var accepted = (int)t.GetProperty("accepted")!.GetValue(payload)!;
                var saved = ((IEnumerable<string>)t.GetProperty("saved")!.GetValue(payload)!).ToList();

                Assert.That(accepted, Is.EqualTo(0));
                Assert.That(saved, Is.Empty);

                Assert.That(Directory.EnumerateFiles(tempRoot).Any(), Is.False);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
