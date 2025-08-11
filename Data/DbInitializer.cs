using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Data.Seeders;

namespace ShipmentFinishGood.Data.Seeders;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext ctx)
    {
        await ctx.Database.MigrateAsync();
        await UserSeeder.SeedAsync(ctx);
        // Use updated model config with exact data from user requirements
        await ModelConfigSeeder.SeedAsync(ctx);
    }
}
