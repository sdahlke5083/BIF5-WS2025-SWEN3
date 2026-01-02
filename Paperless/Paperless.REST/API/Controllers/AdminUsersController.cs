using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Paperless.REST.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly PostgressDbContext _db;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public AdminUsersController(PostgressDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("/v1/admin/users")]
        public IActionResult ListUsers()
        {
            var users = _db.Users.AsNoTracking().Select(u => new { id = u.Id, username = u.Username, displayName = u.DisplayName }).ToList();
            return Ok(users);
        }

        [HttpPatch]
        [Route("/v1/admin/users/{id}")]
        public IActionResult UpdateUser([FromRoute] Guid id, [FromBody] UserProfileUpdateRequest req)
        {
            var u = _db.Users.FirstOrDefault(x => x.Id == id);
            if (u is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.DisplayName)) u.DisplayName = req.DisplayName!;
            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                var errors = Paperless.REST.BLL.Security.PasswordValidator.Validate(req.Password!);
                if (errors.Count > 0)
                    return BadRequest(new { errors });

                u.Password = Paperless.REST.BLL.Security.PasswordHasher.Hash(req.Password!);
                u.MustChangePassword = false;
            }

            _db.SaveChanges();
            return Ok(new { id = u.Id });
        }
    }
}
