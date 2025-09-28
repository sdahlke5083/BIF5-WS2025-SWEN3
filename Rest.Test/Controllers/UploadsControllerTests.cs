using System.Text;
using System.Text.Json;
using API.Controllers;
using BLL.Uploads;
using BLL.Uploads.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Rest.Test.Controllers
{
    [TestFixture]
    public class UploadsControllerTests
    {
        private static IFormFile CreateFormFile(string fileName, string contentType, string textContent)
        {
            var bytes = Encoding.UTF8.GetBytes(textContent);
            var ms = new MemoryStream(bytes);
            return new FormFile(ms, 0, bytes.Length, "files", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Test]
        public async Task UploadFiles_ReturnsOk_WithAcceptedCount_WhenValidationSucceeds()
        {
            var files = new List<IFormFile>
            {
                CreateFormFile("a.pdf", "application/pdf", "pdfcontent"),
                CreateFormFile("b.png", "image/png", "imgcontent")
            };
            var metadata = """{ "tags": ["x","y"] }""";

            var validation = new UploadValidationResult { AcceptedCount = files.Count };
            var serviceMock = new Mock<IUploadService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    metadata,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(validation);

            var controller = new UploadsController(serviceMock.Object);
            var http = new DefaultHttpContext();
            http.Request.Scheme = "http";
            http.Request.Host = new HostString("localhost");
            http.Request.Path = "/v1/uploads";
            controller.ControllerContext = new ControllerContext { HttpContext = http };

            var result = await controller.UploadFiles(files, metadata, CancellationToken.None);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Expected OkObjectResult");
            Assert.That(ok!.Value, Is.Not.Null);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var accepted = doc.RootElement.GetProperty("accepted").GetInt32();
            Assert.That(accepted, Is.EqualTo(files.Count));

            serviceMock.VerifyAll();
        }

        [Test]
        public async Task UploadFiles_ReturnsBadRequest_WithAggregatedErrors_WhenValidationFails()
        {
            var files = new List<IFormFile> { CreateFormFile("a.pdf", "application/pdf", "x") };
            var metadata = """{ "tags": 123 }""";

            var validation = new UploadValidationResult();
            validation.Errors.Add("Invalid metadata");
            validation.Errors.Add("Unsupported file type");

            var serviceMock = new Mock<IUploadService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    metadata,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(validation);

            var controller = new UploadsController(serviceMock.Object);
            var http = new DefaultHttpContext();
            http.Request.Scheme = "http";
            http.Request.Host = new HostString("localhost");
            http.Request.Path = "/v1/uploads";
            controller.ControllerContext = new ControllerContext { HttpContext = http };

            var result = await controller.UploadFiles(files, metadata, CancellationToken.None);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null, "Expected BadRequestObjectResult");

            var problem = bad!.Value as ProblemDetails;
            Assert.That(problem, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(problem!.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
                Assert.That(problem.Title, Is.EqualTo("Upload validation failed"));
                Assert.That(problem.Instance, Is.EqualTo("/v1/uploads"));
                Assert.That(problem.Detail, Does.Contain("Invalid metadata"));
                Assert.That(problem.Detail, Does.Contain("Unsupported file type"));
                Assert.That(problem.Detail, Does.Contain(" | "));
            });

            serviceMock.VerifyAll();
        }

        [Test]
        public async Task UploadFiles_ForwardsCorrectFileInfos_AndToken_ToService()
        {
            var files = new List<IFormFile>
            {
                CreateFormFile("a.pdf", "application/pdf", "x"),
                CreateFormFile("b.jpg", "image/jpeg", "y")
            };
            var metadata = """{ "lang": "de" }""";
            using var cts = new CancellationTokenSource();
            var expectedToken = cts.Token;

            IReadOnlyCollection<UploadFile>? capturedFiles = null;
            string? capturedMetadata = null;
            CancellationToken capturedToken = default;

            var validation = new UploadValidationResult { AcceptedCount = 2 };

            var serviceMock = new Mock<IUploadService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<IReadOnlyCollection<UploadFile>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<UploadFile>, string?, CancellationToken>((f, m, t) =>
                {
                    capturedFiles = f;
                    capturedMetadata = m;
                    capturedToken = t;
                })
                .ReturnsAsync(validation);

            var controller = new UploadsController(serviceMock.Object);
            var http = new DefaultHttpContext();
            http.Request.Scheme = "http";
            http.Request.Host = new HostString("localhost");
            http.Request.Path = "/v1/uploads";
            controller.ControllerContext = new ControllerContext { HttpContext = http };

            var result = await controller.UploadFiles(files, metadata, expectedToken);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(capturedMetadata, Is.EqualTo(metadata));
            Assert.That(capturedToken, Is.EqualTo(expectedToken));
            Assert.That(capturedFiles, Is.Not.Null);
            Assert.That(capturedFiles!.Count, Is.EqualTo(2));
            Assert.That(capturedFiles.Any(u => u.FileName == "a.pdf" && u.ContentType == "application/pdf"), Is.True);
            Assert.That(capturedFiles.Any(u => u.FileName == "b.jpg" && u.ContentType == "image/jpeg"), Is.True);

            serviceMock.VerifyAll();
        }
    }
}
