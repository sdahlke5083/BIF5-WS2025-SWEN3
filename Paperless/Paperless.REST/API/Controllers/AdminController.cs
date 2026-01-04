using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Models.BaseResponse;
using Paperless.REST.BLL.Diagnostics;
using Paperless.REST.BLL.Worker;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Health, readiness, diagnostics, and audit logs.
    /// </summary>
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IInfrastructureHealthChecker _dependencyChecker;
        private readonly IDiagnosticsService _diagnosticsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController"/> class with the specified infrastructure
        /// health checker dependency.
        /// </summary>
        /// <param name="dependencyChecker">An object that provides health checking functionality for infrastructure dependencies. Cannot be <see langword="null"/>.</param>
        /// <param name="diagnosticsService">An object that provides diagnostic functionality. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dependencyChecker"/> or <paramref name="diagnosticsService"/> is <see langword="null"/>.</exception>
        public AdminController(IInfrastructureHealthChecker dependencyChecker, IDiagnosticsService diagnosticsService)
        {
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
        }

        /// <summary>
        /// Liveness check
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/health")]
        [ProducesResponseType(statusCode: 200, type: typeof(BasicStatusResponse))]
        public virtual IActionResult Health()
        {
            var ip = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.Trace($"Health requested from {ip}");

            return Ok(new BasicStatusResponse { Status = "OK" });
        }

        /// <summary>
        /// Readiness check
        /// </summary>
        /// <response code="200">Ready</response>
        /// <response code="503">Service unavailable (not ready)</response>
        [HttpGet]
        [Route("/ready")]
        [ProducesResponseType(statusCode: 200, type: typeof(BasicStatusResponse))]
        [ProducesResponseType(statusCode: 503, type: typeof(BasicStatusResponse))]
        public virtual async Task<IActionResult> Ready()
        {
            var rip = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.Trace($"Readiness requested from {rip}");

            // Aufruf an BLL: prüfe alle Abhängigkeiten (DB, RabbitMQ, Elasticsearch, MinIO, ...)
            // Erwartete Rückgabe: (bool AllReady, string[] NotReady)
            try
            {
                var (allOk, failedChecks) = await _dependencyChecker.CheckDependenciesAsync();

                if (allOk)
                {
                    _logger.Trace("Readiness check: all dependencies OK.");
                    return Ok(new BasicStatusResponse { Status = "READY" });
                }
                else
                {
                    var failedList = failedChecks?.ToList() ?? new List<string> { "unknown" };
                    var message = $"Not ready: {string.Join(", ", failedList)}";
                    _logger.Warn($"Readiness check failed: {message}");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new BasicStatusResponse { Status = message });
                }
            }
            catch (Exception ex)
            {
                // Bei Ausnahme: als nicht bereit melden und Fehler protokollieren.
                _logger.Error(ex, "Exception while performing readiness checks.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new BasicStatusResponse { Status = "Not ready: internal error" });
            }
        }

        /// <summary>
        /// Read-only diagnostics (no secrets)
        /// </summary>
        /// <response code="200">Returns a set of basic diagnostics data.</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/v1/admin/diagnostics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(statusCode: 200, type: typeof(DiagnosticsResponse))]
        public virtual async Task<IActionResult> Diagnostics()
        {
            _logger.Trace($"Diagnostics requested from {Request.HttpContext.Connection.RemoteIpAddress}");
            try
            {
                var info = await _diagnosticsService.GetDiagnosticsAsync();
                var diagnostics = new DiagnosticsResponse
                {
                    ApplicationVersion = info.ApplicationVersion ?? string.Empty,
                    DatabaseVersion = info.DatabaseVersion ?? string.Empty,
                    WorkersConnected = info.WorkersConnected,
                    QueueBacklog = info.QueueBacklog
                };

                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to gather diagnostics");
                return StatusCode(StatusCodes.Status500InternalServerError, new DiagnosticsResponse
                {
                    ApplicationVersion = string.Empty,
                    DatabaseVersion = "error",
                    WorkersConnected = false,
                    QueueBacklog = 0
                });
            }
        }
    }
}
