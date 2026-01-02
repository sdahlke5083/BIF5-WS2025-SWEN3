using System.Diagnostics;

namespace Paperless.Worker.OCR.Connectors;

public static class PdfToPngConverter
{
    public static async Task<IReadOnlyList<byte[]>> ConvertAsync(byte[] pdfBytes, CancellationToken ct)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "paperless-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var pdfPath = Path.Combine(workDir, "input.pdf");
        await File.WriteAllBytesAsync(pdfPath, pdfBytes, ct);

        var outPrefix = Path.Combine(workDir, "page");

        var psi = new ProcessStartInfo
        {
            FileName = "pdftoppm",
            Arguments = $"-rx 300 -ry 300 -png \"{pdfPath}\" \"{outPrefix}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start pdftoppm.");

            var stderrTask = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync(ct);

            var stderr = await stderrTask;
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"pdftoppm failed (ExitCode={p.ExitCode}). {stderr}");

            var files = Directory.GetFiles(workDir, "page-*.png")
                                 .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            if (files.Count == 0)
                throw new InvalidOperationException("pdftoppm produced no images.");

            var result = new List<byte[]>(files.Count);
            foreach (var f in files)
                result.Add(await File.ReadAllBytesAsync(f, ct));

            return result;
        }
        finally
        {
            try { Directory.Delete(workDir, true); } catch { /* ignore */ }
        }
    }
}
