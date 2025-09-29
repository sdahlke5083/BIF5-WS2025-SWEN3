using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class WorkspaceMember
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid WorkspaceId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid UserId { get; set; }
        public Guid WorkspaceRoleId { get; set; }

        // EF Core references
        public WorkspaceRole Role { get; set; } = default!;
        public Workspace Workspace { get; set; } = default!;
        public User User { get; set; } = default!;
    }

}
