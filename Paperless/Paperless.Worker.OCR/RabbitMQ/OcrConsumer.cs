using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Worker.OCR.RabbitMQ
{
    internal class OcrConsumer : IAsyncDisposable
    {
        private IChannel? _ch;
        private IConnection? _conn;
        private String _queueName;
        private String _hostName;
        private int _port;
        private String _username;
        private String _password;

        public Func<string, Task>? OnMessageReceived { get; set; }

        public OcrConsumer()
        {
            // read from environment variables with sensible defaults
            _hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";
            _port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672;
            _username = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
            _password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "paperless"; // TODO HIDE CREDENTIALS
            _queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "ocr-queue";
        }

        public async Task SetupAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _hostName,
                Port = _port,
                UserName = _username,
                Password = _password,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            Console.WriteLine($"OcrConsumer: attempting to connect to RabbitMQ at {_hostName}:{_port}");

            var attempt = 0;
            var delayMs = 2000;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    attempt++;
                    Console.WriteLine($"OcrConsumer: connect attempt {attempt}");
                    _conn = await factory.CreateConnectionAsync(cancellationToken);
                    _ch = await _conn.CreateChannelAsync();

                    // declare queue (synchronous-like via async API)
                    await _ch.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    Console.WriteLine($"OcrConsumer: connected to RabbitMQ and declared queue '{_queueName}'");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("OcrConsumer: connection attempt cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OcrConsumer: failed to connect to RabbitMQ on attempt {attempt}: {ex.Message}. Retrying in {delayMs}ms");
                    try
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    delayMs = Math.Min(delayMs * 2, 30_000);
                }
            }

            Console.WriteLine("OcrConsumer: could not establish RabbitMQ connection before cancellation/shutdown was requested.");
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait until channel is ready or stoppingToken is requested. This prevents crash when RabbitMQ is not yet available.
            while (_ch is null && !stoppingToken.IsCancellationRequested)
            {
                // Poll briefly - SetupAsync should be running in parallel and will populate _ch when connected
                await Task.Delay(500, stoppingToken).ContinueWith(_ => { });
            }

            if (_ch is null)
            {
                // If still null, just return so the background worker can stop gracefully.
                Console.WriteLine("OcrConsumer: channel not available, aborting ExecuteAsync");
                return;
            }

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

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_ch is not null)
                {
                    await _ch.DisposeAsync();
                    _ch = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OcrConsumer: error disposing channel: {ex.Message}");
            }

            try
            {
                if (_conn is not null)
                {
                    await _conn.DisposeAsync();
                    _conn = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OcrConsumer: error disposing connection: {ex.Message}");
            }
        }
    }
}
