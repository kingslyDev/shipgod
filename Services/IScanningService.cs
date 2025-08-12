using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services
{
    public interface IScanningService
    {
        Task<List<ScanSessionSummaryDto>> GetActiveSessionsAsync();
        Task<ScanSessionDto?> GetScanSessionAsync(int sessionId);
        Task<Result<ScanResultDto>> ScanMasterQRAsync(int sessionId, string qrCode, string scannedBy);
        Task<Result<ScanResultDto>> ScanBoxBarcodeAsync(int sessionId, string barcode, string scannedBy);
        Task<Result<string>> AssignAreaAsync(int sessionId, string area, string assignedBy);
        Task<ScanProgressDto> GetScanProgressAsync(int sessionId);
        Task<List<BarcodeItemDto>> GetBarcodeListAsync(int sessionId);
        Task<List<ScanHistoryDto>> GetScanHistoryAsync();
        Task<ScanSessionDetailDto?> GetSessionDetailAsync(int sessionId);
        Task<Result<bool>> CompleteScanAsync(int sessionId, string completedBy);
    }
}
