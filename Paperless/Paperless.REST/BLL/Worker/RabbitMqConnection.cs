using Microsoft.Extensions.Options;
using Paperless.REST.BLL.Models;
using RabbitMQ.Client;

namespace Paperless.REST.BLL.Worker
{
    public class RabbitMqConnection : IRabbitMqConnection
    {
        private IChannel? _channel;
        private IConnection? _connection;
        private RabbitMqOptions? _options;

        public IConnection Connection => _connection ?? throw new InvalidOperationException("RabbitMQ connection has not been established.");
        public IChannel Channel => _channel ?? throw new InvalidOperationException("RabbitMQ channel has not been created.");

        /// <summary>
        /// Establishes an asynchronous connection to a RabbitMQ server using the specified configuration options.
        /// </summary>
        /// <param name="options">The configuration options for the RabbitMQ connection. The <see cref="RabbitMqOptions"/> must include a
        /// valid server address.</param>
        /// <returns>A task that represents the asynchronous operation of establishing the connection.</returns>
        public async Task RabbitMqConnectionAsync(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _options.ServerAddress,
                UserName = "paperless",
                Password = "paperless"
            };

            _connection = await factory.CreateConnectionAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
                await _channel.DisposeAsync();
            await Connection.DisposeAsync();
        }

        public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
        {
            if (_channel == null)
            {
                _channel = await Connection.CreateChannelAsync(null, ct);
                if (_options == null)
                    throw new InvalidOperationException("RabbitMQ options have not been set.");
                await _channel.QueueDeclareAsync(queue: _options.QueueName, durable: _options.Durable, autoDelete: false, exclusive: false, arguments: null, cancellationToken: ct);
            }
            return _channel;
        }
    }
}
