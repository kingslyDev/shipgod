using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PO> POs => Set<PO>();
    public DbSet<ModelConfig> ModelConfigs => Set<ModelConfig>();
    public DbSet<ShipmentGroup> ShipmentGroups => Set<ShipmentGroup>();
    public DbSet<ShipmentGroupPO> ShipmentGroupPOs => Set<ShipmentGroupPO>();
    public DbSet<GroupBreakdown> GroupBreakdowns => Set<GroupBreakdown>();
    public DbSet<BarcodeUnit> BarcodeUnits => Set<BarcodeUnit>();
    public DbSet<ExcelUploadSession> ExcelUploadSessions => Set<ExcelUploadSession>();
    public DbSet<ExcelUploadItem> ExcelUploadItems => Set<ExcelUploadItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    modelBuilder.Entity<ModelConfig>().HasIndex(m => new { m.ModelCode, m.Method, m.IsActive });
    modelBuilder.Entity<ShipmentGroup>().HasIndex(g => g.GroupSignature).IsUnique();
    modelBuilder.Entity<BarcodeUnit>().HasIndex(b => b.Code).IsUnique();
    }
}
