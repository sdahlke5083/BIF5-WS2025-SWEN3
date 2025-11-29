using Google.GenAI;
using Google.GenAI.Types;
using NLog;
using Paperless.Worker.GenAI.Exceptions;

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
            _apiKey = System.Environment.GetEnvironmentVariable("GENAI_API_KEY") ?? string.Empty;
            _model = System.Environment.GetEnvironmentVariable("GENAI_MODEL") ?? "gemini-2.5-pro";
        }

        /// <summary>
        /// Place-Holder: simuliert das Erzeugen einer Summary.
        /// textToSummarize wird später der Dokumenttext sein.
        /// </summary>
        public async Task<string> SummarizeAsync(string textToSummarize, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Debug("SummarizeAsync wurde aufgerufen, aber der CancellationToken ist bereits abgebrochen.");
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.Error("GENAI_API_KEY ist nicht gesetzt oder leer.");
                throw new GenAiConfigurationException("GENAI_API_KEY is missing. GenAI worker cannot call the API.");
            }

            if (string.IsNullOrWhiteSpace(_model))
            {
                _logger.Error("GENAI_MODEL ist nicht gesetzt oder leer.");
                throw new GenAiConfigurationException("GENAI_MODEL is missing. GenAI worker cannot call the API.");
            }

            if (string.IsNullOrWhiteSpace(textToSummarize))
            {
                throw new ArgumentException("Text to summarize must not be null or empty.", nameof(textToSummarize));
            }

            var client = new Client(apiKey: _apiKey);

            var prompt = $"Summarize the following text in 3 concise bullet points:\n{textToSummarize}";

            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: _model,
                    contents: prompt);

                var text = response?.Candidates?.FirstOrDefault()
                                     ?.Content?.Parts?.FirstOrDefault()?.Text;

                return string.IsNullOrWhiteSpace(text)
                    ? "[GenAI returned empty response]"
                    : text.Trim();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[GenAI] API call failed.");
                throw new GenAiApiException("Gemini API call failed", ex);
            }
        }
    }
}
