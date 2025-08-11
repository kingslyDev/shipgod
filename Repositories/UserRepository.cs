using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;
    public UserRepository(AppDbContext ctx) { _ctx = ctx; }

    public Task<User?> GetByIdAsync(int id) => _ctx.Users.FirstOrDefaultAsync(u => u.UserId == id && !u.IsDeleted);
    public Task<User?> GetByUsernameAsync(string username) => _ctx.Users.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    public async Task<IEnumerable<User>> GetAllAsync() => await _ctx.Users.AsNoTracking().Where(u=>!u.IsDeleted).OrderBy(u=>u.Username).ToListAsync();
    public async Task<(IEnumerable<User> Users,int TotalCount)> GetPagedAsync(int page,int pageSize,string? search=null)
    {
        var query = _ctx.Users.AsNoTracking().Where(u=>!u.IsDeleted);
        if(!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(u=> u.Username.ToLower().Contains(search) || u.Name.ToLower().Contains(search) || u.Role.ToString().ToLower().Contains(search));
        }
        var total = await query.CountAsync();
        var users = await query.OrderBy(u=>u.Username).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return (users,total);
    }
    public async Task AddAsync(User user) => await _ctx.Users.AddAsync(user);
    public Task UpdateAsync(User user)
    {
        _ctx.Users.Update(user);
        return Task.CompletedTask;
    }
    public Task DeleteAsync(User user)
    {
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        _ctx.Users.Update(user);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
}
