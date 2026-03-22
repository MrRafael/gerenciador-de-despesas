using Clerk.BackendAPI.Models.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinBackend.Auth;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class GroupMemberController : ControllerBase
    {

        private readonly FinanceContext _dbContext;

        public GroupMemberController(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Users/5/GroupMember
        [HttpGet("/api/Users/{userId}/[controller]")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetGroupMembersByUserId(string userId)
        {
            string contextUserId = GetUserIdFromContext();
            if (userId != contextUserId)
            {
                return BadRequest();
            }

            var userFullData = await _dbContext.Users.Include(u => u.OwnedGroups)
                .Include(u => u.GroupMemberships)
                .ThenInclude(gm => gm.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);


            if (userFullData != null)
            {
                var groups = new List<GroupMemberDto>();
                foreach (var group in userFullData.OwnedGroups)
                {
                    groups.Add(new GroupMemberDto
                    {
                        Id = group.Id,
                        IsActive = true,
                        Name = group.Name,
                        UserId = userFullData.Id,
                        OwnerId = userFullData.Id
                    });
                }

                foreach (var memberGroup in userFullData.GroupMemberships)
                {
                    if (memberGroup.IsActive == true)
                    {
                        groups.Add(new GroupMemberDto
                        {
                            Id = memberGroup.Group.Id,
                            IsActive = memberGroup.IsActive,
                            Name = memberGroup.Group.Name,
                            UserId = userFullData.Id,
                            OwnerId = memberGroup.Group.UserId
                        });
                    }
                }

                return Ok(groups);
            }

            return NotFound();
        }

        // GET: api/Users/5/GroupMember/Invites
        [HttpGet("/api/Users/{userId}/[controller]/Invites")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetInvitesByUserId(string userId)
        {
            string contextUserId = GetUserIdFromContext();
            if (userId != contextUserId)
            {
                return BadRequest();
            }

            var invites = await _dbContext.GroupMembers
                .Include(x => x.Group)
                .ThenInclude(g => g.User)
                .Where(gm => gm.UserId == userId && gm.IsActive == false)
                .ToListAsync();


            if (invites != null)
            {
                var groups = new List<GroupMemberDto>();

                foreach (var memberGroup in invites)
                {
                    groups.Add(new GroupMemberDto
                    {
                        Id = memberGroup.Group.Id,
                        IsActive = memberGroup.IsActive,
                        Name = memberGroup.Group.Name,
                        UserId = memberGroup.UserId,
                        OwnerId = memberGroup.Group.UserId,
                        OwnerEmail = memberGroup.Group.User.Email,
                        OwnerName = memberGroup.Group.User.Name
                    });
                }

                return Ok(groups);
            }

            return NotFound();
        }

        // GET: api/Groups/5/GroupMember
        [HttpGet("/api/Groups/{groupId}/[controller]")]
        public async Task<ActionResult<List<GroupMemberDto>>> GetGroupMembersByGroupId(int groupId)
        {
            var groupFullData = await _dbContext.Groups
                .Include(u => u.User)
                .Include(u => u.Members)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(u => u.Id == groupId);

            if (groupFullData != null)
            {
                var members = new List<GroupMemberDto>();
                members.Add(new GroupMemberDto
                {
                    Id = groupId,
                    IsActive = true,
                    Name = groupFullData.Name,
                    UserId = groupFullData.UserId,
                    OwnerId = groupFullData.UserId,
                    MemberEmail = groupFullData.User.Email,
                    MemberName = groupFullData.User.Name
                });

                foreach (var memberGroup in groupFullData.Members)
                {
                    members.Add(new GroupMemberDto
                    {
                        Id = groupFullData.Id,
                        IsActive = memberGroup.IsActive,
                        Name = groupFullData.Name,
                        UserId = memberGroup.UserId,
                        OwnerId = groupFullData.UserId,
                        MemberEmail = memberGroup.User.Email,
                        MemberName = memberGroup.User.Name
                    });
                }

                return Ok(members);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<GroupMemberDto>> PostGroup(Model.Group group)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != group.UserId)
            {
                return BadRequest();
            }

            _dbContext.Groups.Add(group);
            await _dbContext.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new GroupMemberDto
            {
                Id = group.Id,
                IsActive = true,
                Name = group.Name,
                UserId = group.UserId,
                OwnerId = group.UserId,
            });

        }

        [HttpPut("Accept")]
        public async Task<ActionResult<GroupMemberDto>> AcceptInvite(GroupMember invite)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != invite.UserId)
            {
                return BadRequest();
            }

            var groupMemberToUpdate = await _dbContext.GroupMembers.Include(x => x.Group).FirstOrDefaultAsync(x => x.GroupId == invite.GroupId && x.UserId == invite.UserId);
            if (groupMemberToUpdate == null)
            {
                return BadRequest();
            }

            groupMemberToUpdate.IsActive = true;
            _dbContext.Entry(groupMemberToUpdate).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new GroupMemberDto
            {
                Id = groupMemberToUpdate.GroupId,
                IsActive = groupMemberToUpdate.IsActive,
                Name = groupMemberToUpdate.Group.Name,
                UserId = groupMemberToUpdate.UserId,
                OwnerId = groupMemberToUpdate.Group.UserId,
            });

        }

        [HttpDelete("Refuse")]
        public async Task<ActionResult<GroupMemberDto>> RefuseInvite(string UserId, int GroupId)
        {
            var contextUserId = GetUserIdFromContext();
            var invite = await _dbContext.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == GroupId && x.UserId == UserId);
            if (invite == null || contextUserId != invite.UserId)
            {
                return BadRequest();
            }
            
            _dbContext.GroupMembers.Remove(invite);
            await _dbContext.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("Member")]
        public async Task<ActionResult<GroupMemberDto>> DeleteMember(string UserId, int GroupId)
        {
            var contextUserId = GetUserIdFromContext();
            var groupMember = await _dbContext.GroupMembers.Include(x => x.Group).FirstOrDefaultAsync(x => x.GroupId == GroupId && x.UserId == UserId);
            if (groupMember == null || (contextUserId != groupMember.Group.UserId && groupMember.UserId != contextUserId))
            {
                return BadRequest();
            }

            _dbContext.GroupMembers.Remove(groupMember);
            await _dbContext.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("Group")]
        public async Task<ActionResult<GroupMemberDto>> DeleteGroup(int GroupId)
        {
            var contextUserId = GetUserIdFromContext();
            var group = await _dbContext.Groups.FirstOrDefaultAsync(x => x.Id == GroupId);
            if (group == null || group.UserId != contextUserId)
            {
                return BadRequest();
            }

            _dbContext.Groups.Remove(group);
            await _dbContext.SaveChangesAsync();

            return NoContent();

        }

        [HttpPost("NewMember")]
        public async Task<ActionResult<GroupMemberDto>> PostMemberGroup(MemberGrouToAddDto memberGroup)
        {
            var contextUserId = GetUserIdFromContext();
            var userToAdd = await _dbContext.Users.Where(x => x.Email == memberGroup.UserEmail).FirstOrDefaultAsync();
            if (userToAdd == null)
            {
                return BadRequest();
            }

            var group = await _dbContext.Groups.FindAsync(memberGroup.GroupId);
            if (contextUserId != group.UserId || userToAdd.Id == contextUserId)
            {
                return BadRequest();
            }


            var groupMemberExists = await _dbContext.GroupMembers
                .Where(x => x.GroupId == memberGroup.GroupId && x.UserId == userToAdd.Id)
                .FirstOrDefaultAsync();
            if (groupMemberExists != null)
            {
                return Conflict();
            }

            var newGroup = new GroupMember
            {
                GroupId = memberGroup.GroupId,
                UserId = userToAdd.Id,
                IsActive = false
            };

            _dbContext.GroupMembers.Add(newGroup);

            await _dbContext.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new GroupMemberDto
            {
                Id = group.Id,
                IsActive = newGroup.IsActive,
                Name = group.Name,
                UserId = newGroup.UserId,
                OwnerId = contextUserId,
            });

        }

        private string GetUserIdFromContext()
        {
            var mainClaims = User;
            var userId = mainClaims.FindFirstValue("sub");

            return userId;
        }
    }
}
