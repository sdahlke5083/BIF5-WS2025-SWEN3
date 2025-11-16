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
            var factory = new ConnectionFactory
            {
                HostName = _options!.ServerAddress,
                UserName = "paperless",
                Password = "paperless", // TODO HIDE CREDENTIALS
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            _logger.Debug("Attempting to connect to RabbitMQ at {ServerAddress}", _options.ServerAddress);
            _connection = await factory.CreateConnectionAsync();
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

                await _channel.QueueDeclareAsync(queue: _options.QueueName, durable: _options.Durable, autoDelete: false, exclusive: false, arguments: null, cancellationToken: ct);
            }
            return _channel;
        }
    }
}
