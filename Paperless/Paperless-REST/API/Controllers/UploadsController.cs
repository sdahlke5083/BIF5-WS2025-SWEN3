using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.Controllers
{
    /// <summary>
    /// Create new documents by uploading one or more files.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

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
        [Route("/v1/upload")]
        [Authorize(Policy = "apiKeyWorker")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public virtual IActionResult UploadFiles([FromForm(Name = "files")][Required()] List<System.IO.Stream> files, [FromForm(Name = "metadata")] string metadata)
        {
            _logger.Info("Received {FileCount} files for upload.", files.Count);
            return StatusCode(201, default);
        }
    }
}
