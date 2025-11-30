using NLog;
using Paperless.Worker.GenAI.Connectors;
using Paperless.Worker.GenAI.RabbitMQ;

namespace Paperless.Worker.GenAI
{
    public class GenAiWorker : BackgroundService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly GenAiConnector _genAiConnector;
        private GenAiConsumer _consumer = null!;

        public GenAiWorker()
        {
            // später für echte Gemini-Aufrufe
            _genAiConnector = new GenAiConnector();

            // RabbitMQ-Consumer für die "genai-queue"
            _consumer = new GenAiConsumer
            {
                // Callback: hierher kommt jede Nachricht aus der Queue
                OnMessageReceived = HandleMessageAsync
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting GenAI Worker...");
            // Queue verbinden, Consumer aufsetzen
            await _consumer.SetupAsync(cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("GenAI Worker is running and waiting for messages...");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopping GenAI Worker...");

            if (_consumer is not null)
            {
                await _consumer.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Nachricht aus der RabbitMQ-Queue ankommt.
        /// jsonMessage enthält den Text bzw. die Infos zum Dokument.
        /// </summary>
        private async Task HandleMessageAsync(string jsonMessage)
        {
            _logger.Info("[GenAI] Worker started.");

            try
            {
                var dummy = Environment.GetEnvironmentVariable("This is a dummy OCR text for E2E test.")
                          ?? "This is a dummy OCR text. Please summarize in 3 bullets.";

                var summary = await _genAiConnector.SummarizeAsync(dummy, CancellationToken.None);
                _logger.Info($"[GenAI] SmokeTest summary:\n{summary}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[GenAI] SmokeTest failed.");
            }
        }
    }
}
