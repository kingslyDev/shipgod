namespace ShipmentFinishGood.DTOs
{
    // DTO for outbound item tracking
    public class OutboundItemDto
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
        public string LineNumber { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Country { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public DateTime? ScannedDate { get; set; }
        public string Status { get; set; } = string.Empty; // Generated, Scanned, Ready to Ship, Shipped
        public string? Area { get; set; }
        public string? ScannedBy { get; set; }
        public string? ShipDate { get; set; }
        public int DaysInProcess => (DateTime.Now - GeneratedDate).Days;
        public bool IsDelayed => DaysInProcess > 3 && Status != "Shipped";
    }

    // DTO for outbound tracking dashboard
    public class OutboundTrackingDto
    {
        public List<OutboundItemDto> OutboundItems { get; set; } = new();
        public int TotalItemsGenerated { get; set; }
        public int TotalItemsScanned { get; set; }
        public int TotalItemsShipped { get; set; }
        public int TotalItemsPending { get; set; }
        public List<string> AvailableCountries { get; set; } = new();
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, int> CountryBreakdown { get; set; } = new();
        public List<DailyOutboundTrendDto> DailyTrends { get; set; } = new();
    }

    // DTO for daily trends
    public class DailyOutboundTrendDto
    {
        public DateTime Date { get; set; }
        public int Generated { get; set; }
        public int Scanned { get; set; }
        public int Shipped { get; set; }
    }

    // Chart data DTOs
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Data { get; set; } = new();
        public List<string> BackgroundColors { get; set; } = new();
    }

    public class OutboundAnalyticsDto
    {
        public ChartDataDto StatusChart { get; set; } = new();
        public ChartDataDto CountryChart { get; set; } = new();
        public ChartDataDto TrendChart { get; set; } = new();
        public double EfficiencyRate { get; set; }
        public double AverageProcessingTime { get; set; }
        public int TotalDelayedItems { get; set; }
    }

    // =====  HIERARCHICAL LOCK SYSTEM DTOs (PHASE 2) =====

    /// <summary>
    /// DTO for session validation results in hierarchical lock system
    /// </summary>
    public class SessionValidationDto
    {
        public int SessionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanScan { get; set; }
        public string? CompletionReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string QRIdentity { get; set; } = string.Empty;
        public int TotalPOs { get; set; }
        public int CompletedPOs { get; set; }
        public double ProgressPercentage { get; set; }
    }

    /// <summary>
    /// DTO for PO validation results in hierarchical lock system
    /// </summary>
    public class POValidationDto
    {
        public int POId { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool CanContinueScanning { get; set; }
        public string? NextAvailableItemType { get; set; }
        public POProgressSummaryDto Progress { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for PO availability in session context
    /// </summary>
    public class POAvailabilityDto
    {
        public int POId { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
        public POProgressSummaryDto Progress { get; set; } = new();
        public List<string> AvailableItemTypes { get; set; } = new();
        public string? NextItemType { get; set; }
    }

    /// <summary>
    /// DTO for PO progress summary in hierarchical lock context
    /// </summary>
    public class POProgressSummaryDto
    {
        public int TotalBoxes { get; set; }
        public int ScannedBoxes { get; set; }
        public int TotalPallets { get; set; }
        public int ScannedPallets { get; set; }
        public int TotalPcs { get; set; }
        public int ScannedPcs { get; set; }
        
        public double BoxProgressPercentage => TotalBoxes > 0 ? (double)ScannedBoxes / TotalBoxes * 100 : 0;
        public double PalletProgressPercentage => TotalPallets > 0 ? (double)ScannedPallets / TotalPallets * 100 : 0;
        public double PcsProgressPercentage => TotalPcs > 0 ? (double)ScannedPcs / TotalPcs * 100 : 0;
        
        public bool IsBoxComplete => TotalBoxes > 0 && ScannedBoxes >= TotalBoxes;
        public bool IsPalletComplete => TotalPallets > 0 && ScannedPallets >= TotalPallets;
        public bool IsPcsComplete => TotalPcs > 0 && ScannedPcs >= TotalPcs;
        public bool IsAllComplete => (TotalBoxes == 0 || IsBoxComplete) && 
                                   (TotalPallets == 0 || IsPalletComplete) && 
                                   (TotalPcs == 0 || IsPcsComplete);
        
        public double OverallProgressPercentage
        {
            get
            {
                var totalItems = TotalBoxes + TotalPallets + TotalPcs;
                if (totalItems == 0) return 100;
                
                var scannedItems = ScannedBoxes + ScannedPallets + ScannedPcs;
                return (double)scannedItems / totalItems * 100;
            }
        }
    }
}
