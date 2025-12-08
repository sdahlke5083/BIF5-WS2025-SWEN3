using Microsoft.AspNetCore.Mvc;
using Paperless.REST.BLL.Search;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Search and filter documents.
    /// </summary>
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly MyElasticSearchClient _es;

        public SearchController(MyElasticSearchClient es)
        {
            _es = es;
        }

        /// <summary>
        /// Search documents (free-text + filters)
        /// </summary>
        /// <param name="q">Free-text query</param>
        /// <param name="page"></param>
        /// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        /// <param name="sort"></param>
        /// <param name="fileType"></param>
        /// <param name="sizeMin"></param>
        /// <param name="sizeMax"></param>
        /// <param name="uploadDateFrom"></param>
        /// <param name="uploadDateTo"></param>
        /// <param name="hasSummary"></param>
        /// <param name="hasError"></param>
        /// <param name="uploaderId"></param>
        /// <param name="workspaceId"></param>
        /// <param name="approvalStatus"></param>
        /// <param name="shared"></param>
        /// <response code="200">Paged search results</response>
        [HttpGet]
        [Route("/v1/search")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(DocumentPage))]
        public virtual async Task<IActionResult> SearchDocuments([FromQuery (Name = "q")]string q, [FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize, [FromQuery (Name = "sort")]string? sort, [FromQuery (Name = "fileType")]string? fileType, [FromQuery (Name = "sizeMin")]long? sizeMin, [FromQuery (Name = "sizeMax")]long? sizeMax, [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, [FromQuery (Name = "hasSummary")]bool? hasSummary, [FromQuery (Name = "hasError")]bool? hasError, [FromQuery (Name = "uploaderId")]Guid? uploaderId, [FromQuery (Name = "workspaceId")]Guid? workspaceId, [FromQuery (Name = "approvalStatus")]string? approvalStatus, [FromQuery (Name = "shared")]bool? shared)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest();
            var from = ((page ?? 1) - 1) * (pageSize ?? 20);
            var size = pageSize ?? 20;
            try
            {
                var ids = await _es.SearchAsync(q, from, size);
                return Ok(new { ids = ids });
            }
            catch (InvalidOperationException ioe)
            {
                _logger.Error(ioe, "Elasticsearch request failed");
                return Problem(detail: ioe.Message, statusCode: 502, title: "Bad Gateway");
            }
            catch (HttpRequestException hre)
            {
                _logger.Error(hre, "Elasticsearch HTTP error");
                return Problem(detail: hre.Message, statusCode: 502, title: "Bad Gateway");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Elasticsearch search failed");
                return Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while processing your request.");
            }
        }
    }
}
