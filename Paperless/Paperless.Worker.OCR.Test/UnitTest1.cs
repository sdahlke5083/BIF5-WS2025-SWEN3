using NUnit.Framework;
using SkiaSharp;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;

namespace Paperless.Worker.OCR.Test
{
    [TestFixture]
    public class OcrIntegrationTests : IDisposable
    {
        private readonly string _workDir;
        private readonly string _tessDataPath;

        public OcrIntegrationTests()
        {
            _workDir = Path.Combine(Path.GetTempPath(), "paperless-ocr-test");
            Directory.CreateDirectory(_workDir);

            // In the Docker image Tesseract is expected to have tessdata in a standard location.
            // Allow override via environment variable TESSDATA_PREFIX or TESSDATA_PATH if set in the container.
            _tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
                ?? Environment.GetEnvironmentVariable("TESSDATA_PATH")
                ?? "/tessdata"; // common default in many images
        }

        [SetUp]
        public void Setup()
        {
            // no-op currently
        }

        [Test]
        public void Tesseract_IsAvailable_And_CanRecognizeSimpleText()
        {
            // Arrange
            var text = "HELLO 123";
            var imagePath = Path.Combine(_workDir, "hello.png");
            CreateImageWithText(imagePath, text);

            // Act
            var result = RunTesseractOcr(imagePath);

            // Assert
            Assert.That(string.IsNullOrWhiteSpace(result), Is.False);
            Assert.That(result, Does.Contain("HELLO").IgnoreCase);
            Assert.That(result, Does.Contain("123"));
        }

        [Test]
        public void Tesseract_ReturnsEmpty_ForBlankImage()
        {
            // Arrange
            var imagePath = Path.Combine(_workDir, "blank.png");
            CreateBlankImage(imagePath);

            // Act
            var result = RunTesseractOcr(imagePath);

            // Assert
            Assert.That(string.IsNullOrWhiteSpace(result) || result.Trim().Length == 0, Is.True);
        }

        [Test]
        public void Tesseract_CanRecognize_MultilineText()
        {
            // Arrange
            var text = "LineOne\nLineTwo";
            var imagePath = Path.Combine(_workDir, "multiline.png");
            CreateImageWithText(imagePath, "LineOne\nLineTwo");

            // Act
            var result = RunTesseractOcr(imagePath);

            // Assert
            Assert.That(result, Does.Contain("LineOne").IgnoreCase);
            Assert.That(result, Does.Contain("LineTwo").IgnoreCase);
        }

        private string RunTesseractOcr(string imagePath)
        {
            // Use engine with language English and explicit tessdata path
            using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            return page.GetText() ?? string.Empty;
        }

        private void CreateImageWithText(string path, string text)
        {
            // Create a white image and draw black text
            int width = 400;
            int height = 150;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 32,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            var lines = text.Split('\n');
            float y = 40;
            foreach (var line in lines)
            {
                canvas.DrawText(line, 10, y, paint);
                y += 40;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }

        private void CreateBlankImage(string path)
        {
            int width = 200;
            int height = 100;
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_workDir))
                    Directory.Delete(_workDir, true);
            }
            catch { }
        }
    }
}
