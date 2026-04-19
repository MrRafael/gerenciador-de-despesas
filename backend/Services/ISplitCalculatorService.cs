using MyFinBackend.Dto;

namespace MyFinBackend.Services
{
    public interface ISplitCalculatorService
    {
        Task<ServiceResult<List<SplitMemberResultDto>>> CalculateAsync(int groupId, int month, int year, string contextUserId);
    }
}
