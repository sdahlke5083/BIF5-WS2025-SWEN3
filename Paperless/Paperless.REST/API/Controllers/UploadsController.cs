using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Attributes;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Storage;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Uploads.Models;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Create new documents by uploading one or more files.
    /// </summary>
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUploadService _uploadService;
        private readonly IFileStorageService? _fileStorageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadsController"/> class.
        /// </summary>
        /// <param name="uploadService">The service responsible for handling upload operations. This parameter cannot be null.</param>
        /// <param name="fileStorage">Optional file storage. If not provided, controller falls back to filesystem writes using IUploadService.Path.</param>
        public UploadsController(IUploadService uploadService, IFileStorageService fileStorageService)
        {
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        }

        /// <summary>
        /// Upload one or more files and create document records
        /// </summary>
        /// <param name="files">One or more files to upload (pdf, docx, images, etc.).</param>
        /// <param name="metadata">Optional JSON object mapping original filenames to metadata. Example: {   \\\&quot;scan1.pdf\\\&quot;: {\\\&quot;title\\\&quot;:\\\&quot;March invoice\\\&quot;,\\\&quot;tags\\\&quot;:[\\\&quot;invoice\\\&quot;,\\\&quot;2025-03\\\&quot;],\\\&quot;lang\\\&quot;:\\\&quot;de\\\&quot;},   \\\&quot;contract.docx\\\&quot;: {\\\&quot;title\\\&quot;:\\\&quot;NDA\\\&quot;,\\\&quot;tags\\\&quot;:[\\\&quot;contract\\\&quot;],\\\&quot;lang\\\&quot;:\\\&quot;en\\\&quot;} } </param>
        /// <response code="201">Documents created</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="413">Payload too large</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Route("/v1/uploads")]
        //[Authorize(Policy = "apiKeyWorker")]
        //[Authorize]
        [Consumes("multipart/form-data")]
        [ValidateModelState]
        //[ProducesResponseType(statusCode: 201, type: typeof(UploadResponse))]
        [ProducesResponseType(statusCode: 400, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 413, type: typeof(ProblemResponse))]
        [ProducesResponseType(statusCode: 500, type: typeof(ProblemResponse))]
        public async Task<IActionResult> UploadFiles(
            [FromForm(Name = "files")][Required] List<IFormFile> files,
            [FromForm(Name = "metadata")] string? metadata)
        {
            using var cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;

            var fileInfos = (files ?? new List<IFormFile>())
                .Select(f => new UploadFile(f.FileName, f.ContentType, f.Length))
                .ToList()
                .AsReadOnly();

            _logger.Debug($"Received {fileInfos.Count} file(s) at /v1/uploads");

            var validation = await _uploadService
                .ValidateAsync(fileInfos, metadata, cancelToken)
                .ConfigureAwait(false);

            if (!validation.Success)
            {
                var problemResponse = new ProblemResponse
                {
                    Title = "Upload validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = string.Join(" | ", validation.Errors),
                    Instance = HttpContext?.Request.Path ?? string.Empty
                };

                _logger.Warn($"Upload validation failed: {problemResponse.Detail}");
                return BadRequest(problemResponse);
            }

            _logger.Debug($"Number of uploads accepted: {validation.AcceptedCount} file(s)");

            var saved = new List<string>();

            // NEU: Speicherung in MinIO via IFileStorageService
            for (var index = 0; index < files.Count; index++)
            {
                var formFile = files[index];

                if (formFile.Length <= 0)
                    continue;

                var originalFileName = Path.GetFileName(formFile.FileName);

                // Dokument-ID (wenn vorhanden aus Validation) für den Objektpfad verwenden
                Guid documentId = Guid.NewGuid();
                if (validation.DocumentIds is { Count: > 0 } && index < validation.DocumentIds.Count)
                {
                    documentId = validation.DocumentIds[index];
                }

                var objectName = $"{documentId:D}/{originalFileName}";

                await using var stream = formFile.OpenReadStream();
                await _fileStorageService.SaveFileAsync(
                    objectName,
                    stream,
                    formFile.Length,
                    formFile.ContentType,
                    cancelToken).ConfigureAwait(false);

                saved.Add(originalFileName);
                _logger.Info($"Uploaded file '{originalFileName}' as object '{objectName}' in MinIO.");
            }

            return Ok(new
            {
                accepted = validation.AcceptedCount,
                saved,
                guids = validation.DocumentIds
            });
        }
    }
}
