using Microsoft.Extensions.Options;
using Minio.DataModel.Replication;
using NLog;
using Paperless.Worker.OCR.Connectors;
using Paperless.Worker.OCR.RabbitMQ;
using System.Text.Json;

namespace Paperless.Worker.OCR
{
    public class OcrWorker : BackgroundService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private OcrConsumer _consumer;
        private readonly MinioConnector _minio;
        private readonly TesseractConnector _tesseract;

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
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("OCR Worker is now executing.");
            await _consumer.ExecuteAsync(stoppingToken);
        }

        // Verarbeitung der empfangenen Nachricht: MinIO holen -> Tesseract OCR -> loggen
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
