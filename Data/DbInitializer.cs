using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Data.Seeders;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext ctx)
    {
        await ctx.Database.MigrateAsync();
        await UserSeeder.SeedAsync(ctx);
    }
}
