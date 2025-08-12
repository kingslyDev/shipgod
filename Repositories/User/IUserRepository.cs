using ShipmentFinishGood.Models;
using ShipmentFinishGood.Domain;

namespace ShipmentFinishGood.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<(IEnumerable<User> Users,int TotalCount)> GetPagedAsync(int page,int pageSize,string? search=null);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task SaveChangesAsync();
}
