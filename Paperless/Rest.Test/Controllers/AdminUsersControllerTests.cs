using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.BLL.Security;

namespace Paperless.REST.Test.Controllers
{
    public class AdminUsersControllerTests
    {
        [Test]
        public void ListUsers_Returns_Ok_For_Admin()
        {
            var db = TestUtils.CreateInMemoryDb("admin1");
            var userService = new UserService(db);
            var controller = new AdminUsersController(userService);

            // create admin principal
            var user = db.Users.Include(u => u.UserRoles).First();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, "Admin") };
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) } };

            var res = controller.ListUsers() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
