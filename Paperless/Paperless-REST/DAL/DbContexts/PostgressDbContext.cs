using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DAL.DbContexts
{
    public class PostgressDbContext : DbContext
    {

        // DB Sets
        // User
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        // Workspace
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<WorkspaceRoleLkp> WorkspaceRoleLkps => Set<WorkspaceRoleLkp>();
        // Document
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();
        public DbSet<ContentTypeLkp> ContentTypeLkps => Set<ContentTypeLkp>();
        public DbSet<LanguageLkp> LanguageLkps => Set<LanguageLkp>();
        public DbSet<DocumentMetadataTag> DocumentTags => Set<DocumentMetadataTag>();
        public DbSet<FileVersion> FileVersions => Set<FileVersion>();
        // Processing
        public DbSet<ProcessingStatus> ProcessingStatuses => Set<ProcessingStatus>();
        public DbSet<Summary> DocumentSummary => Set<Summary>();
        public DbSet<ProcessingStateLkp> ProcessingStateLkps => Set<ProcessingStateLkp>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            // TODO
        }

        // Normalize UTZ on saves 
        public override int SaveChanges()
        {
            NormalizeUtc();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeUtc();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void NormalizeUtc()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified);

            foreach (var e in entries)
                foreach (var p in e.Properties.Where(p => p.Metadata.ClrType == typeof(DateTimeOffset) && p.CurrentValue is DateTimeOffset))
                {
                    var dto = (DateTimeOffset)p.CurrentValue!;
                    // force to UTC offset
                    if (dto.Offset != TimeSpan.Zero)
                        p.CurrentValue = dto.ToUniversalTime();
                }
        }
    }
}
