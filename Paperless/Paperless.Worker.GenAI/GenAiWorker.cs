using NLog;
using Paperless.Worker.GenAI.Connectors;
using Paperless.Worker.GenAI.RabbitMQ;
using System.Text.Json;
using System.Net.Http.Json;

namespace Paperless.Worker.GenAI
{
    public class GenAiWorker : BackgroundService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly GenAiConnector _genAiConnector;
        private GenAiConsumer _consumer = null!;
        private readonly HttpClient _http;
        private readonly string _restBaseUrl;

        public GenAiWorker()
        {
            // später für echte Gemini-Aufrufe
            _genAiConnector = new GenAiConnector();

            // RabbitMQ-Consumer für die "task-queue"
            _consumer = new GenAiConsumer
            {
                // Callback: hierher kommt jede Nachricht aus der Queue
                OnMessageReceived = HandleMessageAsync
            };

            // Http client to call REST API to store summaries
            _http = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _restBaseUrl = Environment.GetEnvironmentVariable("REST_API_URL") ?? "http://paperless-rest:8081";
            // set base address for easier calls
            try
            {
                _http.BaseAddress = new Uri(_restBaseUrl);
            }
            catch
            {
                _logger.Warn("REST_API_URL '{0}' is not a valid URI, falling back to no BaseAddress.", _restBaseUrl);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting GenAI Worker...");
            // Queue verbinden, Consumer aufsetzen
            await _consumer.SetupAsync(cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("GenAI Worker is running and waiting for messages...");
            // Start consuming messages until stopped
            if (_consumer is not null)
            {
                await _consumer.ExecuteAsync(stoppingToken);
            }
            else
            {
                _logger.Warn("GenAiConsumer is null; nothing to execute.");
                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException) { }
            }
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
                // Expect the message to contain ocr_text and document_id
                using var doc = JsonDocument.Parse(jsonMessage);
                var root = doc.RootElement;

                string ocrText = string.Empty;
                if (root.TryGetProperty("ocr_text", out var t))
                    ocrText = t.GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    _logger.Warn("[GenAI] No OCR text found in message, skipping.");
                    return;
                }

                var summary = await _genAiConnector.SummarizeAsync(ocrText, CancellationToken.None);
                _logger.Info($"[GenAI] Summary for document:\n{summary}");

                // send summary to REST API
                var documentId = root.TryGetProperty("document_id", out var idProp) ? idProp.GetString() : null;
                if (documentId is null)
                {
                    _logger.Warn("[GenAI] No document_id present in message; skipping REST upload.");
                    return;
                }

                var payload = new
                {
                    model = "genai",
                    lengthPresetId = (Guid?)null,
                    content = summary
                };

                var relativeUrl = $"/v1/documents/{documentId}/summaries";

                // Retry with exponential backoff for transient network errors
                var maxAttempts = 3;
                var delayMs = 500;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        HttpResponseMessage resp;
                        if (_http.BaseAddress != null)
                            resp = await _http.PostAsJsonAsync(relativeUrl, payload);
                        else
                            resp = await _http.PostAsJsonAsync(new Uri(new Uri(_restBaseUrl), relativeUrl), payload);

                        if (resp.IsSuccessStatusCode)
                        {
                            _logger.Info($"[GenAI] Successfully stored summary for document {documentId} via REST API.");
                            // also update ES directly (worker-side) to set summary
                            try
                            {
                                var host = Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST") ?? "elasticsearch";
                                var port = Environment.GetEnvironmentVariable("ELASTICSEARCH_PORT") ?? "9200";
                                var esUrl = $"http://{host}:{port}";
                                using var client = new HttpClient() { BaseAddress = new Uri(esUrl), Timeout = TimeSpan.FromSeconds(10) };
                                if (Guid.TryParse(documentId, out var docGuid))
                                {
                                    var indexName = "document_texts";
                                    var updateBody = new { doc = new { summary = summary, timestamp = DateTime.UtcNow }, doc_as_upsert = true };
                                    var updateResp = await client.PostAsJsonAsync($"/{indexName}/_update/{docGuid}", updateBody);
                                    if (!updateResp.IsSuccessStatusCode)
                                        _logger.Warn("Failed to update summary in Elasticsearch directly from GenAI worker: {0}", updateResp.StatusCode);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Error updating summary in Elasticsearch from GenAI worker");
                            }
                            break;
                        }

                        var respText = string.Empty;
                        try
                        {
                            respText = await resp.Content.ReadAsStringAsync();
                        }
                        catch { }

                        _logger.Error($"[GenAI] REST API returned status {resp.StatusCode} on attempt {attempt}. Response: {respText}");

                        if (attempt == maxAttempts)
                        {
                            _logger.Error($"[GenAI] Giving up after {maxAttempts} attempts.");
                        }
                        else
                        {
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                        }
                    }
                    catch (HttpRequestException hre)
                    {
                        _logger.Warn(hre, $"[GenAI] Network error while sending summary (attempt {attempt}): {hre.Message}");
                        if (attempt == maxAttempts)
                        {
                            _logger.Error(hre, "[GenAI] Final attempt failed. Giving up.");
                        }
                        else
                        {
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"[GenAI] Unexpected error while sending summary (attempt {attempt}).");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[GenAI] Processing failed.");
            }
        }
    }
}
