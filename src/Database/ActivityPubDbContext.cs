using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Jellyfin.Plugin.ActivityPub.Database
{
    public class ActivityPubDbContext : DbContext
    {
        public DbSet<InstanceKeyInfo> InstanceKeys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Rely on the path established in the Plugin entry point
                var dbPath = Path.Combine(Plugin.Instance.DataFolderPath, "activitypub.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InstanceKeyInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.PrivateKeyPem).IsRequired();
                entity.Property(e => e.PublicKeyPem).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });
        }
    }

    public class InstanceKeyInfo
    {
        public int Id { get; set; }
        public string PrivateKeyPem { get; set; }
        public string PublicKeyPem { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
