using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Search and filter documents.
    /// </summary>
    [ApiController]
    //public class SearchController : ControllerBase
    public class SearchController : BaseApiController
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        ///// <summary>
        ///// List documents with optional filters (page-based)
        ///// </summary>
        ///// <param name="q">Free-text query</param>
        ///// <param name="page"></param>
        ///// <param name="pageSize">Number of items per page (default 20; max 100 supported server-side).</param>
        ///// <param name="sort"></param>
        ///// <param name="fileType"></param>
        ///// <param name="sizeMin"></param>
        ///// <param name="sizeMax"></param>
        ///// <param name="uploadDateFrom"></param>
        ///// <param name="uploadDateTo"></param>
        ///// <param name="hasSummary"></param>
        ///// <param name="hasError"></param>
        ///// <param name="uploaderId"></param>
        ///// <param name="workspaceId"></param>
        ///// <param name="approvalStatus"></param>
        ///// <param name="shared"></param>
        ///// <response code="200">Paged result</response>
        //[HttpGet]
        //[Route("/v1/documents")]
        ////[Authorize]
        ////[ProducesResponseType(statusCode: 200, type: typeof(DocumentPage))]
        //public virtual IActionResult ListDocuments([FromQuery (Name = "q")]string q, [FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize, [FromQuery (Name = "sort")]string sort, [FromQuery (Name = "fileType")]string fileType, [FromQuery (Name = "sizeMin")]long? sizeMin, [FromQuery (Name = "sizeMax")]long? sizeMax, [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, [FromQuery (Name = "hasSummary")]bool? hasSummary, [FromQuery (Name = "hasError")]bool? hasError, [FromQuery (Name = "uploaderId")]Guid? uploaderId, [FromQuery (Name = "workspaceId")]Guid? workspaceId, [FromQuery (Name = "approvalStatus")]string approvalStatus, [FromQuery (Name = "shared")]bool? shared)
        //{

        //    //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
        //    // return StatusCode(200, default);
        //    //TODO: Change the data returned
        //    return NotImplementedStub();
        //}

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
        public virtual IActionResult SearchDocuments([FromQuery (Name = "q")]string q, [FromQuery (Name = "page")]int? page, [FromQuery (Name = "pageSize")]int? pageSize, [FromQuery (Name = "sort")]string sort, [FromQuery (Name = "fileType")]string fileType, [FromQuery (Name = "sizeMin")]long? sizeMin, [FromQuery (Name = "sizeMax")]long? sizeMax, [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, [FromQuery (Name = "hasSummary")]bool? hasSummary, [FromQuery (Name = "hasError")]bool? hasError, [FromQuery (Name = "uploaderId")]Guid? uploaderId, [FromQuery (Name = "workspaceId")]Guid? workspaceId, [FromQuery (Name = "approvalStatus")]string approvalStatus, [FromQuery (Name = "shared")]bool? shared)
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Change the data returned
            return NotImplementedStub();
        }
    }
}
