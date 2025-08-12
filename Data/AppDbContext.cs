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
            
        // Add unique constraint for barcode
        modelBuilder.Entity<BarcodeRegistry>()
            .HasIndex(b => b.BarcodeValue)
            .IsUnique();
    }
}
