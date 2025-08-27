using System;
using System.Collections.Generic;

namespace ShipmentFinishGood.DTOs
{
    public class ShippingDashboardDto
    {
        // Legacy properties (maintaining compatibility)
        public int TotalOrders { get; set; }
        public int CompletedShipments { get; set; }
        public int PendingShipments { get; set; }
        public int ProcessingShipments { get; set; }
        public double CompletionRate => TotalOrders > 0 ? (double)CompletedShipments / TotalOrders * 100 : 0;
        public List<ShipmentSummaryDto> RecentShipments { get; set; } = new List<ShipmentSummaryDto>();
        
        // Enhanced executive metrics
        public int TotalItemsGenerated { get; set; }
        public int TotalItemsScanned { get; set; }
        public int TotalItemsShipped { get; set; }
        public int TotalItemsPending { get; set; }
        
        // Today's performance KPIs
        public int TodayGenerated { get; set; }
        public int TodayScanned { get; set; }
        public double TodayCompletionRate { get; set; }
        
        // Scanner metrics
        public int ActiveScanners { get; set; }
        public int TotalScanners { get; set; }
        public double AverageLeadTime { get; set; }
        
        // Operational efficiency
        public double OverallEfficiency { get; set; }
        public int ActiveSessions { get; set; }
        public int CompletedSessions { get; set; }
        
        // Business intelligence
        public Dictionary<string, int> CountryDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> HourlyTrends { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopModels { get; set; } = new Dictionary<string, int>();
        
        // Detailed data for drill-down
        public List<SessionMetric> SessionMetrics { get; set; } = new List<SessionMetric>();
        public List<OutboundItemDto> RecentItems { get; set; } = new List<OutboundItemDto>();
        public List<OutboundItemDto> OutboundItems { get; set; } = new List<OutboundItemDto>(); // For compatibility
        public List<CountryModelStat> CountryModelStats { get; set; } = new List<CountryModelStat>();
        
        // Filter options
        public List<string> AvailableCountries { get; set; } = new List<string>();
        public List<string> AvailableModels { get; set; } = new List<string>();
    }

    // Simple executive dashboard focused view model
    public class SimpleShippingDashboardViewModel
    {
        // KPI
        public int TotalItems { get; set; }
        public int ScannedItems { get; set; }
        public int ShippedItems { get; set; }
        public int PendingItems => TotalItems - ScannedItems;
        public double CompletionRate => TotalItems > 0 ? (double)ScannedItems / TotalItems * 100 : 0;

        // Filters (selected values)
        public string? SelectedCountry { get; set; }
        public string? SelectedModel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Options
        public List<string> AvailableCountries { get; set; } = new();
        public List<string> AvailableModels { get; set; } = new();

        // Charts / Aggregations
        public Dictionary<string,int> CountryVolumes { get; set; } = new(); // For bar chart (absolute)
        public Dictionary<string,double> CountryShares { get; set; } = new(); // For pie (percentage)

        // Tables
        public List<CountrySummaryRow> TopCountries { get; set; } = new();
        public List<OutboundItemDto> RecentActivities { get; set; } = new();
    }

    public class CountrySummaryRow
    {
        public string Country { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Scanned { get; set; }
        public double CompletionRate => Total > 0 ? (double)Scanned / Total * 100 : 0;
        public string StatusTag => CompletionRate switch
        {
            >= 90 => "Excellent",
            >= 70 => "Good",
            _ => "Needs Attention"
        };
        public string StatusColor => StatusTag switch
        {
            "Excellent" => "#28a745",
            "Good" => "#ffc107",
            _ => "#dc3545"
        };
    }
    
    public class SessionMetric
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int ScannedItems { get; set; }
        public double CompletionRate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Country { get; set; } = string.Empty;
        
        public string StatusClass => Status switch
        {
            "SCAN_COMPLETED" => "success",
            "IN_PROGRESS" => "warning", 
            "QR_GENERATED" => "info",
            "VALIDATED" => "info",
            _ => "secondary"
        };
        
        public string ProgressClass => CompletionRate switch
        {
            >= 100 => "success",
            >= 75 => "info",
            >= 50 => "warning",
            _ => "danger"
        };
    }

    public class CountryModelStat
    {
        public string Country { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TotalShipped { get; set; }
        public double CompletionRate { get; set; }
        public double AvgLeadTime { get; set; }
    // New: representative shipment date for this country+model group
    public DateTime? ShipmentDate { get; set; }
    }

    public class ShipmentSummaryDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public DateTime CreatedDate { get; set; }
        
        public string StatusClass => Status switch
        {
            "SCAN_COMPLETED" => "success",
            "IN_PROGRESS" => "warning",
            "VALIDATED" => "info",
            "QR_GENERATED" => "info",
            _ => "secondary"
        };

        public string StatusText => Status switch
        {
            "SCAN_COMPLETED" => "Completed",
            "IN_PROGRESS" => "In Progress",
            "VALIDATED" => "Validated",
            "QR_GENERATED" => "QR Generated",
            _ => "Unknown"
        };
    }

    public class ShippingReportDto
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalBoxes { get; set; }
        public Dictionary<string, int> SessionsByStatus { get; set; } = new Dictionary<string, int>();
        public List<MonthlyShipmentDto> MonthlyReport { get; set; } = new List<MonthlyShipmentDto>();
    }

    public class MonthlyShipmentDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalSessions { get; set; }
        public int TotalBoxes { get; set; }
        public int CompletedSessions { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
    }
}
