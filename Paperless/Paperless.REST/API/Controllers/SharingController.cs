using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Paperless.REST.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Create and manage password-protected share links for guests.
    /// </summary>
    [ApiController]
    public class SharingController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly PostgressDbContext _db;

        public SharingController(PostgressDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Create a password-protected share link
        /// </summary>
        /// <param name="id"></param>
        /// <param name="shareCreate"></param>
        /// <response code="201">Created</response>
        [HttpPost]
        [Route("/v1/documents/{id}/shares")]
        [Authorize]
        [Consumes("application/json")]
        //[ProducesResponseType(statusCode: 201, type: typeof(Share))]
        public virtual IActionResult CreateShare([FromRoute (Name = "id")][Required]Guid id, [FromBody]JsonElement body)
        {
            if (!_db.Documents.Any(d => d.Id == id))
                return NotFound();

            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest();

            var token = Guid.NewGuid().ToString("N");
            string? passwordHash = null;
            if (body.TryGetProperty("password", out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()))
            {
                // Hash password using Rfc2898DeriveBytes (PBKDF2)
                var pwd = p.GetString()!;
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                var salt = new byte[16];
                rng.GetBytes(salt);
                using var derive = new System.Security.Cryptography.Rfc2898DeriveBytes(pwd, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                var hash = derive.GetBytes(32);
                var combined = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);
                passwordHash = Convert.ToBase64String(combined);
            }

            var expires = body.TryGetProperty("expiresAt", out var ex) && ex.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ex.GetString(), out var dt) ? dt : (DateTimeOffset?)null;

            var share = new DAL.Models.Share
            {
                DocumentId = id,
                Token = token,
                PasswordHash = passwordHash,
                ExpiresAt = expires
            };

            _db.Add(share);
            _db.SaveChanges();

            return Created($"/v1/shares/{share.Id}", new { id = share.Id, token = share.Token });
        }

        /// <summary>
        /// Revoke a share
        /// </summary>
        /// <param name="shareId"></param>
        /// <response code="204">Revoked</response>
        [HttpDelete]
        [Route("/v1/shares/{shareId}")]
        //[Authorize]
        public virtual IActionResult DeleteShare([FromRoute (Name = "shareId")][Required]Guid shareId)
        {
            var s = _db.Set<DAL.Models.Share>().FirstOrDefault(sh => sh.Id == shareId);
            if (s is null)
                return NotFound();

            _db.Set<DAL.Models.Share>().Remove(s);
            _db.SaveChanges();
            return NoContent();
        }

        /// <summary>
        /// Get a share configuration
        /// </summary>
        /// <param name="shareId"></param>
        /// <response code="200">OK</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/v1/shares/{shareId}")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(Share))]
        //[ProducesResponseType(statusCode: 404, type: typeof(Problem))]
        public virtual IActionResult GetShare([FromRoute (Name = "shareId")][Required]Guid shareId)
        {
            var s = _db.Set<DAL.Models.Share>().FirstOrDefault(sh => sh.Id == shareId);
            if (s is null) return NotFound();
            return Ok(new { id = s.Id, token = s.Token, expiresAt = s.ExpiresAt, documentId = s.DocumentId });
        }

        /// <summary>
        /// List share links for a document
        /// </summary>
        /// <param name="id"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/v1/documents/{id}/shares")]
        //[Authorize]
        //[ProducesResponseType(statusCode: 200, type: typeof(ListShares200Response))]
        public virtual IActionResult ListShares([FromRoute (Name = "id")][Required]Guid id)
        {
            var list = _db.Set<DAL.Models.Share>().Where(s => s.DocumentId == id).Select(s => new { id = s.Id, token = s.Token, expiresAt = s.ExpiresAt }).ToList();
            return Ok(list);
        }
    }
}
