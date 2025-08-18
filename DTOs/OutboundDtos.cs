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
}
