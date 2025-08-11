using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Services;

public interface IModelConfigService
{
    Task<ModelConfig?> GetActiveAsync(string modelCode, string method);
    Task<ModelConfig?> GetByModelAsync(string modelCode);
    Task<bool> ValidateModelSupportAsync(string modelCode, string shipmentMethod);
    Task<(int pcsPerPallet, int pcsPerBox)> GetCapacityAsync(string modelCode, string shipmentMethod);
}
