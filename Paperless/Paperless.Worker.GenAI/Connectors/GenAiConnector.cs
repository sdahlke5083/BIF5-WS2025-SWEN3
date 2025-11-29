using NLog;
using System.Net.Http;

namespace Paperless.Worker.GenAI.Connectors
{
    /// <summary>
    /// Stub-Klasse für die Anbindung an die GenAI-API
    /// </summary>
    public class GenAiConnector
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GenAiConnector()
        {
            _httpClient = new HttpClient();

            // kommen aus .env und docker-compose
            _apiKey = Environment.GetEnvironmentVariable("GENAI_API_KEY") ?? string.Empty;
            _model = Environment.GetEnvironmentVariable("GENAI_MODEL") ?? "gemini-1.5-pro";
        }

        /// <summary>
        /// Place-Holder: simuliert das Erzeugen einer Summary.
        /// textToSummarize wird später der Dokumenttext sein.
        /// </summary>
        public async Task<string> SummarizeAsync(string textToSummarize, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.Warn("GENAI_API_KEY is not configured. Returning fallback summary.");
                return "[GenAI disabled: missing API key]";
            }

            // Hier nur ein Fake-Call.
            _logger.Info($"Simulating GenAI summary for model '{_model}', text length = {textToSummarize?.Length ?? 0}.");

            await Task.Delay(10, cancellationToken); // kleine künstliche Verzögerung
            return $"[Summary placeholder for text length {textToSummarize?.Length ?? 0}]";
        }
    }
}
