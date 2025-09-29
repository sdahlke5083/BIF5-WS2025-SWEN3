using System.Text.Json;
using Paperless.REST.BLL.Uploads.Models;

namespace Paperless.REST.BLL.Uploads
{
    /// <summary>
    /// Upload validation logic
    /// </summary>
    public sealed class UploadService : IUploadService
    {
        public string Path { get; set; } = string.Empty;

        public Task<UploadValidationResult> ValidateAsync(
            IReadOnlyCollection<UploadFile> files,  // files to be uploaded
            string? metadataRaw,
            CancellationToken cancelToken = default)    // optional cancellation token
        {
            var result = new UploadValidationResult();

            // must have atleast one file
            if (files is null || files.Count == 0)
            {
                result.Errors.Add("No files were provided. Send at least one file via form field 'files'.");
                return Task.FromResult(result); // Return precompleted task with result
            }

            // must be a plosible file
            foreach (var f in files)
            {
                if (string.IsNullOrWhiteSpace(f.FileName))
                    result.Errors.Add("A file without a name was provided.");

                if (f.Length <= 0)
                    result.Errors.Add($"File '{f.FileName}' has 0 length.");
            }

            // optional metadata must be valid JSON
            if (!string.IsNullOrWhiteSpace(metadataRaw))
            {
                try
                {
                    using var _ = JsonDocument.Parse(metadataRaw);  // parse to validate Json
                                                                    // throws if invalid
                }
                catch (JsonException jEx)
                {
                    result.Errors.Add($"Metadata is not valid JSON: {jEx.Message}");
                }
            }

            if (result.Errors.Count == 0)
            {
                // no errors -> accept the files received
                result.AcceptedCount = files.Count;
            }

            return Task.FromResult(result);
        }
    }
}
