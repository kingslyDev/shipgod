using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShipmentFinishGood.Services
{
    public class ModelConfigurationService : IModelConfigurationService
    {
        private readonly IModelConfigurationRepository _repository;
        public ModelConfigurationService(IModelConfigurationRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<ModelConfiguration>> GetAllAsync() => _repository.GetAllAsync();
        public Task<ModelConfiguration?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task AddAsync(ModelConfiguration modelConfig) => _repository.AddAsync(modelConfig);
        public Task UpdateAsync(ModelConfiguration modelConfig) => _repository.UpdateAsync(modelConfig);
        public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
        public Task<bool> ExistsAsync(int id) => _repository.ExistsAsync(id);
    }
}
