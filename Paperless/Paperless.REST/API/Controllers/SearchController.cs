using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.BLL.Search;
using Paperless.REST.DAL.DbContexts;

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
        private readonly PostgressDbContext _db;

        public SearchController(MyElasticSearchClient es, PostgressDbContext db)
        {
            _es = es;
            _db = db;
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
        public virtual async Task<IActionResult> SearchDocuments(
            [FromQuery (Name = "q")]string q, 
            [FromQuery (Name = "page")]int? page, 
            [FromQuery (Name = "pageSize")]int? pageSize, 
            [FromQuery (Name = "sort")]string? sort, 
            [FromQuery (Name = "fileType")]string? fileType, 
            [FromQuery (Name = "sizeMin")]long? sizeMin, 
            [FromQuery (Name = "sizeMax")]long? sizeMax, 
            [FromQuery (Name = "uploadDateFrom")]DateTime? uploadDateFrom, 
            [FromQuery (Name = "uploadDateTo")]DateTime? uploadDateTo, 
            [FromQuery (Name = "hasSummary")]bool? hasSummary, 
            [FromQuery (Name = "hasError")]bool? hasError, 
            [FromQuery (Name = "uploaderId")]Guid? uploaderId, 
            [FromQuery (Name = "workspaceId")]Guid? workspaceId, 
            [FromQuery (Name = "approvalStatus")]string? approvalStatus, 
            [FromQuery (Name = "shared")]bool? shared
            )
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest();
            var from = ((page ?? 1) - 1) * (pageSize ?? 20);
            var size = pageSize ?? 20;
            try
            {
                // ES liefert OCR/Summary pro Doc
                var esHits = await _es.SearchWithTextAsync(q, from, size);
                var ids = esHits.Select(h => h.DocumentId).ToList();

                // DB liefert Dateiname + UploadedAt (letzte FileVersion)
                var fileInfos = await _db.FileVersions.AsNoTracking()
                    .Where(f => ids.Contains(f.DocumentId))
                    .GroupBy(f => f.DocumentId)
                    .Select(g => new
                    {
                        documentId = g.Key,
                        fileName = g.OrderByDescending(x => x.Version).Select(x => x.OriginalFileName).FirstOrDefault(),
                        uploadedAt = g.OrderByDescending(x => x.Version).Select(x => x.UploadedAt).FirstOrDefault()
                    })
                    .ToListAsync();

                var fileMap = fileInfos.ToDictionary(x => x.documentId, x => x);

                var metaInfos = await _db.DocumentMetadatas.AsNoTracking()
                    .Where(m => ids.Contains(m.DocumentId))
                    .GroupBy(m => m.DocumentId)
                    .Select(g => new
                    {
                        documentId = g.Key,
                        title = g.OrderByDescending(x => x.Version).Select(x => x.Title).FirstOrDefault(),
                        createdAt = g.OrderByDescending(x => x.Version).Select(x => x.CreatedAt).FirstOrDefault()
                    })
                    .ToListAsync();

                var metaMap = metaInfos.ToDictionary(x => x.documentId, x => x);

                // Build result items
                var items = esHits.Select(h =>
                {
                    fileMap.TryGetValue(h.DocumentId, out var f);
                    metaMap.TryGetValue(h.DocumentId, out var m);

                    var resolvedName = f?.fileName;
                    if (string.IsNullOrWhiteSpace(resolvedName)) resolvedName = m?.title;
                    if (string.IsNullOrWhiteSpace(resolvedName)) resolvedName = "(unknown)";

                    var resolvedUploadedAt = f?.uploadedAt ?? m?.createdAt;

                    var ocrPrev = Preview(h.Ocr, int.MaxValue);
                    var sumPrev = Preview(h.Summary, int.MaxValue);

                    return new
                    {
                        id = h.DocumentId,
                        fileName = resolvedName,
                        uploadedAt = resolvedUploadedAt,
                        ocrPreview = ocrPrev,
                        summaryPreview = sumPrev,
                        hasSummary = !string.IsNullOrWhiteSpace(h.Summary)
                    };
                }).ToList();

                return Ok(new { ids, items });
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
        private static string Preview(string? text, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var cleaned = Regex.Replace(text, @"\s+", " ").Trim();
            return cleaned;
        }
    }
}
