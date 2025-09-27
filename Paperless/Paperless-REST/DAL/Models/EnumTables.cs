namespace DAL.Models
{
    public enum WorkspaceRole
    {
        Owner,
        Editor,
        Viewer
    }

    public enum DocumentContentType
    {
        ApplicationPdf = 1,
        ImagePng = 2,
        ImageJpeg = 3,
        ImageTiff = 4,
        TextPlain = 5,
        ApplicationMsword = 6,
        ApplicationDocx = 7
    }

    public enum DocumentLanguage
    {
        DE = 1,
        EN = 2,
        IT = 3,
        FR = 4,
        ES = 5,
        OTHER = 99
    }

    public enum ProcessingState
    {
        NotProcessed,
        Queued,
        Running,
        Succeeded,
        Failed
    }


    public sealed class ContentTypeLkp { public int Id { get; set; } public string Name { get; set; } = default!; }   // e.g. "application/pdf"
    public sealed class LanguageLkp { public int Id { get; set; } public string Code { get; set; } = default!; }   // e.g. "de","en"
    public sealed class WorkspaceRoleLkp { public int Id { get; set; } public string Name { get; set; } = default!; }   // e.g. "Owner","Editor","Viewer"
    public sealed class ProcessingStateLkp { public int Id { get; set; } public string Name { get; set; } = default!; }   // e.g. "NotProcessed","Queued","Running","Succeeded","Failed"
}
