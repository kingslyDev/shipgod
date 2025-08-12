using ShipmentFinishGood.Domain;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services;

public interface IUserService
{
    Task<Result<User>> AuthenticateAsync(string username, string password);
    Task<Result<User>> CreateUserAsync(CreateUserRequest request);
    Task<Result<IEnumerable<UserDto>>> GetAllAsync();
    Task<Result<(IEnumerable<UserDto> Users,int Total,int Page,int PageSize,string? Search)>> GetPagedAsync(int page,int pageSize,string? search);
    Task<Result<User>> GetAsync(int id);
    Task<Result> UpdateAsync(int id, UpdateUserRequest request);
    Task<Result> DeleteAsync(int id);
}
