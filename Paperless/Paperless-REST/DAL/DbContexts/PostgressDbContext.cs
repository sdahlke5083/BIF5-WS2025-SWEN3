using Microsoft.EntityFrameworkCore;
using Paperless.REST.DAL.Models;

namespace Paperless.REST.DAL.DbContexts
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
        public DbSet<WorkspaceRole> WorkspaceRoles => Set<WorkspaceRole>();
        // Document
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<DocumentMetadata> DocumentMetadatas => Set<DocumentMetadata>();
        public DbSet<DocumentFileType> FileTypes => Set<DocumentFileType>();
        public DbSet<DocumentLanguage> DocumentLanguages => Set<DocumentLanguage>();
        public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
        public DbSet<FileVersion> FileVersions => Set<FileVersion>();
        // Processing
        public DbSet<ProcessingStatus> ProcessingStatuses => Set<ProcessingStatus>();
        public DbSet<Summary> DocumentSummaries => Set<Summary>();
        public DbSet<SummaryPreset> SummaryPresets => Set<SummaryPreset>();

        public PostgressDbContext(DbContextOptions<PostgressDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // ---------- Users / Roles ----------
            b.Entity<UserRole>(e =>
            {
                e.HasKey(x => new { x.UserId, x.RoleId });
                e.HasOne(x => x.User).WithMany(u => u.UserRoles).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Role).WithMany(r => r.UserRoles).OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Workspaces ----------

            b.Entity<WorkspaceMember>(e =>
            {
                e.HasKey(x => new { x.WorkspaceId, x.UserId });
                e.HasOne(x => x.Role).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Workspace).WithMany(w => w.Members).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Documents (slim) ----------
            b.Entity<Document>(e =>
            {
                // FKs
                e.HasOne(x => x.DeletedByUser).WithMany().OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Workspace).WithMany().OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.ProcessingStatus).WithOne(ps => ps.Document).OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Metadata (versioned) ----------
            b.Entity<DocumentMetadata>(e =>
            {
                e.HasKey(x => new { x.DocumentId, x.Version });
                e.HasOne(x => x.Document).WithMany(d => d.MetadataVersions).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.DocumentLanguage).WithMany().OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<DocumentTag>(e =>
            {
                e.HasKey(x => new { x.DocumentId, x.TagId });
                e.HasOne(x => x.Document).WithMany(d => d.DocumentTags).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Tag).WithMany(t => t.DocumentTags).OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Processing / File / Summaries ----------
            b.Entity<FileVersion>(e =>
            {
                e.HasOne(x => x.Document).WithMany(d => d.FileVersions).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.FileType).WithMany().OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.UploadedByUser).WithMany().OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<ProcessingStatus>(e =>
            {
                e.Property(x => x.LastError).HasDefaultValueSql("NULL");
                // maybe need to add HasConversion for enums here but according to docs it should work out of the box
            });

            b.Entity<Summary>(e =>
            {
                e.HasOne(x => x.Document).WithMany(d => d.Summaries).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.LengthPreset).WithMany().OnDelete(DeleteBehavior.Restrict);
            });
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
