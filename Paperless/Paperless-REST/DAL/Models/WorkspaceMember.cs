namespace DAL.Models
{
    public sealed class WorkspaceMember
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = default!;
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public WorkspaceRole Role { get; set; }
    }

}
