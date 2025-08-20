using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface ISmartCalculationEngine
    {
        Task<SmartUpdateResult> ExecuteSmartUpdateAsync(int sessionId, int rowIndex, SmartUpdateRequest request);
    }
}
