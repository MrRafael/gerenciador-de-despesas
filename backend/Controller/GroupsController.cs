using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/groups")]
    [ApiController]
    [ClerkAuthorize]
    public class GroupsController(IGroupService groupService) : ControllerBase
    {
        [HttpGet("/api/users/{userId}/groups")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetGroupsByUser(string userId)
        {
            var result = await groupService.GetGroupsByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("/api/users/{userId}/group-invitations")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetInvitationsByUser(string userId)
        {
            var result = await groupService.GetInvitesByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("{groupId}/members")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetMembers(int groupId)
        {
            var result = await groupService.GetMembersByGroupIdAsync(groupId);
            return result.IsSuccess ? Ok(result.Data) : NotFound();
        }

        [HttpGet("{groupId}/expenses")]
        public async Task<ActionResult<List<GroupExpenseDto>>> GetExpenses(int groupId, DateOnly startDate, DateOnly endDate)
        {
            var result = await groupService.GetGroupExpensesAsync(groupId, GetUserId(), startDate, endDate);
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpPost]
        public async Task<ActionResult<GroupMemberDto>> CreateGroup(Group group)
        {
            var result = await groupService.CreateGroupAsync(group, GetUserId());
            if (!result.IsSuccess) return BadRequest();
            return CreatedAtAction(nameof(GetMembers), new { groupId = result.Data!.Id }, result.Data);
        }

        [HttpPost("{groupId}/members")]
        public async Task<ActionResult<GroupMemberDto>> InviteMember(int groupId, InviteMemberDto dto)
        {
            var result = await groupService.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = groupId, UserEmail = dto.UserEmail },
                GetUserId()
            );
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => StatusCode(StatusCodes.Status201Created, result.Data)
            };
        }

        [HttpPut("{groupId}/invitations/{userId}/accept")]
        public async Task<ActionResult<GroupMemberDto>> AcceptInvitation(int groupId, string userId)
        {
            var result = await groupService.AcceptInviteAsync(
                new GroupMember { GroupId = groupId, UserId = userId },
                GetUserId()
            );
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpDelete("{groupId}")]
        public async Task<ActionResult> DeleteGroup(int groupId)
        {
            var result = await groupService.DeleteGroupAsync(groupId, GetUserId());
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                _ => NoContent()
            };
        }

        [HttpDelete("{groupId}/members/{userId}")]
        public async Task<ActionResult> DeleteMember(int groupId, string userId)
        {
            var result = await groupService.DeleteMemberAsync(userId, groupId, GetUserId());
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                _ => NoContent()
            };
        }

        [HttpDelete("{groupId}/invitations/{userId}")]
        public async Task<ActionResult> RefuseInvitation(int groupId, string userId)
        {
            var result = await groupService.RefuseInviteAsync(userId, groupId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => NoContent()
            };
        }

        [HttpPut("{groupId}/members/{userId}/salary")]
        public async Task<ActionResult> SetMemberSalary(int groupId, string userId, [FromBody] SetMemberSalaryDto dto)
        {
            var result = await groupService.SetMemberSalaryAsync(groupId, userId, dto.Salary, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => NoContent()
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
