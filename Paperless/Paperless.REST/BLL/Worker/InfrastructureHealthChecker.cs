using Microsoft.EntityFrameworkCore;
using Paperless.REST.BLL.Search;
using Paperless.REST.BLL.Storage;
using Paperless.REST.DAL.DbContexts;

namespace Paperless.REST.BLL.Worker
{
    /// <summary>
    /// Provides functionality to check the readiness of critical infrastructure dependencies required by the
    /// application.
    /// </summary>
    /// <remarks><see cref="InfrastructureHealthChecker"/> verifies the availability of essential services
    /// such as databases, object storage, search engines, and message brokers. It is typically used during application
    /// startup or health check endpoints to ensure that all required external services are accessible and operational
    /// before the application proceeds.</remarks>
    public class InfrastructureHealthChecker : IInfrastructureHealthChecker
    {
        private readonly ILogger<InfrastructureHealthChecker>? _logger;
        private readonly PostgressDbContext? _dbContext;
        private readonly IFileStorageService? _fileStorageService;
        private readonly MyElasticSearchClient? _esClient;
        private readonly IRabbitMqConnection? _rabbitConnection;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureHealthChecker"/> class with optional dependencies
        /// for health checking various infrastructure components.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> instance used to check database connectivity. Can be <see langword="null"/> if
        /// database health checks are not required.</param>
        /// <param name="fileStorageService">The <see cref="IFileStorageService"/> implementation used to verify file storage availability. Can be <see
        /// langword="null"/> if file storage health checks are not required.</param>
        /// <param name="esClient">The <see cref="MyElasticSearchClient"/> instance used to check the health of the Elasticsearch service. Can
        /// be <see langword="null"/> if Elasticsearch health checks are not required.</param>
        /// <param name="rabbitConnection">The <see cref="IRabbitMqConnection"/> used to verify RabbitMQ connectivity. Can be <see langword="null"/> if
        /// RabbitMQ health checks are not required.</param>
        /// <param name="logger">The <see cref="ILogger{InfrastructureHealthChecker}"/> used for logging health check results and errors. Can
        /// be <see langword="null"/> if logging is not needed.</param>
        public InfrastructureHealthChecker(
            PostgressDbContext? dbContext = null,
            IFileStorageService? fileStorageService = null,
            MyElasticSearchClient? esClient = null,
            IRabbitMqConnection? rabbitConnection = null,
            ILogger<InfrastructureHealthChecker>? logger = null)
        {
            _dbContext = dbContext;
            _fileStorageService = fileStorageService;
            _esClient = esClient;
            _rabbitConnection = rabbitConnection;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously checks the readiness of all required service dependencies.
        /// </summary>
        /// <remarks>This method verifies the status of each configured dependency (such as the database,
        /// object storage, search engine, and message broker) and determines whether all are ready for use. It is
        /// typically called during application startup or health checks to ensure that critical services are
        /// available.</remarks>
        /// <returns>A tuple containing: <list type="bullet"> <item><description><c>AllReady</c>: <see langword="true"/> if all
        /// dependencies are ready; otherwise, <see langword="false"/>.</description></item>
        /// <item><description><c>NotReady</c>: An array of strings listing the names of dependencies that are not
        /// ready. Each entry may include an optional reason for the failure.</description></item> </list></returns>
        public async Task<(bool AllReady, string[] NotReady)> CheckDependenciesAsync()
        {
            var checks = new[]
            {
                CheckDatabaseAsync(),
                CheckMinioAsync(),
                CheckElasticsearchAsync(),
                CheckRabbitMqAsync()
            };

            var results = await Task.WhenAll(checks).ConfigureAwait(false);

            var notReady = results.Where(r => !r.Ready).Select(r => r.Name + (string.IsNullOrEmpty(r.Reason) ? "" : $": {r.Reason}")).ToArray();
            return (notReady.Length == 0, notReady);
        }


        private async Task<(string Name, bool Ready, string Reason)> CheckDatabaseAsync()
        {
            const string name = "Database";
            try
            {
                // Require an injected DbContext.
                if (_dbContext == null)
                {
                    return (name, false, "DbContext not provided to InfrastructureHealthChecker; register DbContext in DI and provide it to the checker.");
                }

                try
                {
                    using var cts = new CancellationTokenSource(DefaultTimeout);
                    var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token).ConfigureAwait(false);
                    return canConnect ? (name, true, "") : (name, false, "Database not reachable (EF CanConnect failed)");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "EF Database.CanConnectAsync failed on injected DbContext");
                    return (name, false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fehler bei Database-Check");
                return (name, false, ex.Message);
            }
        }

        private async Task<(string Name, bool Ready, string Reason)> CheckMinioAsync()
        {
            const string name = "MinIO";

            // Require an IFileStorageService to perform reliable MinIO checks. Avoid brittle env/connection-string parsing.
            if (_fileStorageService == null)
            {
                return (name, false, "FileStorageService not provided to InfrastructureHealthChecker; register IFileStorageService in DI and provide it to the checker.");
            }

            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);
                var connected = await _fileStorageService.TestConnectionAsync(cts.Token).ConfigureAwait(false);
                return connected ? (name, true, "") : (name, false, "MinIO client reports bucket not reachable");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fehler bei MinIO-Check");
                return (name, false, ex.Message);
            }
        }

        private async Task<(string Name, bool Ready, string Reason)> CheckElasticsearchAsync()
        {
            const string name = "Elasticsearch";
            // Require an Elasticsearch client to perform a reliable check
            if (_esClient == null)
                return (name, false, "Elasticsearch client not provided to InfrastructureHealthChecker; register MyElasticSearchClient in DI and provide it to the checker.");

            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);
                var connected = await _esClient.TestConnectionAsync(cts.Token).ConfigureAwait(false);
                return connected ? (name, true, "") : (name, false, "Elasticsearch cluster not reachable");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Elasticsearch TestConnectionAsync failed");
                return (name, false, ex.Message);
            }
        }

        private async Task<(string Name, bool Ready, string Reason)> CheckRabbitMqAsync()
        {
            const string name = "RabbitMQ";

            if (_rabbitConnection == null)
            {
                return (name, false, "RabbitMQ connection service not provided to InfrastructureHealthChecker; register IRabbitMqConnection in DI and provide it to the checker.");
            }

            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);

                try
                {
                    // Erzeuge asynchron einen Channel zur Verifikation der Verbindung
                    var channel = await _rabbitConnection.CreateChannelAsync(cts.Token).ConfigureAwait(false);

                    // Versuche, den Channel sauber zu schließen, falls er disposable ist
                    if (channel is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (channel is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    // Wenn keine Exception aufgetreten ist, gilt RabbitMQ als erreichbar
                    return (name, true, "");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "RabbitMQ connection not ready");
                    return (name, false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fehler bei RabbitMQ-Check");
                return (name, false, ex.Message);
            }
        }
    }
}
