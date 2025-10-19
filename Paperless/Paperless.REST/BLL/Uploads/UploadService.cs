using Paperless.REST.BLL.Uploads.Models;
using Paperless.REST.BLL.Worker;
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
        /// <summary>
        /// Gets or sets the file system path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        // private fields
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentEventPublisher _documentEventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <remarks>This constructor requires a valid implementation of <see cref="IDocumentRepository"/>
        /// to handle document-related operations. Ensure that the provided repository is properly configured before
        /// using this service.</remarks>
        /// <param name="documentRepository">The repository used to manage document storage and retrieval operations.</param>
        public UploadService(IDocumentRepository documentRepository, IDocumentEventPublisher documentEventPublisher)
        {
            _documentRepository = documentRepository;
            _documentEventPublisher = documentEventPublisher;
        }

        /// <summary>
        /// Validates the provided files and optional metadata for upload.
        /// </summary>
        /// <remarks>This method performs the following validations: <list type="bullet">
        /// <item><description>Ensures that at least one file is provided.</description></item>
        /// <item><description>Checks that each file has a valid name and a non-zero length.</description></item>
        /// <item><description>Validates that the optional metadata, if provided, is a valid JSON
        /// string.</description></item> </list> If the validation succeeds, the method attempts to process the files
        /// and associate them with the provided metadata. Any errors encountered during validation or processing are
        /// included in the <see cref="UploadValidationResult.Errors"/> collection.</remarks>
        /// <param name="files">A collection of files to be validated. Must contain at least one file.</param>
        /// <param name="metadataRaw">An optional raw JSON string representing metadata for the files. If provided, it must be valid JSON.</param>
        /// <param name="cancelToken">An optional <see cref="CancellationToken"/> to observe while performing the validation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains an <see
        /// cref="UploadValidationResult"/> object that includes the validation outcome, any errors encountered, and the
        /// IDs of successfully processed documents.</returns>
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
                    metadataRaw.Replace("\\\"", "\"");

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
                foreach (var f in files)
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

                            // add document to the Database

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

                            // fire up rabbitmq event to process the document
                            _documentEventPublisher.PublishDocumentUploadedAsync(docID, cancelToken).GetAwaiter().GetResult();
                        }
                    }
                }
            }
            return Task.FromResult(result);
        }

        /// <summary>
        /// Saves the specified collection of files asynchronously, with optional metadata and cancellation support.
        /// </summary>
        /// <remarks>This method performs the save operation asynchronously. If the operation is canceled
        /// via the <paramref name="cancelToken"/>, the returned task will be in the Canceled state.</remarks>
        /// <param name="files">A collection of <see cref="UploadFile"/> objects representing the files to be saved. This collection must
        /// not be null or empty.</param>
        /// <param name="metadataRaw">An optional string containing metadata in raw format. Can be null if no metadata is provided.</param>
        /// <param name="cancelToken">An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete. Defaults
        /// to <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the files
        /// were successfully saved; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> SaveFilesAsync(
            IReadOnlyCollection<UploadFile> files,  // files to be uploaded
            string? metadataRaw,
            CancellationToken cancelToken = default)    // optional cancellation token
        {
            throw new NotImplementedException();
        }
    }
}
