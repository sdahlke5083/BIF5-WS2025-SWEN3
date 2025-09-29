namespace Paperless.REST.DAL.Models
{
    public sealed class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!; public string? Description { get; set; }
        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    }

}
