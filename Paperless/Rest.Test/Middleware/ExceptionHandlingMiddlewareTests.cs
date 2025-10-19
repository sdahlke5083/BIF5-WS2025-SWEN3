using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Paperless.REST.API.Middleware;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Exceptions;
using Paperless.REST.DAL.Exceptions;

namespace Rest.Test.Middleware
{
    [TestFixture]
    public class ExceptionHandlingMiddlewareTests
    {
        private static DefaultHttpContext NewContext()
        {
            return new DefaultHttpContext
            {
                Response = { Body = new MemoryStream() }
            };
        }

        private static async Task<ProblemResponse> InvokeAndReadAsync(RequestDelegate throws)
        {
            var mw = new ExceptionHandlingMiddleware(throws);
            var ctx = NewContext();

            await mw.InvokeAsync(ctx);

            ctx.Response.Body.Position = 0;
            var json = await new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEndAsync();
            var problem = JsonSerializer.Deserialize<ProblemResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            return problem;
        }

        [Test]
        public async Task ValidationException_Returns400()
        {
            var problem = await InvokeAndReadAsync(_ => throw new ValidationException("Invalid input", new[] { "A", "B" }));

            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(problem.Title, Is.EqualTo("Validation failed"));
            Assert.That(problem.Detail, Does.Contain("A").And.Contain("B"));
        }

        [Test]
        public async Task BusinessRuleException_Returns409()
        {
            var problem = await InvokeAndReadAsync(_ => throw new BusinessRuleException("Rule broken"));

            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(problem.Title, Is.EqualTo("Business rule violated"));
            Assert.That(problem.Detail, Is.EqualTo("Rule broken"));
        }

        [Test]
        public async Task EntityNotFoundException_Returns404()
        {
            var problem = await InvokeAndReadAsync(_ => throw new EntityNotFoundException("Nope", "Document", Guid.NewGuid()));

            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status404NotFound));
            Assert.That(problem.Title, Is.EqualTo("Entity not found"));
        }

        [Test]
        public async Task DataAccessException_Returns503()
        {
            var problem = await InvokeAndReadAsync(_ => throw new DataAccessException("DB timed out"));

            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
            Assert.That(problem.Title, Is.EqualTo("Data access error"));
        }

        [Test]
        public async Task UnknownException_Returns500()
        {
            var problem = await InvokeAndReadAsync(_ => throw new Exception("Boom"));

            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(problem.Title, Is.EqualTo("An unexpected error occurred."));
        }
    }
}
