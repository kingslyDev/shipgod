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
        Task<Result<ScanResultDto>> ScanItemBarcodeAsync(int sessionId, string barcode, string scannedBy);
        Task<ScanProgressDto> GetScanProgressAsync(int sessionId);
        Task<List<BarcodeItemDto>> GetBarcodeListAsync(int sessionId);
        Task<List<ScanHistoryDto>> GetScanHistoryAsync();
        Task<ScanSessionDetailDto?> GetSessionDetailAsync(int sessionId);
        Task<Result<bool>> CompleteScanAsync(int sessionId, string completedBy);
        Task<Result<bool>> CheckUserLockAsync(string userId);
        Task<Result<string>> GetUserLockedSessionAsync(string userId);
        Task<Result<string>> GenerateQRForSessionAsync(int sessionId);
    }
}
