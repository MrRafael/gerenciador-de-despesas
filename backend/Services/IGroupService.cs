using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public interface IGroupService
    {
        Task<ServiceResult<List<GroupMemberDto>>> GetGroupsByUserIdAsync(string userId, string contextUserId);
        Task<ServiceResult<List<GroupMemberDto>>> GetInvitesByUserIdAsync(string userId, string contextUserId);
        Task<ServiceResult<List<GroupMemberDto>>> GetMembersByGroupIdAsync(int groupId);
        Task<ServiceResult<GroupMemberDto>> CreateGroupAsync(Group group, string contextUserId);
        Task<ServiceResult<GroupMemberDto>> AcceptInviteAsync(GroupMember invite, string contextUserId);
        Task<ServiceResult> RefuseInviteAsync(string userId, int groupId, string contextUserId);
        Task<ServiceResult> DeleteMemberAsync(string userId, int groupId, string contextUserId);
        Task<ServiceResult> DeleteGroupAsync(int groupId, string contextUserId);
        Task<ServiceResult<GroupMemberDto>> InviteMemberAsync(MemberGrouToAddDto memberGroup, string contextUserId);
        Task<ServiceResult<List<GroupExpenseDto>>> GetGroupExpensesAsync(int groupId, string contextUserId, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult> SetMemberSalaryAsync(int groupId, string userId, decimal? salary, string contextUserId);
    }
}
