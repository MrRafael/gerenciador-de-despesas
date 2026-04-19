using MyFinBackend.Dto;

namespace MyFinBackend.Services
{
    public interface IMonthCloseService
    {
        Task<ServiceResult<List<PendingMonthDto>>> GetPendingMonthsAsync(int groupId, string contextUserId);
        Task<ServiceResult<MonthCloseStatusDto>> GetStatusAsync(int groupId, int month, int year, string contextUserId);
        Task<ServiceResult<bool>> ConfirmAsync(int groupId, string userId, int month, int year);
        Task<ServiceResult> UnconfirmAsync(int groupId, string userId, int month, int year);
        Task<bool> IsClosedMonthAsync(int groupId, int month, int year);
    }
}
