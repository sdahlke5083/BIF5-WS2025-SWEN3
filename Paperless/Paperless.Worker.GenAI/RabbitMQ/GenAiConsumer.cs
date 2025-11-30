using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Worker.GenAI.RabbitMQ
{
    internal class GenAiConsumer : IAsyncDisposable
    {
        private IChannel? _ch;
        private IConnection? _conn;
        private string _queueName;
        private string _hostName;
        private int _port;
        private string _username;
        private string _password;

        /// <summary>
        /// Callback, das vom Worker gesetzt wird.
        /// Für jede eingehende Nachricht wird diese Funktion aufgerufen.
        /// </summary>
        public Func<string, Task>? OnMessageReceived { get; set; }

        public GenAiConsumer()
        {
            // read from environment variables with sensible defaults
            _hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";
            _port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672;
            _username = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
            _password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "paperless"; // TODO: Credentials aus Config holen
            _queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "genai-queue";   // eigene Queue für GenAI-Jobs
        }

        public async Task SetupAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                Port = _port,
                UserName = _username,
                Password = _password,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            Console.WriteLine($"GenAiConsumer: attempting to connect to RabbitMQ at {_hostName}:{_port}");

            var attempt = 0;
            var delayMs = 2000;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    attempt++;
                    Console.WriteLine($"GenAiConsumer: connect attempt {attempt}");
                    _conn = await factory.CreateConnectionAsync(cancellationToken);
                    // use overload: CreateChannelAsync(CreateChannelOptions? options, CancellationToken ct)
                    _ch = await _conn.CreateChannelAsync(null, cancellationToken);

                    // declare queue
                    await _ch.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

                    Console.WriteLine($"GenAiConsumer: connected to RabbitMQ and declared queue '{_queueName}'");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("GenAiConsumer: connection attempt cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GenAiConsumer: failed to connect to RabbitMQ on attempt {attempt}: {ex.Message}. Retrying in {delayMs}ms");
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

            Console.WriteLine("GenAiConsumer: could not establish RabbitMQ connection before cancellation/shutdown was requested.");
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
                Console.WriteLine("GenAiConsumer: channel not available, aborting ExecuteAsync");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_ch);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.Span);

                if (OnMessageReceived is null)
                {
                    if (_ch is not null)
                        await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                try
                {
                    await OnMessageReceived(message);

                    if (_ch is not null)
                        await _ch.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    if (_ch is not null)
                        await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Start consuming with the stopping token
            await _ch.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

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
                Console.WriteLine($"GenAiConsumer: error disposing channel: {ex.Message}");
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
                Console.WriteLine($"GenAiConsumer: error disposing connection: {ex.Message}");
            }
        }
    }
}
