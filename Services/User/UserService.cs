using ShipmentFinishGood.Domain;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Utilities;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public async Task<Result<User>> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Result<User>.Failure(ErrorMessages.InvalidCredentials);

        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !PasswordHasher.Verify(user.Password, password))
            return Result<User>.Failure(ErrorMessages.InvalidCredentials);

        return Result<User>.Success(user);
    }

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        var existing = await _repo.GetByUsernameAsync(request.Username);
        if (existing != null)
            return Result<User>.Failure(ErrorMessages.UsernameAlreadyExists);

        var user = new User
        {
            Username = request.Username,
            Password = PasswordHasher.Hash(request.Password),
            Name = request.Name,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user);
        await _repo.SaveChangesAsync();
        return Result<User>.Success(user);
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _repo.GetAllAsync();
        var userDtos = users.Select(u => new UserDto
        {
            UserId = u.UserId,
            Username = u.Username,
            Name = u.Name,
            Role = u.Role,
            CreatedDate = u.CreatedAt
        });
        return Result<IEnumerable<UserDto>>.Success(userDtos);
    }

    public async Task<Result<(IEnumerable<UserDto> Users,int Total,int Page,int PageSize,string? Search)>> GetPagedAsync(int page,int pageSize,string? search)
    {
        if(page < 1) page = 1;
        if(pageSize < 1 || pageSize > 100) pageSize = 10;
        var (users,total) = await _repo.GetPagedAsync(page,pageSize,search);
        var dtos = users.Select(u => new UserDto
        {
            UserId = u.UserId,
            Username = u.Username,
            Name = u.Name,
            Role = u.Role,
            CreatedDate = u.CreatedAt
        });
        return Result<(IEnumerable<UserDto>, int, int, int, string?)>.Success((dtos,total,page,pageSize,search));
    }

    public async Task<Result<User>> GetAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user == null 
            ? Result<User>.Failure(ErrorMessages.UserNotFound)
            : Result<User>.Success(user);
    }

    public async Task<Result> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null)
            return Result.Failure(ErrorMessages.UserNotFound);

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = PasswordHasher.Hash(request.Password);
        
        user.Name = request.Name;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(user);
        await _repo.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null)
            return Result.Failure(ErrorMessages.UserNotFound);

        await _repo.DeleteAsync(user);
        await _repo.SaveChangesAsync();
        return Result.Success();
    }
}
