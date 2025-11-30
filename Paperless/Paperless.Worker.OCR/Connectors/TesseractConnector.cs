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

        // Erwartet Datei-Bytes und gibt den erkannten Text zurück.
        public async Task<string> RunOcrAsync(byte[] fileBytes)
        {
            if (fileBytes is null || fileBytes.Length == 0)
                throw new ArgumentException("fileBytes darf nicht leer sein.", nameof(fileBytes));

            try
            {
                /* NuGet: Tesseract */
                // Tesseract.NET verwendet native Leptonica/Tesseract libs; hier wird das Pix-Objekt erstellt.
                 using var img = Pix.LoadFromMemory(fileBytes);
                 using var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.Default);
                 using var page = engine.Process(img);
                 var text = page.GetText();
                
                return await Task.FromResult(text ?? string.Empty);

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