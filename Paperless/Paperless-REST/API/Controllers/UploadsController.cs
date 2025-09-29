using Microsoft.AspNetCore.Mvc;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Uploads.Models;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Create new documents by uploading one or more files.
    /// </summary>
    [ApiController]
    //public class UploadsController : ControllerBase
    public class UploadsController : BaseApiController
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUploadService _uploadService;

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
        //[ValidateModelState]
        //[ProducesResponseType(statusCode: 201, type: typeof(UploadResponse))]
        //[ProducesResponseType(statusCode: 400, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 401, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 413, type: typeof(Problem))]
        //[ProducesResponseType(statusCode: 500, type: typeof(Problem))]
        public async Task<IActionResult> UploadFiles(
            [FromForm(Name = "files")][Required] List<IFormFile> files,
            [FromForm(Name = "metadata")] string? metadata,
            CancellationToken cancelToken)
        {
            var fileInfos = (files ?? new List<IFormFile>())
                .Select(f => new UploadFile(f.FileName, f.ContentType, f.Length))
                .ToList()
                .AsReadOnly();

            _logger.Info($"Received {fileInfos.Count} file(s) at /v1/uploads");

            var validation = await _uploadService.ValidateAsync(fileInfos, metadata, cancelToken);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default);
            if(validation.Success is false)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Upload validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = string.Join(" | ", validation.Errors),
                    Instance = HttpContext?.Request.Path
                });
            }
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default);
            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default);
            //TODO: Uncomment the next line to return response 413 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(413, default);
            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default);
            //TODO: Change the data returned

            _logger.Info($"Number of Uploads accepted: {validation.AcceptedCount} file(s)");
            return Ok(new { accepted = validation.AcceptedCount, guids = validation.DocumentIds }); // Return HTTP 200
        }
    }
}
