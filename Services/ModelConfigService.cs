using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services;

public class ModelConfigService : IModelConfigService
{
    private readonly AppDbContext _ctx;
    public ModelConfigService(AppDbContext ctx) => _ctx = ctx;

    public async Task<ModelConfig?> GetActiveAsync(string modelCode, string method)
    {
        modelCode = modelCode.Trim().ToUpperInvariant();
        method = method.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow.Date;
        return await _ctx.ModelConfigs
            .Where(c => c.ModelCode == modelCode && c.Method == method && c.IsActive && c.EffectiveFrom <= now)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<ModelConfig?> GetByModelAsync(string modelCode)
    {
        modelCode = modelCode.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow.Date;
        return await _ctx.ModelConfigs
            .Where(c => c.ModelCode == modelCode && c.IsActive && c.EffectiveFrom <= now)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ValidateModelSupportAsync(string modelCode, string shipmentMethod)
    {
        var config = await GetActiveAsync(modelCode, shipmentMethod);
        return config != null;
    }

    public async Task<(int pcsPerPallet, int pcsPerBox)> GetCapacityAsync(string modelCode, string shipmentMethod)
    {
        var config = await GetActiveAsync(modelCode, shipmentMethod);
        if (config == null) 
            throw new InvalidOperationException($"Model {modelCode} tidak mendukung method {shipmentMethod}");

        return (config.PcsPerPallet, config.PcsPerBox);
    }
}
