using Paperless.REST.BLL.Uploads.Models;
using Paperless.REST.DAL.Models;
using Paperless.REST.DAL.Repositories;
using System.Text.Json;

namespace Paperless.REST.BLL.Uploads
{
    /// <summary>
    /// Upload validation logic
    /// </summary>
    public sealed class UploadService : IUploadService
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly IDocumentRepository _documentRepository;

        public UploadService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

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
                    //cleanup if required
                    metadataRaw.Trim();
                    if (metadataRaw.StartsWith("\"")) metadataRaw = metadataRaw.Substring(1);
                    if (metadataRaw.EndsWith("\"")) metadataRaw = metadataRaw.Substring(0, metadataRaw.Length - 1);
                    metadataRaw.Replace("\\\"","\"");

                    using var _ = JsonDocument.Parse(metadataRaw);  // parse to validate Json
                                                                    // throws if invalid
                }
                catch (JsonException jEx)
                {
                    result.Errors.Add($"Metadata is not valid JSON: {jEx.Message}. Recived: {metadataRaw}");
                }
            }

            if (result.Errors.Count == 0)
            {
                // no errors -> accept the files received
                result.AcceptedCount = files.Count;

                var metaJson = JsonSerializer.Deserialize<UploadMultiMetadata>(metadataRaw ?? "{}", _jsonOptions);
                foreach(var f in files)
                {
                    // if no filename in metadata, use the uploaded filename
                    if (metaJson is null || metaJson.Files is null)
                    {
                        result.Errors.Add($"No metadata found or invalid format for file '{f.FileName}'.");
                    }
                    else
                    {
                        var metaFile = metaJson.Files.FirstOrDefault(mf => mf.Key == f.FileName).Value;
                        if (metaFile is not null)
                        {
                            var doc = new Document();
                            var meta = new DocumentMetadata
                            {
                                Title = metaFile.Title ?? f.FileName,
                                Description = metaFile.Description,
                                CreatedAt = DateTimeOffset.UtcNow
                            };
                            var docID = _documentRepository.CreateWithMetadataAsync(doc, meta, cancelToken).GetAwaiter().GetResult();
                            if (docID == Guid.Empty)
                            {
                                result.Errors.Add($"Failed to create document for file '{f.FileName}'.");
                            }
                            else
                            {
                                result.DocumentIds ??= new List<Guid>();
                                result.DocumentIds.Add(docID);
                            }
                        }
                    }
                }


            }

            return Task.FromResult(result);
        }
    }
}
