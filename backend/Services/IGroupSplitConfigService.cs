using MyFinBackend.Dto;

namespace MyFinBackend.Services
{
    public interface IGroupSplitConfigService
    {
        Task<ServiceResult<List<GroupSplitConfigReturnDto>>> GetByGroupIdAsync(int groupId, string contextUserId);
        Task<ServiceResult<GroupSplitConfigReturnDto>> CreateAsync(int groupId, string contextUserId, CreateGroupSplitConfigDto dto);
        Task<ServiceResult<GroupSplitConfigReturnDto>> UpdateAsync(int configId, string contextUserId, UpdateGroupSplitConfigDto dto);
        Task<ServiceResult> DeleteAsync(int configId, string contextUserId);
        Task OnMemberJoinedAsync(int groupId, string newUserId);
        Task OnMemberLeftAsync(int groupId, string userId);
    }
}
