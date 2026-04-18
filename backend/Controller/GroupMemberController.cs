using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class GroupMemberController(IGroupService groupService) : ControllerBase
    {
        [HttpGet("/api/Users/{userId}/[controller]")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetGroupMembersByUserId(string userId)
        {
            var result = await groupService.GetGroupsByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => BadRequest(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("/api/Users/{userId}/[controller]/Invites")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetInvitesByUserId(string userId)
        {
            var result = await groupService.GetInvitesByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => BadRequest(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("/api/Groups/{groupId}/[controller]")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetGroupMembersByGroupId(int groupId)
        {
            var result = await groupService.GetMembersByGroupIdAsync(groupId);
            return result.IsSuccess ? Ok(result.Data) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<GroupMemberDto>> PostGroup(Group group)
        {
            var result = await groupService.CreateGroupAsync(group, GetUserId());
            if (!result.IsSuccess) return BadRequest();
            return StatusCode(StatusCodes.Status201Created, result.Data);
        }

        [HttpPut("Accept")]
        public async Task<ActionResult<GroupMemberDto>> AcceptInvite(GroupMember invite)
        {
            var result = await groupService.AcceptInviteAsync(invite, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => BadRequest(),
                ServiceError.NotFound => BadRequest(),
                _ => StatusCode(StatusCodes.Status201Created, result.Data)
            };
        }

        [HttpDelete("Refuse")]
        public async Task<ActionResult> RefuseInvite(string UserId, int GroupId)
        {
            var result = await groupService.RefuseInviteAsync(UserId, GroupId, GetUserId());
            return result.IsSuccess ? NoContent() : BadRequest();
        }

        [HttpDelete("Member")]
        public async Task<ActionResult> DeleteMember(string UserId, int GroupId)
        {
            var result = await groupService.DeleteMemberAsync(UserId, GroupId, GetUserId());
            return result.Error switch
            {
                ServiceError.NotFound => BadRequest(),
                ServiceError.Unauthorized => BadRequest(),
                _ => NoContent()
            };
        }

        [HttpDelete("Group")]
        public async Task<ActionResult> DeleteGroup(int GroupId)
        {
            var result = await groupService.DeleteGroupAsync(GroupId, GetUserId());
            return result.IsSuccess ? NoContent() : BadRequest();
        }

        [HttpPost("NewMember")]
        public async Task<ActionResult<GroupMemberDto>> PostMemberGroup(MemberGrouToAddDto memberGroup)
        {
            var result = await groupService.InviteMemberAsync(memberGroup, GetUserId());
            return result.Error switch
            {
                ServiceError.NotFound => BadRequest(),
                ServiceError.Unauthorized => BadRequest(),
                ServiceError.Conflict => Conflict(),
                _ => StatusCode(StatusCodes.Status201Created, result.Data)
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
