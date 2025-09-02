using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ModelConfiguration> ModelConfigurations => Set<ModelConfiguration>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<UploadSessionDetail> UploadSessionDetails => Set<UploadSessionDetail>();
    public DbSet<POMaster> POMasters => Set<POMaster>();
    public DbSet<PODetail> PODetails => Set<PODetail>();
    public DbSet<BarcodeRegistry> BarcodeRegistries => Set<BarcodeRegistry>();
    public DbSet<ScanningActivity> ScanningActivities => Set<ScanningActivity>();
    public DbSet<POLock> POLocks => Set<POLock>(); // NEW: PO Lock support

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        
        // Seeder ModelConfiguration
        ShipmentFinishGood.Data.Seeders.ModelConfigurationSeeder.Seed(modelBuilder);
        
        // Configure relationships
        modelBuilder.Entity<UploadSessionDetail>()
            .HasOne(d => d.Session)
            .WithMany(s => s.Details)
            .HasForeignKey(d => d.SessionId);
            
        modelBuilder.Entity<POMaster>()
            .HasOne(p => p.SourceSession)
            .WithMany(s => s.POMasters)
            .HasForeignKey(p => p.SourceSessionId);
            
        modelBuilder.Entity<PODetail>()
            .HasOne(d => d.PO)
            .WithMany(p => p.Details)
            .HasForeignKey(d => d.POId);
            
        modelBuilder.Entity<BarcodeRegistry>()
            .HasOne(b => b.Session)
            .WithMany()
            .HasForeignKey(b => b.SessionId);
            
        modelBuilder.Entity<BarcodeRegistry>()
            .HasOne(b => b.POMaster)
            .WithMany()
            .HasForeignKey(b => b.POId);
            
        // Add unique constraint for active barcodes
        modelBuilder.Entity<BarcodeRegistry>()
            .HasIndex(b => b.BarcodeValue)
            .IsUnique()
            .HasFilter("[IsActive] = 1");
            
        // Add indexes for performance
        modelBuilder.Entity<BarcodeRegistry>()
            .HasIndex(b => new { b.SessionId, b.Status, b.IsActive });
            
        modelBuilder.Entity<BarcodeRegistry>()
            .HasIndex(b => new { b.POId, b.BoxNumber });
            
        // Configure POLock relationships
        modelBuilder.Entity<POLock>()
            .HasOne(pl => pl.Session)
            .WithMany()
            .HasForeignKey(pl => pl.SessionId);
            
        modelBuilder.Entity<POLock>()
            .HasOne(pl => pl.PO)
            .WithMany()
            .HasForeignKey(pl => pl.POId);
            
        // Ensure only one active PO lock per user per session
        modelBuilder.Entity<POLock>()
            .HasIndex(pl => new { pl.UserId, pl.SessionId, pl.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
