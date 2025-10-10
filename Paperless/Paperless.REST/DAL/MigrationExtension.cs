using Microsoft.EntityFrameworkCore;
using Paperless.REST.DAL.DbContexts;

namespace Paperless.REST.DAL
{
    public static class MigrationExtension
    {
        public static void ApplyMigrations(this IApplicationBuilder builder)
        {
            using IServiceScope scope = builder.ApplicationServices.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<PostgressDbContext>();

            if(context.Database.CanConnect())
                context.Database.Migrate();

        }
    }
}
