using ShipmentFinishGood.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShipmentFinishGood.Services
{
    public interface IModelConfigurationService
    {
        Task<IEnumerable<ModelConfiguration>> GetAllAsync();
        Task<ModelConfiguration?> GetByIdAsync(int id);
        Task AddAsync(ModelConfiguration modelConfig);
        Task UpdateAsync(ModelConfiguration modelConfig);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
