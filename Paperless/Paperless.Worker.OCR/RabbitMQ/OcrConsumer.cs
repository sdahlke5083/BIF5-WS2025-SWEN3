using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Worker.OCR.RabbitMQ
{
    internal class OcrConsumer : IAsyncDisposable
    {
        private IChannel? _ch;
        private String _queueName;
        private String _hostName;
        private int _port;
        private String _username;
        private String _password;

        public Func<string, Task>? OnMessageReceived { get; set; }

        public OcrConsumer()
        {
            _hostName = "paperless-rabbitmq";
            _port = 5672;
            _username = "paperless";
            _password = "paperless"; // TODO HIDE CREDENTIALS
            _queueName = "ocr-queue";
        }

        public async Task SetupAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _hostName,
                Port = _port,
                UserName = _username,
                Password = _password
            };

            // Synchrone Erstellung der Verbindung und des Kanals (stabil und kompatibel)
            var conn = await factory.CreateConnectionAsync(cancellationToken);
            _ch = await conn.CreateChannelAsync();

            // Queue synchron deklarieren
            await _ch.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_ch is null)
                throw new InvalidOperationException("Channel is not initialized. Ensure SetupAsync completed successfully before ExecuteAsync runs.");

            var consumer = new AsyncEventingBasicConsumer(_ch);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body;
                var json = Encoding.UTF8.GetString(body.Span);

                if (OnMessageReceived is null)
                {
                    if (_ch is not null)
                        await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                try
                {
                    await OnMessageReceived(json);

                    if (_ch is not null)
                        await _ch.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    if (_ch is not null)
                        await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _ch.BasicConsumeAsync(_queueName, autoAck: false, consumer);

            // Keep the background service running until cancellation is requested.
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
        }

        public ValueTask DisposeAsync()
        {
            if (_ch is not null)
                return _ch.DisposeAsync();
            return ValueTask.CompletedTask;
        }
    }
}
