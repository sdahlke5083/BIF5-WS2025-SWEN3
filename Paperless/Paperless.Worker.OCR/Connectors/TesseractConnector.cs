using System.Text;
using NLog;
using Tesseract;

namespace Paperless.Worker.OCR.Connectors
{
    public class TesseractConnector
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _tessdataPath;
        private readonly string _language;

        public TesseractConnector(string tessdataPath = "/tessdata", string language = "eng")
        {
            _tessdataPath = tessdataPath;
            _language = language;
        }

        private static bool IsPdf(byte[] bytes)
        {
            return bytes.Length >= 5 &&
                   bytes[0] == (byte)'%' &&
                   bytes[1] == (byte)'P' &&
                   bytes[2] == (byte)'D' &&
                   bytes[3] == (byte)'F' &&
                   bytes[4] == (byte)'-';
        }
        // Erwartet Datei-Bytes und gibt den erkannten Text zurück.
        public async Task<string> RunOcrAsync(byte[] fileBytes, CancellationToken ct = default)
        {
            if (fileBytes is null || fileBytes.Length == 0)
                throw new ArgumentException("fileBytes darf nicht leer sein.", nameof(fileBytes));

            try
            {
                /* NuGet: Tesseract */
                // Tesseract.NET verwendet native Leptonica/Tesseract libs; hier wird das Pix-Objekt erstellt.
                // PDF -> Seiten als PNG rendern, sonst direkt als Bild behandeln
                IReadOnlyList<byte[]> images;
                if (IsPdf(fileBytes))
                {
                    images = await PdfToPngConverter.ConvertAsync(fileBytes, ct);
                }
                else
                {
                    images = new[] { fileBytes };
                }

                // Engine EINMAL pro Request erstellen
                using var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.Default);

                var sb = new StringBuilder();

                foreach (var imgBytes in images)
                {
                    ct.ThrowIfCancellationRequested();

                    using var pix = Pix.LoadFromMemory(imgBytes);
                    using var page = engine.Process(pix);
                    sb.AppendLine(page.GetText() ?? string.Empty);
                    sb.AppendLine("\n----- PAGE BREAK -----\n");
                }

                return sb.ToString();
            }
            catch (DllNotFoundException dnfe)
            {
                _logger.Error(dnfe, "Native Bibliothek fehlt oder nicht ladbar: {0}", dnfe.Message);
                _logger.Error("Stelle sicher, dass Tesseract & Leptonica System-Pakete in der Laufzeitumgebung installiert sind. Beispiel (Debian): 'apt-get install -y tesseract-ocr libleptonica-dev libtesseract-dev'.");
                throw new InvalidOperationException("Native Tesseract/Leptonica libs nicht gefunden. Siehe Logs für Details.", dnfe);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fehler beim Ausführen der OCR.");
                throw;
            }
        }
    }
}