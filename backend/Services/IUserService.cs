using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public interface IUserService
    {
        Task<ServiceResult<UserDto>> GetByIdAsync(string id, string contextUserId);
        Task<ServiceResult<UserDto>> CreateAsync(User user, string contextUserId);
        Task<ServiceResult> UpdateAsync(string id, User user, string contextUserId);
        Task<ServiceResult> DeleteAsync(string id, string contextUserId);
    }
}
