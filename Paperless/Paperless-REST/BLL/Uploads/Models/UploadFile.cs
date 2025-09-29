namespace Paperless.REST.BLL.Uploads.Models
{
    /// <summary>
    /// Represents a file upload with metadata including filename, content type, and file size.
    /// </summary>
    public sealed class UploadFile
    {
        public string FileName { get; }
        public string? ContentType { get; }
        public long Length { get; }

        public UploadFile(string fileName, string? contentType, long length)
        {
            FileName = fileName;
            ContentType = contentType;
            Length = length;
        }
    }
}
