using NUnit.Framework;
using Paperless.REST.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Paperless.REST.Test.Controllers
{
    public class SearchControllerTests
    {
        [Test]
        public void SearchDocuments_BadRequest_When_Q_Empty()
        {
            var esMock = new Moq.Mock<Paperless.REST.BLL.Search.MyElasticSearchClient>(null!);
            var controller = new SearchController(esMock.Object);

            var res = controller.SearchDocuments("", 1, 20, null, null, null, null, null, null, null, null, null, null, null, null).GetAwaiter().GetResult() as BadRequestResult;
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.StatusCode, Is.EqualTo(400));
        }
    }
}
