using Microsoft.Extensions.Options;
using Paperless.REST.BLL.Models;
using RabbitMQ.Client;

namespace Paperless.REST.BLL.Worker
{
    public class RabbitMqConnection : IRabbitMqConnection, IHostedService
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private IChannel? _channel;
        private IConnection? _connection;
        private RabbitMqOptions? _options;
        public IConnection Connection => _connection ?? throw new InvalidOperationException("RabbitMQ connection has not been established.");
        public IChannel Channel => _channel ?? throw new InvalidOperationException("RabbitMQ channel has not been created.");

        public RabbitMqConnection(IOptions<RabbitMqOptions> options)
        {
            if(options is null)
                throw new ArgumentNullException(nameof(options));
            _options = options.Value;
        }


        // IHostedService - Establish connection when the host starts
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options == null)
                throw new InvalidOperationException("RabbitMQ options have not been set.");

            var factory = new ConnectionFactory
            {
                HostName = _options.ServerAddress,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            _logger.Debug("Attempting to connect to RabbitMQ at {ServerAddress}", _options.ServerAddress);

            // Retry loop with exponential backoff. Keep trying until cancellation is requested so services don't crash
            var attempt = 0;
            var delayMs = 2000;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    attempt++;
                    _logger.Debug("RabbitMQ connect attempt {Attempt}", attempt);
                    // pass cancellation token so connect can be cancelled when host is shutting down
                    _connection = await factory.CreateConnectionAsync(cancellationToken);

                    _logger.Info("Successfully connected to RabbitMQ on attempt {Attempt}", attempt);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.Debug("RabbitMQ connection attempt cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to connect to RabbitMQ on attempt {Attempt}. Will retry in {Delay}ms", attempt, delayMs);
                    try
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // exponential backoff, cap at 30s
                    delayMs = Math.Min(delayMs * 2, 30_000);
                }
            }

            _logger.Error("Could not establish RabbitMQ connection before cancellation/shutdown was requested.");
        }

        // IHostedService - cleanup when the host stops
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("Stopping RabbitMqConnection");
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.DisposeAsync();
                    _channel = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing RabbitMQ channel");
            }

            try
            {
                if (_connection != null)
                {
                    await Connection.DisposeAsync();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing RabbitMQ connection");
            }
        }

        public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
        {
            if (_channel == null)
            {
                if (_connection == null)
                    throw new InvalidOperationException("RabbitMQ connection has not been established.");

                _channel = await Connection.CreateChannelAsync(null, ct);

                if (_options == null)
                    throw new InvalidOperationException("RabbitMQ options have not been set.");

                // Declare configured exchange
                if (!string.IsNullOrWhiteSpace(_options.ExchangeName))
                {
                    await _channel.ExchangeDeclareAsync(exchange: _options.ExchangeName, type: _options.ExchangeType ?? "direct", durable: _options.Durable, autoDelete: false, arguments: null, cancellationToken: ct);
                }

                // No queue declarations here; per-worker consumers declare and bind their own queues
            }
            return _channel;
        }
    }
}
