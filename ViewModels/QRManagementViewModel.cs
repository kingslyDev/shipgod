using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.ViewModels
{
    public class QRManagementViewModel
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string QRImageBase64 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        
        // Quantities
        public int TotalBoxes { get; set; }
        public int TotalPallets { get; set; }
        public int TotalPcs { get; set; }
        public int TotalQty { get; set; }
        
        // Scanning progress
        public int TotalScanned { get; set; }
        public int ScannedBoxes { get; set; }
        public int ScannedPallets { get; set; }
        public int ScannedPcs { get; set; }
        public int ScanPercentage { get; set; }
        
        // Last scan info
        public DateTime? LastScannedDate { get; set; }
        public string? LastScannedBy { get; set; }
        
        // Collections
        public List<string> BarcodeList { get; set; } = new();
        public List<POSummaryInfo> POSummaries { get; set; } = new();
        
        // Flags
        public bool CanRegenerate { get; set; } = false;
        public bool CanComplete { get; set; } = false;

        // Static factory method to create from DTO
        public static QRManagementViewModel FromDto(QRManagementDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new QRManagementViewModel
            {
                SessionId = dto.SessionId,
                FileName = dto.FileName ?? string.Empty,
                SheetName = dto.SheetName ?? string.Empty,
                QRIdentity = dto.QRIdentity ?? string.Empty,
                QRImageBase64 = dto.QRImageBase64 ?? string.Empty,
                Status = dto.Status ?? string.Empty,
                GeneratedDate = dto.GeneratedDate,
                GeneratedBy = dto.GeneratedBy ?? string.Empty,
                TotalBoxes = dto.TotalBoxes,
                TotalPallets = dto.TotalPallets,
                TotalPcs = dto.TotalPcs,
                TotalQty = dto.TotalQty,
                TotalScanned = dto.TotalScanned,
                ScannedBoxes = dto.ScannedBoxes,
                ScannedPallets = dto.ScannedPallets,
                ScannedPcs = dto.ScannedPcs,
                ScanPercentage = dto.ScanPercentage,
                LastScannedDate = dto.LastScannedDate,
                LastScannedBy = dto.LastScannedBy,
                BarcodeList = dto.BarcodeList ?? new List<string>(),
                POSummaries = dto.POSummaries ?? new List<POSummaryInfo>(),
                CanRegenerate = dto.CanRegenerate,
                CanComplete = dto.CanComplete
            };
        }
    }
}
