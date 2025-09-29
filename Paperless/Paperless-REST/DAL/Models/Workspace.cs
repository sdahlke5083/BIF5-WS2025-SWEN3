using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.DAL.Models
{
    public sealed class Workspace
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        // EF Core references
        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    }

    public sealed class WorkspaceRole
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!; // e.g. "Owner","Editor","Viewer"
    }

}
