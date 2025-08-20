using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface ISmartBarcodeManager
    {
        Task<BarcodeChangeResult> ManageBarcodeChanges(string qrIdentity, string modelName, int oldBoxCount, int newBoxCount, string operatorId);
        Task<List<BarcodeRegistryInfo>> GetBarcodesByQRIdentityAsync(string qrIdentity, string? modelName = null);
        Task<BarcodeStatistics> GetBarcodeStatisticsAsync(string qrIdentity);
        Task<string> GetBarcodeStatusDebugInfo(string qrIdentity, string modelName);
    }

    public interface IScanningProtectionService
    {
        Task<ConflictResolutionResult> CheckScanningConflicts(string qrIdentity, string modelName, int oldBoxCount, int newBoxCount);
    }
}
