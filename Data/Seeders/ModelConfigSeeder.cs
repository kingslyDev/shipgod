using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Data.Seeders;

public static class ModelConfigSeeder
{
    // Data sesuai requirement user - EXACT data yang diminta
    // Setiap model akan memiliki entry terpisah untuk LOOSE dan PALLET (jika support)
    private static readonly ModelConfig[] SeedData = new[]
    {
        // LOOSE entries - semua model support LOOSE
        new ModelConfig { ModelCode = "RF-D10EB-K", Method = "LOOSE", PcsPerPallet = 192, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-D10EG-K", Method = "LOOSE", PcsPerPallet = 192, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-D10EG-W", Method = "LOOSE", PcsPerPallet = 192, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-D10GN-K", Method = "LOOSE", PcsPerPallet = 192, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "R-2255-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2450-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEB-K", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEE-K", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEG-K", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DGN-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DGT-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DPC-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DP-S", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DP-K", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DLJ-K", Method = "LOOSE", PcsPerPallet = 240, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-P155-S", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P55-S", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P150DEG-S", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P150DGC-S", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P150DGT-S", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P150DBAGA", Method = "LOOSE", PcsPerPallet = 800, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DGC-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DGC-R", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DEG-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DLJ-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DPR-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DPP-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-562DDGC-K", Method = "LOOSE", PcsPerPallet = 350, PcsPerBox = 5 },
        new ModelConfig { ModelCode = "RF-U156-S", Method = "LOOSE", PcsPerPallet = 450, PcsPerBox = 6 },
        new ModelConfig { ModelCode = "RF-NA35R-S", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-NA35R", Method = "LOOSE", PcsPerPallet = 1000, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-5270LJ-K", Method = "LOOSE", PcsPerPallet = 600, PcsPerBox = 20 },
        
        // PALLET entries - hanya model yang support PALLET
        new ModelConfig { ModelCode = "RF-562DDGC-K", Method = "PALLET", PcsPerPallet = 210, PcsPerBox = 5 },
        new ModelConfig { ModelCode = "RF-P150DGC-S", Method = "PALLET", PcsPerPallet = 1600, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P150DEG-S", Method = "PALLET", PcsPerPallet = 1600, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DGC-R", Method = "PALLET", PcsPerPallet = 1600, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-P50DEG-S", Method = "PALLET", PcsPerPallet = 1200, PcsPerBox = 20 },
        new ModelConfig { ModelCode = "RF-D10EG-K", Method = "PALLET", PcsPerPallet = 144, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-D10GN-K", Method = "PALLET", PcsPerPallet = 126, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2450-S", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "R-2255-S", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DPC-S", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DP-S", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DP-K", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEE-K", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEG-K", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DGN-S", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
        new ModelConfig { ModelCode = "RF-2400DEB-K", Method = "PALLET", PcsPerPallet = 216, PcsPerBox = 3 },
    };

    public static async Task SeedAsync(AppDbContext ctx)
    {
        // Clear existing data first untuk avoid conflicts
        if (ctx.ModelConfigs.Any())
        {
            var existing = ctx.ModelConfigs.ToList();
            ctx.ModelConfigs.RemoveRange(existing);
            await ctx.SaveChangesAsync();
        }

        foreach (var config in SeedData)
        {
            config.IsActive = true;
            config.EffectiveFrom = DateTime.UtcNow.Date;
            ctx.ModelConfigs.Add(config);
        }
        
        await ctx.SaveChangesAsync();
    }
}
