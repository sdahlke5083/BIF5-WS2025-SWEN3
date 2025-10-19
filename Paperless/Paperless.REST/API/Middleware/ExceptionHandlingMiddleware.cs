using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Exceptions;
using Paperless.REST.DAL.Exceptions;

namespace Paperless.REST.API.Middleware
{
    /// <summary>
    /// Globale Exception-Handling Middleware:
    /// - Mappt layer spezifische Exceptions -> HTTP-Status
    /// - Liefert ProblemDetails
    /// - Loggt mit NLog (Warn/Error je nach Schwere)
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Default
            var status = HttpStatusCode.InternalServerError;
            string type = "urn:paperless:errors:internal";
            string title = "An unexpected error occurred.";
            string detail = ex.Message;
            bool logAsError = true;

            switch (ex)
            {
                case ValidationException vex:
                    status = HttpStatusCode.BadRequest;                 // 400
                    type = "urn:paperless:errors:validation";
                    title = "Validation failed";
                    if (vex.Errors.Any())
                        detail = string.Join(" | ", vex.Errors);
                    logAsError = false; // client-side issue => Warn
                    break;

                case BusinessRuleException brex:
                    status = HttpStatusCode.Conflict;                   // 409
                    type = "urn:paperless:errors:business-rule";
                    title = "Business rule violated";
                    if (brex.InnerException is not null)
                        detail = brex.InnerException.Message;
                    logAsError = false;
                    break;

                case EntityNotFoundException nfx:
                    status = HttpStatusCode.NotFound;                   // 404
                    type = "urn:paperless:errors:not-found";
                    title = "Entity not found";
                    if(nfx.InnerException is not null)
                        detail = nfx.InnerException.Message;
                    logAsError = false;
                    break;

                case DataAccessException daex:
                    status = HttpStatusCode.ServiceUnavailable;         // 503 (DB down, Timeout, etc.)
                    type = "urn:paperless:errors:data-access";
                    title = "Data access error";
                    if(daex.InnerException is not null)
                        detail = daex.InnerException.Message;
                    logAsError = true;
                    break;
            }

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var problem = new ProblemResponse
            {
                Type = type,
                Title = title,
                Status = (int)status,
                Detail = detail,
                Instance = context.Request?.Path.Value ?? string.Empty,
                TraceId = traceId
            };

            // Logging
            var logMsg = $"{problem.Title} ({problem.Status}) - {problem.Detail} | TraceId={traceId} | Path={problem.Instance}";
            if (logAsError)
                _logger.Error(ex, logMsg);
            else
                _logger.Warn(ex, logMsg);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }
    }
}
