using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Worker.OCR.RabbitMQ
{
    internal class DemoConsumer : BackgroundService
    {
        private IChannel? _ch;

        public DemoConsumer()
        {
            // empty constructor for now
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "paperless-rabbitmq",
                UserName = "paperless",
                Password = "paperless"
            };

            // Synchrone Erstellung der Verbindung und des Kanals (stabil und kompatibel)
            var conn = await factory.CreateConnectionAsync(cancellationToken);
            _ch = await conn.CreateChannelAsync();

            // Queue synchron deklarieren
            await _ch.QueueDeclareAsync(queue: "demo-queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            // StartAsync der Basisklasse aufrufen, damit ExecuteAsync gestartet wird
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_ch is null)
                throw new InvalidOperationException("Channel is not initialized. Ensure StartAsync completed successfully before ExecuteAsync runs.");

            var consumer = new AsyncEventingBasicConsumer(_ch);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body;
                    var json = Encoding.UTF8.GetString(body.Span);
                    Console.WriteLine($"[DemoConsumer] Recieved new Message in Queue: {json}");
                    await _ch.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    if (_ch is not null)
                        await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _ch.BasicConsumeAsync("demo-queue", autoAck: false, consumer);

            // Keep the background service running until cancellation is requested.
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
        }

        public override async void Dispose()
        {
            if (_ch is not null)
                await _ch.DisposeAsync();

            base.Dispose();
        }
    }
}
