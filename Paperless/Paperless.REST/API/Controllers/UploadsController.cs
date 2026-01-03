using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.API.Attributes;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Storage;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Uploads.Models;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Models;

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
        private readonly PostgressDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadsController"/> class.
        /// </summary>
        /// <param name="uploadService">The service responsible for handling upload operations. This parameter cannot be null.</param>
        /// <param name="fileStorage">Optional file storage. If not provided, controller falls back to filesystem writes using IUploadService.Path.</param>
        public UploadsController(IUploadService uploadService, IFileStorageService fileStorageService, PostgressDbContext db)
        {
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _db = db ?? throw new ArgumentNullException(nameof(db));
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

            // Speicherung in MinIO via IFileStorageService
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
                // Prefer streaming upload if supported
                await _fileStorageService.SaveFileAsync(
                    objectName,
                    stream,
                    formFile.Length,
                    formFile.ContentType,
                    cancelToken).ConfigureAwait(false);

                await UpsertFileVersionAsync(
                    documentId: documentId,
                    originalFileName: originalFileName,
                    storedObjectName: objectName,
                    contentType: formFile.ContentType,
                    sizeBytes: formFile.Length,
                    cancelToken: cancelToken).ConfigureAwait(false);

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
        private async Task UpsertFileVersionAsync(
            Guid documentId,
            string originalFileName,
            string storedObjectName,
            string? contentType,
            long sizeBytes,
            CancellationToken cancelToken
            )
        {
            var doc = await _db.Documents
                .Include(d => d.FileVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId, cancelToken)
                .ConfigureAwait(false);

            if (doc is null)
            {
                _logger.Warn($"Upload: document '{documentId}' not found while creating FileVersion.");
                return;
            }

            var nextVersion = doc.FileVersions.Count == 0
                ? 1
                : doc.FileVersions.Max(f => f.Version) + 1;

            var ext = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";
            ext = ext.StartsWith('.') ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant();

            var mime = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

            var fileType = await _db.FileTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(ft => ft.MimeType == mime || ft.FileExtension == ext, cancelToken)
                .ConfigureAwait(false);

            if (fileType is null)
            {
                fileType = new DocumentFileType
                {
                    Id = Guid.NewGuid(),
                    DisplayName = $"{ext.Trim('.').ToUpperInvariant()} File",
                    MimeType = mime,
                    FileExtension = ext
                };
                _db.FileTypes.Add(fileType);
                await _db.SaveChangesAsync(cancelToken).ConfigureAwait(false);
            }

            var fv = new FileVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Version = nextVersion,
                OriginalFileName = originalFileName,
                StoredName = storedObjectName,
                SizeBytes = sizeBytes,
                FileTypeId = fileType.Id,
                UploadedAt = DateTimeOffset.UtcNow,
                UploadedByUserId = null
            };

            _db.FileVersions.Add(fv);
            doc.CurrentFileVersion = nextVersion;
            await _db.SaveChangesAsync(cancelToken).ConfigureAwait(false);
        }
    }
}
