using Microsoft.Extensions.Options;
using NLog;
using Paperless.Worker.OCR.Connectors;
using Paperless.Worker.OCR.RabbitMQ;
using RabbitMQ.Client;
using System.Net.Http.Json;
using System.Text.Json;

namespace Paperless.Worker.OCR
{
    public class OcrWorker : BackgroundService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private OcrConsumer _consumer;
        private readonly MinioConnector _minio;
        private readonly TesseractConnector _tesseract;
        private IChannel? _publishChannel; // for forwarding to genai
        private HttpClient? _esHttp; // HTTP client for Elasticsearch

        public OcrWorker(IOptions<MinioStorageOptions> options)
        {
            _minio = new MinioConnector(options);
            _tesseract = new TesseractConnector();

            _consumer = new OcrConsumer
            {
                // Callback delegieren an die Handler-Methode des Workers
                OnMessageReceived = HandleMessageAsync
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting OCR Worker...");
            await _consumer.SetupAsync(cancellationToken);

            // create a connection/channel for publishing results to the shared exchange
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq",
                    Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672,
                    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless",
                    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "paperless",
                    AutomaticRecoveryEnabled = true
                };

                var conn = await factory.CreateConnectionAsync(cancellationToken);
                _publishChannel = await conn.CreateChannelAsync();

                // ensure exchange exists
                var exchangeName = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? "tasks";
                await _publishChannel.ExchangeDeclareAsync(exchange: exchangeName, type: "direct", durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create publisher channel for OCR worker");
            }

            // create http client for elasticsearch
            try
            {
                // support multiple env names and fallbacks
                var baseUrl = Environment.GetEnvironmentVariable("ELASTIC_URL")
                    ?? Environment.GetEnvironmentVariable("ELASTICSEARCH_URL")
                    ?? (Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST") is string host && !string.IsNullOrWhiteSpace(host)
                        ? $"http://{host}:{Environment.GetEnvironmentVariable("ELASTICSEARCH_PORT") ?? "9200"}"
                        : null)
                    ?? "http://ELASTIC_URL-MISSING:9200";

                // Guard against placeholder values like {ELASTICSEARCH_HOST}
                if (baseUrl.Contains("{") || baseUrl.Contains("}"))
                {
                    _logger.Warn("ELASTIC_URL contains unresolved placeholders ('{0}'), falling back to default.", baseUrl);
                    baseUrl = "http://elasticsearch:9200";
                }

                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    _esHttp = new HttpClient() { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
                }
                else
                {
                    _logger.Warn("ELASTIC_URL '{0}' is not a valid absolute URI; Elasticsearch client will be disabled.", baseUrl);
                    _esHttp = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to create Elasticsearch HTTP client");
                _esHttp = null;
            }
            // create http client for elasticsearch using ELASTICSEARCH_HOST/PORT
            try
            {
                var host = Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST") ?? "elasticsearch";
                var port = Environment.GetEnvironmentVariable("ELASTICSEARCH_PORT") ?? "9200";
                var baseUrl = $"http://{host}:{port}";

                if (baseUrl.Contains("{") || baseUrl.Contains("}"))
                {
                    _logger.Warn("ELASTICSEARCH_HOST/PORT contain unresolved placeholders ('{0}'), falling back to default.", baseUrl);
                    baseUrl = "http://elasticsearch:9200";
                }

                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    _esHttp = new HttpClient() { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
                }
                else
                {
                    _logger.Warn("ELASTICSEARCH base URL '{0}' is not a valid absolute URI; Elasticsearch client will be disabled.", baseUrl);
                    _esHttp = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to create Elasticsearch HTTP client");
                _esHttp = null;
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("OCR Worker is now executing.");
            await _consumer.ExecuteAsync(stoppingToken);
        }

        // Verarbeitung der empfangenen Nachricht: MinIO holen -> Tesseract OCR -> loggen -> weitergeben
        private async Task HandleMessageAsync(string json)
        {
            _logger.Debug("Rohnachricht erhalten: {0}", json);

            // Versuche, einen Objekt-Schlüssel aus dem JSON zu extrahieren
            var key = ExtractObjectKey(json);
            _logger.Debug("Extrahierter ObjectKey: {0}", key);

            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.Warn("Kein gültiger Objekt-Key extrahiert; Nachricht wird übersprungen.");
                return;
            }

            _logger.Info($"Received OCR request for object key: {key}");

            try
            {
                // Hole Datei von MinIO (Implementierung in MinioConnector)
                var fileBytes = await _minio.FetchObjectAsync(key);

                // OCR ausführen (Implementierung in TesseractConnector)
                var ocrResult = await _tesseract.RunOcrAsync(fileBytes);

                _logger.Info($"OCR result for {key}: {ocrResult}");

                // derive document id (GUID) from the key by taking the first path segment
                string documentIdSegment = key;
                var segments = key.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    documentIdSegment = segments[0];
                }
                // Thumbnail (erste Seite) speichern
                try
                {
                    var thumbKey = $"{documentIdSegment}/thumbnail.png";
                    var thumbBytes = await PdfToPngConverter.ConvertFirstPageAsync(fileBytes, dpi: 120);
                    if (thumbBytes is not null && thumbBytes.Length > 0)
                    {
                        await _minio.SaveObjectAsync(thumbKey, thumbBytes, contentType: "image/png");
                        _logger.Info("Thumbnail saved for document {0}: {1}", documentIdSegment, thumbKey);
                    }
                }
                catch (Exception ex)
                {
                    // Thumbnail darf die OCR nicht killen
                    _logger.Warn(ex, "Failed to store thumbnail for document {0}", documentIdSegment);
                }

                // If it's a GUID, normalize; otherwise keep as-is but log warning
                if (!Guid.TryParse(documentIdSegment, out var _))
                {
                    _logger.Warn("Extracted document id segment '{0}' is not a valid GUID. Sending segment as-is.", documentIdSegment);
                }

                // attempt to update (upsert) in elasticsearch using _update with doc_as_upsert=true
                if (_esHttp is not null)
                {
                    try
                    {
                        if (Guid.TryParse(documentIdSegment, out var docGuid))
                        {
                            var indexName = "document_texts";
                            var body = new
                            {
                                doc = new { ocr = ocrResult, timestamp = DateTime.UtcNow },
                                doc_as_upsert = true
                            };

                            var updateUri = $"/{indexName}/_update/{docGuid}";
                            var resp = await _esHttp.PostAsJsonAsync(updateUri, body);
                            if (!resp.IsSuccessStatusCode)
                                _logger.Warn("Failed to update (upsert) OCR text to Elasticsearch: {0}", resp.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Error while updating OCR text in Elasticsearch");
                    }
                }

                // Publish result to exchange for GenAI: use routingKey 'genai' and include ocr_text and document_id (GUID only)
                if (_publishChannel is not null)
                {
                    var exchangeName = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? "tasks";
                    var routingKey = "genai";
                    var message = new
                    {
                        schema = "paperless.task.v1",
                        document_id = documentIdSegment,
                        ocr_text = ocrResult
                    };
                    var body = JsonSerializer.SerializeToUtf8Bytes(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    await _publishChannel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, body: body);
                    _logger.Info($"Published GenAI task for document {documentIdSegment} to exchange {exchangeName} with routingKey {routingKey}");
                }
                else
                {
                    _logger.Warn("Publish channel not available; cannot forward OCR result to GenAI.");
                }
            }
            catch (FileNotFoundException fnf)
            {
                _logger.Warn(fnf, "Datei nicht gefunden beim Abruf von MinIO für key='{0}'.", key);
                // ggf. Nachricht dead-lettern / neu versuchen — hier nur Logging
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fehler während Verarbeitung von key='{0}'. Exception: {1}", key, ex.Message);
            }
        }

        private string ExtractObjectKey(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                foreach (var name in new[] { "document_id", "objectKey", "object_key", "key", "filename", "fileKey", "file_key" })
                {
                    if (root.TryGetProperty(name, out var prop))
                    {
                        return prop.GetString() ?? string.Empty;
                    }
                }

                // Fallback: komplette JSON zurückgeben, falls kein Feld gefunden wurde
                return json;
            }
            catch
            {
                // Bei Parse-Fehlern: ursprüngliche Nachricht zurückgeben
                return json;
            }
        }
    }
}
