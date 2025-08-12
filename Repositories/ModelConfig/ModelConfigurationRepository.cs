using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShipmentFinishGood.Repositories
{
    public class ModelConfigurationRepository : IModelConfigurationRepository
    {
        private readonly AppDbContext _context;
        public ModelConfigurationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ModelConfiguration>> GetAllAsync()
        {
            return await _context.ModelConfigurations.AsNoTracking().ToListAsync();
        }

        public async Task<ModelConfiguration?> GetByIdAsync(int id)
        {
            return await _context.ModelConfigurations.FindAsync(id);
        }

        public async Task AddAsync(ModelConfiguration modelConfig)
        {
            _context.ModelConfigurations.Add(modelConfig);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ModelConfiguration modelConfig)
        {
            _context.ModelConfigurations.Update(modelConfig);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ModelConfigurations.FindAsync(id);
            if (entity != null)
            {
                _context.ModelConfigurations.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ModelConfigurations.AnyAsync(e => e.ConfigId == id);
        }
    }
}
