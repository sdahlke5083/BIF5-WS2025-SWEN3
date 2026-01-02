using Microsoft.EntityFrameworkCore;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Models;

namespace Paperless.REST.Test
{
    internal static class TestUtils
    {
        public static PostgressDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<PostgressDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var db = new PostgressDbContext(options);

            // seed minimal data
            if (!db.Roles.Any())
            {
                var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
                db.Roles.Add(adminRole);
                var user = new User { Id = Guid.NewGuid(), Username = "test", DisplayName = "Test User", Password = Paperless.REST.BLL.Security.PasswordHasher.Hash("password"), MustChangePassword = false };
                db.Users.Add(user);
                db.UserRoles.Add(new UserRole { RoleId = adminRole.Id, UserId = user.Id });
                db.SaveChanges();
            }

            return db;
        }
    }
}
