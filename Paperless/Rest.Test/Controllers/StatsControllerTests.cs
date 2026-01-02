using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class StatsControllerTests
    {
        [Test]
        public void GetTopStats_Returns_OK()
        {
            var db = TestUtils.CreateInMemoryDb("stats1");
            var controller = new StatsController(db);

            var res = controller.GetTopStats("7d", "views", 10) as OkObjectResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(200));
        }
    }
}
