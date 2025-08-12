namespace ShipmentFinishGood.DTOs
{
    public class ExcelRowData
    {
        public string? NoPO { get; set; }
        public string? Model { get; set; }
        public int Qty { get; set; }
        public int RowIndex { get; set; }
    }

    public class ProcessedPOData
    {
        public string NoPO { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public int QtyPallet { get; set; }
        public int QtyBox { get; set; }
        public int QtyPcs { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
        public bool CanEdit { get; set; } = true;
    }

    public class UploadPreviewDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public List<ExcelRowData> RawData { get; set; } = new();
        public List<ProcessedPOData> ProcessedData { get; set; } = new();
        public string? ShipmentType { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public string? PalletPrefix { get; set; }
        public string? PcsPrefix { get; set; }
    }

    public class CalculationRequest
    {
        public int SessionId { get; set; }
        public string ShipmentType { get; set; } = string.Empty; // LOOSE or PALLET
        public DateTime ShipmentDate { get; set; }
        public string? PalletPrefix { get; set; }
        public string? PcsPrefix { get; set; }
        public List<ProcessedPOData> ProcessedData { get; set; } = new();
    }

    public class FinalTableDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ShipmentType { get; set; } = string.Empty;
        public DateTime? ShipmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<FinalRowData> FinalData { get; set; } = new();
        public string? QRIdentity { get; set; }
        public bool HasQRGenerated { get; set; }
    }

    public class FinalRowData
    {
        public int RowIndex { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public int QtyPallet { get; set; }
        public int QtyBox { get; set; }
        public int QtyPcs { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
        public bool IsEditable { get; set; } = true;
    }

    public class FinalRowUpdateRequest
    {
        public int TotalQty { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
    }

    public class FinalRowMetadata
    {
        public int RowIndex { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
    }

    // Requests used by FinalController (AJAX payload wrappers)
    public class UpdateRowCommand
    {
        public int SessionId { get; set; }
        public int RowIndex { get; set; }
        public FinalRowUpdateRequest Request { get; set; } = new();
    }

    public class SaveMetadataCommand
    {
        public int SessionId { get; set; }
        public List<FinalRowMetadata> Metadata { get; set; } = new();
    }

    public class QRManagementDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string QRImageBase64 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public List<string> BarcodeList { get; set; } = new();
        public bool CanRegenerate { get; set; } = true;
    }

    public class POMasterDto
    {
        public int POId { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string ModelProduk { get; set; } = string.Empty;
        public int QtyTotal { get; set; }
        public int QtyPallet { get; set; }
        public int QtyBox { get; set; }
        public int QtyPcs { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ShipmentMethod { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        
        // Session info
        public int? SourceSessionId { get; set; }
        public string? FileName { get; set; }
        public string? QRIdentity { get; set; }
        
        // Session-level scanning progress
        public int SessionTotalItems { get; set; }
        public int SessionScannedItems { get; set; }
        public double SessionScanProgress => SessionTotalItems > 0 ? (double)SessionScannedItems / SessionTotalItems * 100 : 0;
        public string SessionProgressStatus => SessionScanProgress >= 100 ? "Completed" : 
                                              SessionScanProgress > 0 ? "In Progress" : "Not Started";
        public int SessionTotalPOs { get; set; }
    }

    public class POSessionSummaryDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string? ShipmentType { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? QRIdentity { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        
        // Summary data
        public int TotalPOs { get; set; }
        public int TotalQty { get; set; }
        public int TotalBoxes { get; set; }
        public int TotalPallets { get; set; }
        
        // Progress tracking
        public int TotalItemsToScan { get; set; }
        public int ScannedItems { get; set; }
        public double ScanProgress => TotalItemsToScan > 0 ? (double)ScannedItems / TotalItemsToScan * 100 : 0;
        public string ProgressStatus => ScanProgress >= 100 ? "Completed" : 
                                       ScanProgress > 0 ? "In Progress" : "Not Started";
        
        // Individual POs in this session
        public List<POMasterDto> POs { get; set; } = new();
        
        public bool HasQRCode => !string.IsNullOrEmpty(QRIdentity);
        public bool CanStartScanning => HasQRCode && Status == "QR_GENERATED";
    }

    // Scanning DTOs
    public class ScanSessionSummaryDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string ShipmentType { get; set; } = string.Empty;
        public DateTime? ShipmentDate { get; set; }
        public int TotalBoxes { get; set; }
        public int TotalPOs { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Ready, In Progress, Completed
        public string? AssignedArea { get; set; }
    }

    public class ScanSessionDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string ShipmentType { get; set; } = string.Empty;
        public DateTime? ShipmentDate { get; set; }
        public int TotalBoxes { get; set; }
        public int TotalBarcodes { get; set; }
        public int ScannedCount { get; set; }
        public List<BarcodeDto> BarcodeList { get; set; } = new();
        public List<string> ScannedBarcodes { get; set; } = new();
        public bool IsMasterScanned { get; set; }
        public string? AssignedArea { get; set; }
        public bool CanComplete { get; set; }
        public double ProgressPercentage => TotalBarcodes > 0 ? (double)ScannedCount / TotalBarcodes * 100 : 0;
    }

    public class BarcodeDto
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public int BoxNumber { get; set; }
    }

    public class BarcodeItemDto
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string ModelProduct { get; set; } = string.Empty;
        public int BoxNumber { get; set; }
        public bool IsScanned { get; set; }
        public DateTime? ScannedTime { get; set; }
    }

    public class ScanResultDto
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string ScanType { get; set; } = string.Empty; // MASTER_QR, BOX_BARCODE
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ScannedBy { get; set; } = string.Empty;
    }

    public class ScanProgressDto
    {
        public int SessionId { get; set; }
        public int TotalBarcodes { get; set; }
        public int ScannedCount { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsMasterScanned { get; set; }
        public string? AssignedArea { get; set; }
        public DateTime? LastScanTime { get; set; }
        public bool CanComplete { get; set; }
    }

    public class ScanHistoryDto
    {
        public int ActivityId { get; set; }
        public string BarcodeValue { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? AssignedArea { get; set; }
        public DateTime Timestamp { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class ScanSessionDetailDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string ShipmentType { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public List<ScanHistoryDto> ScanActivities { get; set; } = new();
    }
}
