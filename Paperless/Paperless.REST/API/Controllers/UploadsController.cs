using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Attributes;
using Paperless.REST.API.Models.BaseResponse;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadsController"/> class.
        /// </summary>
        /// <param name="uploadService">The service responsible for handling upload operations. This parameter cannot be null.</param>
        public UploadsController(IUploadService uploadService)
        {
            _uploadService = uploadService;
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

            var validation = await _uploadService.ValidateAsync(fileInfos, metadata, cancelToken);

            if(validation.Success is false)
            {
                var problemResponse = new ProblemResponse 
                { 
                    Title = "Upload validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = string.Join(" | ", validation.Errors),
                    Instance = HttpContext?.Request.Path ?? String.Empty
                };
                return BadRequest(problemResponse);
            }

            _logger.Debug($"Number of Uploads accepted: {validation.AcceptedCount} file(s)");
            var basePath = _uploadService.Path;
            Directory.CreateDirectory(basePath); // Ensure the directory exists
            var saved = new List<string>();
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    var targetPath = Path.Combine(basePath, formFile.FileName);
                    await using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await formFile.CopyToAsync(stream, cancelToken);
                    }
                    saved.Add(Path.GetFileName(targetPath));
                    _logger.Info($"Saved file '{formFile.FileName}' as '{targetPath}'");
                }
            }
            return Ok(new { accepted = validation.AcceptedCount , saved, guids = validation.DocumentIds }); // Return HTTP 200
        }
    }
}
