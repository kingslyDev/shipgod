using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ModelConfiguration> ModelConfigurations => Set<ModelConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);
    // Global query filter for soft delete
    modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    // Seeder ModelConfiguration
    ShipmentFinishGood.Data.Seeders.ModelConfigurationSeeder.Seed(modelBuilder);
    }
}
