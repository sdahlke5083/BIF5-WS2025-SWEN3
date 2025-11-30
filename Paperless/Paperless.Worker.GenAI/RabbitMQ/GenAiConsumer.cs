using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Worker.GenAI.RabbitMQ
{
    internal class GenAiConsumer : IAsyncDisposable
    {
        private IChannel? _channel;
        private IConnection? _connection;

        private readonly string _queueName;
        private readonly string _hostName;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        /// <summary>
        /// Callback, das vom Worker gesetzt wird.
        /// Für jede eingehende Nachricht wird diese Funktion aufgerufen.
        /// </summary>
        public Func<string, Task>? OnMessageReceived { get; set; }

        public GenAiConsumer()
        {
            _hostName = "paperless-rabbitmq";
            _port = 5672;
            _username = "paperless";
            _password = "paperless"; // TODO: Credentials aus Config holen
            _queueName = "genai-queue";   // eigene Queue für GenAI-Jobs
        }

        public async Task SetupAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                Port = _port,
                UserName = _username,
                Password = _password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(null, cancellationToken);

            // Queue anlegen
            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (OnMessageReceived is not null)
                {
                    await OnMessageReceived(message);
                }

                // Nachricht als verarbeitet markieren
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
        }
    }
}
