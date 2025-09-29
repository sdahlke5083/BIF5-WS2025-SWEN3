namespace Paperless.REST.DAL.Models
{
    public sealed class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        // EF Core references
        public User User { get; set; } = default!; 
        public Role Role { get; set; } = default!;
    }

}
