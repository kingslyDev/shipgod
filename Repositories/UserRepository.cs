using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;
    public UserRepository(AppDbContext ctx) { _ctx = ctx; }

    public Task<User?> GetByIdAsync(int id) => _ctx.Users.FirstOrDefaultAsync(u => u.UserId == id);
    public Task<User?> GetByUsernameAsync(string username) => _ctx.Users.FirstOrDefaultAsync(u => u.Username == username);
    public async Task<IEnumerable<User>> GetAllAsync() => await _ctx.Users.AsNoTracking().OrderBy(u=>u.Username).ToListAsync();
    public async Task AddAsync(User user) => await _ctx.Users.AddAsync(user);
    public Task UpdateAsync(User user)
    {
        _ctx.Users.Update(user);
        return Task.CompletedTask;
    }
    public Task DeleteAsync(User user)
    {
        _ctx.Users.Remove(user);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
}
