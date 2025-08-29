namespace ShipmentFinishGood.DTOs
{
    public class RecentScanDto
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty; // BOX, PALLET, PCS
        public DateTime ScannedAt { get; set; }
        public string ScannedBy { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        
        // ✨ ENHANCED PROPERTIES
        public string PONumber { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Container { get; set; } = string.Empty;
        public string ShipmentDetail { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string DisplayName => !string.IsNullOrEmpty(ModelProduct) ? ModelProduct : ItemType;
        public string ShortBarcode => BarcodeValue.Length > 25 ? 
            BarcodeValue.Substring(0, 12) + "..." + BarcodeValue.Substring(BarcodeValue.Length - 8) : 
            BarcodeValue;
    }

    public class RecentScansResponseDto
    {
        public List<RecentScanDto> RecentScans { get; set; } = new();
        public int TotalCount { get; set; }
        public int ScannedCount { get; set; }
        public int PendingCount { get; set; }
        public int TotalScannedToday { get; set; }
        public string SessionInfo { get; set; } = string.Empty;
        public string LastScanTime { get; set; } = string.Empty;
        
        // Credible typed stats sourced from POMaster (by SourceSessionId)
        public RecentStatsDto? Stats { get; set; }
    }

    public class RecentStatsDto
    {
        // Totals from POMaster (sum by SourceSessionId)
        public int BoxTotal { get; set; }
        public int PalletTotal { get; set; }
        public int PcsTotal { get; set; }

        // Scanned counts from ScanningActivities (distinct by barcode)
        public int BoxScanned { get; set; }
        public int PalletScanned { get; set; }
        public int PcsScanned { get; set; }
    }
}
