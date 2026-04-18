using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class UserService(FinanceContext db) : IUserService
    {
        public async Task<ServiceResult<UserDto>> GetByIdAsync(string id, string contextUserId)
        {
            if (id != contextUserId)
                return ServiceResult<UserDto>.Fail(ServiceError.Unauthorized);

            var user = await db.Users.FindAsync(id);
            if (user == null)
                return ServiceResult<UserDto>.Fail(ServiceError.NotFound);

            return ServiceResult<UserDto>.Ok(ToDto(user));
        }

        public async Task<ServiceResult<UserDto>> CreateAsync(User user, string contextUserId)
        {
            if (user.Id != contextUserId)
                return ServiceResult<UserDto>.Fail(ServiceError.Unauthorized);

            var exists = await db.Users.AnyAsync(u => u.Id == user.Id);
            if (exists)
                return ServiceResult<UserDto>.Fail(ServiceError.Conflict);

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return ServiceResult<UserDto>.Ok(ToDto(user));
        }

        public async Task<ServiceResult> UpdateAsync(string id, User user, string contextUserId)
        {
            if (id != user.Id || id != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            var exists = await db.Users.AnyAsync(u => u.Id == id);
            if (!exists)
                return ServiceResult.Fail(ServiceError.NotFound);

            db.Entry(user).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteAsync(string id, string contextUserId)
        {
            if (id != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            var user = await db.Users.FindAsync(id);
            if (user == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Photo = u.Photo
        };
    }
}
