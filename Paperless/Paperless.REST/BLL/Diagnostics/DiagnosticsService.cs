using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.BLL.Worker;
using Paperless.REST.DAL.Models;

namespace Paperless.REST.BLL.Diagnostics
{
    /// <summary>
    /// Default implementation of <see cref="IDiagnosticsService"/>.
    /// Gathers application version, database version and a simple queue backlog metric and worker connectivity.
    /// </summary>
    public class DiagnosticsService : IDiagnosticsService
    {
        private readonly PostgressDbContext? _dbContext;
        private readonly IRabbitMqConnection? _rabbit;
        private readonly ILogger<DiagnosticsService>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsService"/> class with optional dependencies for
        /// database access, message queueing, and logging.
        /// </summary>
        /// <remarks>Use this constructor to provide custom dependencies for database, messaging, or
        /// logging. If any dependency is omitted, related functionality may be limited or unavailable.</remarks>
        /// <param name="dbContext">An optional <see cref="PostgressDbContext"/> used for database operations. If <see langword="null"/>,
        /// database-related features may be unavailable.</param>
        /// <param name="rabbit">An optional <see cref="IRabbitMqConnection"/> used for message queue communication. If <see
        /// langword="null"/>, message queue features may be unavailable.</param>
        /// <param name="logger">An optional <see cref="ILogger{DiagnosticsService}"/> used for logging diagnostic information. If <see
        /// langword="null"/>, logging is disabled.</param>
        public DiagnosticsService(PostgressDbContext? dbContext = null, IRabbitMqConnection? rabbit = null, ILogger<DiagnosticsService>? logger = null)
        {
            _dbContext = dbContext;
            _rabbit = rabbit;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves diagnostic information about the application, database, processing queue, and
        /// worker connectivity.
        /// </summary>
        /// <remarks>This method gathers a snapshot of system diagnostics, including application version,
        /// database version, the number of queued or running processing tasks, and the connectivity status of
        /// background workers. The returned information can be used for health checks, monitoring, or support
        /// scenarios. <para> If any diagnostic component cannot be retrieved, the corresponding property in the result
        /// will indicate an error or unknown state, but the method will not throw. The method attempts to continue
        /// collecting other diagnostics even if some checks fail. </para></remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If cancellation is requested, the operation is canceled where
        /// possible.</param>
        /// <returns>A <see cref="DiagnosticsInfo"/> object containing the current application version, database version,
        /// processing queue backlog, and worker connectivity status.</returns>
        public async Task<DiagnosticsInfo> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var info = new DiagnosticsInfo();

            // Application version from entry assembly
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var ver = asm?.GetName().Version?.ToString() ?? "unknown";
                info.ApplicationVersion = ver;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get application version");
            }

            // Database version (Postgres) - try simple query
            try
            {
                if (_dbContext != null)
                {
                    var conn = _dbContext.Database.GetDbConnection();
                    await using (conn)
                    {
                        await conn.OpenAsync(cancellationToken);
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT version()";
                        var res = await cmd.ExecuteScalarAsync(cancellationToken);
                        var dbVer = res?.ToString() ?? "unknown";
                        info.DatabaseVersion = dbVer;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to query database version");
                info.DatabaseVersion = "error";
            }

            // Queue backlog: count ProcessingStatuses in Queued or Running
            try
            {
                if (_dbContext != null)
                {
                    var count = await _dbContext.ProcessingStatuses.CountAsync(ps => ps.Ocr == ProcessingState.Queued || ps.Ocr == ProcessingState.Running, cancellationToken);
                    info.QueueBacklog = count;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to compute queue backlog");
            }

            // RabbitMQ worker connectivity: try create channel
            try
            {
                if (_rabbit != null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                    var channel = await _rabbit.CreateChannelAsync(linked.Token);
                    // dispose channel if possible
                    if (channel is IAsyncDisposable a)
                        await a.DisposeAsync();
                    else if (channel is IDisposable d)
                        d.Dispose();
                    info.WorkersConnected = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "RabbitMQ connectivity test failed");
                info.WorkersConnected = false;
            }

            return info;
        }
    }
}
