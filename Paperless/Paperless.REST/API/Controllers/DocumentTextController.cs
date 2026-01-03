using Microsoft.AspNetCore.Mvc;
using Paperless.REST.BLL.Search;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Controllers
{
    [ApiController]
    public class DocumentTextController : ControllerBase
    {
        private readonly MyElasticSearchClient _es;

        public DocumentTextController(MyElasticSearchClient es)
        {
            _es = es;
        }

        [HttpGet]
        [Route("/v1/documents/{id}/text")]
        public async Task<IActionResult> GetDocumentText([FromRoute][Required] Guid id)
        {
            var entry = await _es.GetTextByIdAsync(id);
            if (entry is null)
                return NotFound();

            return Ok(new
            {
                documentId = id,
                ocr = entry.Ocr,
                summary = entry.Summary,
                timestamp = entry.Timestamp
            });
        }
    }
}
