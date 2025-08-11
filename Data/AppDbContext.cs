using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}
