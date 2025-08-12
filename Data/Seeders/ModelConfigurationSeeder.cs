using ShipmentFinishGood.Models;
using Microsoft.EntityFrameworkCore;

namespace ShipmentFinishGood.Data.Seeders
{
    public static class ModelConfigurationSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModelConfiguration>().HasData(
                new ModelConfiguration { ConfigId = 1, ModelName = "RF-D10EB-K", PcsPerPallet = 192, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 2, ModelName = "RF-D10EG-K", PcsPerPallet = 192, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 3, ModelName = "RF-D10EG-W", PcsPerPallet = 192, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 4, ModelName = "RF-D10GN-K", PcsPerPallet = 192, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 5, ModelName = "R-2255-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 6, ModelName = "RF-2450-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 7, ModelName = "RF-2400DEB-K", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 8, ModelName = "RF-2400DEE-K", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 9, ModelName = "RF-2400DEG-K", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 10, ModelName = "RF-2400DGN-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 11, ModelName = "RF-2400DGT-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 12, ModelName = "RF-2400DPC-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 13, ModelName = "RF-2400DP-S", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 14, ModelName = "RF-2400DP-K", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 15, ModelName = "RF-2400DLJ-K", PcsPerPallet = 240, PcsPerBox = 3, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 16, ModelName = "RF-P155-S", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 17, ModelName = "RF-P55-S", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 18, ModelName = "RF-P150DEG-S", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 19, ModelName = "RF-P150DGC-S", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 20, ModelName = "RF-P150DGT-S", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 21, ModelName = "RF-P150DBAGA", PcsPerPallet = 800, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 22, ModelName = "RF-P50DGC-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 23, ModelName = "RF-P50DGC-R", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 24, ModelName = "RF-P50DEG-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 25, ModelName = "RF-P50DLJ-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 26, ModelName = "RF-P50DPR-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 27, ModelName = "RF-P50DPP-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 28, ModelName = "RF-562DDGC-K", PcsPerPallet = 350, PcsPerBox = 5, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 29, ModelName = "RF-U156-S", PcsPerPallet = 450, PcsPerBox = 6, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 30, ModelName = "RF-NA35R-S", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 31, ModelName = "RF-NA35R", PcsPerPallet = 1000, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 32, ModelName = "RF-5270LJ-K", PcsPerPallet = 600, PcsPerBox = 20, Type = "LOOSE", Description = "LOOSE (Pengiriman tanpa palet)" },
                new ModelConfiguration { ConfigId = 33, ModelName = "RF-562DDGC-K", PcsPerPallet = 210, PcsPerBox = 5, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 34, ModelName = "RF-P150DGC-S", PcsPerPallet = 1600, PcsPerBox = 20, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 35, ModelName = "RF-P150DEG-S", PcsPerPallet = 1600, PcsPerBox = 20, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 36, ModelName = "RF-P50DGC-R", PcsPerPallet = 1600, PcsPerBox = 20, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 37, ModelName = "RF-P50DEG-S", PcsPerPallet = 1200, PcsPerBox = 20, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 38, ModelName = "RF-D10EG-K", PcsPerPallet = 144, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 39, ModelName = "RF-D10GN-K", PcsPerPallet = 126, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 40, ModelName = "RF-2450-S", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 41, ModelName = "R-2255-S", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 42, ModelName = "RF-2400DPC-S", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 43, ModelName = "RF-2400DP-S", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 44, ModelName = "RF-2400DP-K", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 45, ModelName = "RF-2400DEE-K", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 46, ModelName = "RF-2400DEG-K", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 47, ModelName = "RF-2400DGN-S", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" },
                new ModelConfiguration { ConfigId = 48, ModelName = "RF-2400DEB-K", PcsPerPallet = 216, PcsPerBox = 3, Type = "PALLET", Description = "PALLET (Pengiriman dengan palet)" }
            );
        }
    }
}
