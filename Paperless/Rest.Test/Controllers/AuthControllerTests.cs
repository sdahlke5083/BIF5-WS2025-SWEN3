using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Paperless.REST.API.Controllers;
using Paperless.REST.API.Models;
using Paperless.REST.BLL.Security;
using System.Security.Claims;

namespace Paperless.REST.Test.Controllers
{
    public class AuthControllerTests
    {
        [Test]
        public void Login_With_Valid_Credentials_Returns_Token()
        {
            var db = TestUtils.CreateInMemoryDb("auth1");
            var inMemorySettings = new Dictionary<string,string> {
                // 32-byte key for HMAC-SHA256
                { "Jwt__Key", "01234567890123456789012345678901" },
                { "Jwt__Issuer", "test" },
                { "Jwt__Audience", "test" }
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();
            var userService = new UserService(db);
            var loginService = new LoginService(userService, config);
            var controller = new AuthController(userService, loginService);

            var req = new LoginRequest { Username = "test", Password = "password" };
            var res = controller.Login(req) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
            Assert.That(res.Value, Is.Not.Null);
        }

        [Test]
        public void GetProfile_Returns_User()
        {
            var db = TestUtils.CreateInMemoryDb("auth2");
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var userService = new UserService(db);
            var loginService = new LoginService(userService, config);
            var controller = new AuthController(userService, loginService);

            var user = db.Users.First();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

            var res = controller.GetProfile() as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
