using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services
{
    public interface IFinalProcessingService
    {
        Task<FinalTableDto?> GetFinalDataAsync(int sessionId);
        Task<Result<FinalRowData>> UpdateFinalRowAsync(int sessionId, int rowIndex, FinalRowUpdateRequest request);
        Task<bool> SaveMetadataAsync(int sessionId, List<FinalRowMetadata> metadata, string updatedBy);
        Task<QRManagementDto?> GenerateQRIdentityAsync(int sessionId, string generatedBy);
        
        // 🧠 SMART AUTO-CALCULATION ENGINE
        Task<SmartUpdateResult> SmartUpdateRowAsync(int sessionId, int rowIndex, SmartUpdateRequest request);
    }
}
