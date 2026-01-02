using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Cryptography;

#nullable disable

namespace Paperless.REST.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create a seeded admin user with hashed password (password: "admin")
            var userId = Guid.NewGuid();
            var username = "admin";
            var displayName = "Administrator";
            var passwordPlain = "admin"; // change after first start

            // generate salt+hash using PBKDF2
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var derive = new Rfc2898DeriveBytes(passwordPlain, salt, 100_000, HashAlgorithmName.SHA256))
            {
                hash = derive.GetBytes(32);
            }

            var combined = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);
            var stored = Convert.ToBase64String(combined);

            var createdAt = DateTimeOffset.UtcNow;

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "username", "displayName", "password", "createdAt", "MustChangePassword" },
                values: new object[] { userId, username, displayName, stored, createdAt, true }
            );

            // assign Admin role to the created user (role must exist from initial seeding)
            migrationBuilder.Sql($@"INSERT INTO ""userRoles"" (""userId"", ""roleId"")
                SELECT '{userId}'::uuid, id FROM ""roles"" WHERE name = 'Admin' AND NOT EXISTS (
                    SELECT 1 FROM ""userRoles"" ur WHERE ur.""userId"" = '{userId}'::uuid
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // remove seeded user and its role link
            migrationBuilder.Sql("DELETE FROM \"userRoles\" WHERE \"userId\" IN (SELECT id FROM \"users\" WHERE username = 'admin');");
            migrationBuilder.Sql("DELETE FROM \"users\" WHERE username = 'admin';");
        }
    }
}
