using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.ViewModels
{
    public class ShippingMonitoringViewModel
    {
        // Filter parameters
        public string? SelectedCountry { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Summary statistics
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int InProgressSessions { get; set; }
        public int PendingSessions { get; set; }
        public double OverallCompletionRate { get; set; }
        
        // Main monitoring data
        public List<SessionMonitoringInfo> SessionDetails { get; set; } = new();
        
        // Filter options
        public List<string> AvailableCountries { get; set; } = new();
        
        // Additional metrics
        public int TotalQtyScanned { get; set; }
        public int TotalQtyRemaining { get; set; }
        public int TotalQtyTarget { get; set; }
        
        // Real-time scanning metrics
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }
    
    public class SessionMonitoringInfo
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastScanDate { get; set; }
        
        // Basic PO information
        public int TotalPOs { get; set; }
        
        // Quantity tracking (converted to actual pieces using ModelConfig)
        public int TotalQtyTarget { get; set; }  // Total pieces target based on ModelConfig
        public int TotalQtyScanned { get; set; }  // Actual pieces scanned
        public int TotalQtyRemaining { get; set; } // Remaining pieces
        public double ProgressPercentage { get; set; }
        
        // Breakdown by unit type
        public int TotalPallets { get; set; }
        public int ScannedPallets { get; set; }
        public int TotalBoxes { get; set; }
        public int ScannedBoxes { get; set; }
        public int TotalPcs { get; set; }
        public int ScannedPcs { get; set; }
        
        // Smart calculations based on ModelConfig
        public int PalletQtyEquivalent { get; set; } // Pallets converted to pieces
        public int BoxQtyEquivalent { get; set; }    // Boxes converted to pieces
        public int DirectPcsQty { get; set; }        // Direct pieces
        
        // Status and progress indicators
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string ProgressClass { get; set; } = string.Empty;
        
        // Performance metrics
        public double ScanningVelocity { get; set; } // Items per hour
        public TimeSpan? EstimatedCompletion { get; set; }
        
        // Model configuration breakdown
        public List<ModelBreakdown> ModelBreakdowns { get; set; } = new();
    }
    
    public class ModelBreakdown
    {
        public string ModelName { get; set; } = string.Empty;
        public int PcsPerPallet { get; set; }
        public int PcsPerBox { get; set; }
        public string Type { get; set; } = string.Empty; // LOOSE atau PALLET
        
        // Quantities for this model
        public int TotalPallets { get; set; }
        public int TotalBoxes { get; set; }
        public int TotalPcs { get; set; }
        
        // Scanned quantities
        public int ScannedPallets { get; set; }
        public int ScannedBoxes { get; set; }
        public int ScannedPcs { get; set; }
        
        // Calculated total pieces based on ModelConfig
        public int TotalPcsEquivalent => (TotalPallets * PcsPerPallet) + (TotalBoxes * PcsPerBox) + TotalPcs;
        public int ScannedPcsEquivalent => (ScannedPallets * PcsPerPallet) + (ScannedBoxes * PcsPerBox) + ScannedPcs;
        public int RemainingPcsEquivalent => TotalPcsEquivalent - ScannedPcsEquivalent;
        public double ProgressPercentage => TotalPcsEquivalent > 0 ? (double)ScannedPcsEquivalent / TotalPcsEquivalent * 100 : 0;
    }
}
