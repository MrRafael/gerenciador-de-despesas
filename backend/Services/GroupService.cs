using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class GroupService(FinanceContext db, IGroupSplitConfigService splitConfig) : IGroupService
    {
        public async Task<ServiceResult<List<GroupMemberDto>>> GetGroupsByUserIdAsync(string userId, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult<List<GroupMemberDto>>.Fail(ServiceError.Unauthorized);

            var user = await db.Users
                .Include(u => u.OwnedGroups)
                .Include(u => u.GroupMemberships).ThenInclude(gm => gm.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<List<GroupMemberDto>>.Fail(ServiceError.NotFound);

            var groups = user.OwnedGroups.Select(g => new GroupMemberDto
            {
                Id = g.Id,
                IsActive = true,
                Name = g.Name,
                UserId = userId,
                OwnerId = userId
            }).ToList();

            groups.AddRange(user.GroupMemberships
                .Where(gm => gm.IsActive)
                .Select(gm => new GroupMemberDto
                {
                    Id = gm.Group.Id,
                    IsActive = gm.IsActive,
                    Name = gm.Group.Name,
                    UserId = userId,
                    OwnerId = gm.Group.UserId
                }));

            return ServiceResult<List<GroupMemberDto>>.Ok(groups);
        }

        public async Task<ServiceResult<List<GroupMemberDto>>> GetInvitesByUserIdAsync(string userId, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult<List<GroupMemberDto>>.Fail(ServiceError.Unauthorized);

            var invites = await db.GroupMembers
                .Include(x => x.Group).ThenInclude(g => g.User)
                .Where(gm => gm.UserId == userId && !gm.IsActive)
                .ToListAsync();

            var result = invites.Select(gm => new GroupMemberDto
            {
                Id = gm.Group.Id,
                IsActive = gm.IsActive,
                Name = gm.Group.Name,
                UserId = gm.UserId,
                OwnerId = gm.Group.UserId,
                OwnerEmail = gm.Group.User.Email,
                OwnerName = gm.Group.User.Name
            }).ToList();

            return ServiceResult<List<GroupMemberDto>>.Ok(result);
        }

        public async Task<ServiceResult<List<GroupMemberDto>>> GetMembersByGroupIdAsync(int groupId)
        {
            var group = await db.Groups
                .Include(g => g.User)
                .Include(g => g.Members).ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return ServiceResult<List<GroupMemberDto>>.Fail(ServiceError.NotFound);

            var allUserIds = new[] { group.UserId }.Concat(group.Members.Select(m => m.UserId)).ToList();
            var salaries = await db.GroupMemberConfigs
                .Where(c => c.GroupId == groupId && allUserIds.Contains(c.UserId))
                .ToDictionaryAsync(c => c.UserId, c => c.Salary);

            var members = new List<GroupMemberDto>
            {
                new() {
                    Id = groupId,
                    IsActive = true,
                    Name = group.Name,
                    UserId = group.UserId,
                    OwnerId = group.UserId,
                    MemberEmail = group.User.Email,
                    MemberName = group.User.Name,
                    Salary = salaries.TryGetValue(group.UserId, out var os) ? os : null
                }
            };

            members.AddRange(group.Members.Select(gm => new GroupMemberDto
            {
                Id = group.Id,
                IsActive = gm.IsActive,
                Name = group.Name,
                UserId = gm.UserId,
                OwnerId = group.UserId,
                MemberEmail = gm.User.Email,
                MemberName = gm.User.Name,
                Salary = salaries.TryGetValue(gm.UserId, out var ms) ? ms : null
            }));

            return ServiceResult<List<GroupMemberDto>>.Ok(members);
        }

        public async Task<ServiceResult> SetMemberSalaryAsync(int groupId, string userId, decimal? salary, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            var isMember = await db.Groups.AnyAsync(g => g.Id == groupId && g.UserId == userId)
                || await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.IsActive);

            if (!isMember)
                return ServiceResult.Fail(ServiceError.NotFound);

            var config = await db.GroupMemberConfigs.FindAsync(groupId, userId);
            if (config == null)
            {
                db.GroupMemberConfigs.Add(new Model.GroupMemberConfig { GroupId = groupId, UserId = userId, Salary = salary });
            }
            else
            {
                config.Salary = salary;
                db.Entry(config).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<GroupMemberDto>> CreateGroupAsync(Group group, string contextUserId)
        {
            if (group.UserId != contextUserId)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.Unauthorized);

            db.Groups.Add(group);
            await db.SaveChangesAsync();

            return ServiceResult<GroupMemberDto>.Ok(new GroupMemberDto
            {
                Id = group.Id,
                IsActive = true,
                Name = group.Name,
                UserId = group.UserId,
                OwnerId = group.UserId
            });
        }

        public async Task<ServiceResult<GroupMemberDto>> AcceptInviteAsync(GroupMember invite, string contextUserId)
        {
            if (invite.UserId != contextUserId)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.Unauthorized);

            var member = await db.GroupMembers
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.GroupId == invite.GroupId && x.UserId == invite.UserId);

            if (member == null)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.NotFound);

            member.IsActive = true;
            db.Entry(member).State = EntityState.Modified;
            await db.SaveChangesAsync();

            await splitConfig.OnMemberJoinedAsync(invite.GroupId, invite.UserId);

            return ServiceResult<GroupMemberDto>.Ok(new GroupMemberDto
            {
                Id = member.GroupId,
                IsActive = member.IsActive,
                Name = member.Group!.Name,
                UserId = member.UserId,
                OwnerId = member.Group.UserId
            });
        }

        public async Task<ServiceResult> RefuseInviteAsync(string userId, int groupId, string contextUserId)
        {
            var invite = await db.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
            if (invite == null || invite.UserId != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            db.GroupMembers.Remove(invite);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteMemberAsync(string userId, int groupId, string contextUserId)
        {
            var member = await db.GroupMembers
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

            if (member == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            var isOwner = member.Group!.UserId == contextUserId;
            var isSelf = member.UserId == contextUserId;
            if (!isOwner && !isSelf)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            await splitConfig.OnMemberLeftAsync(member.GroupId, member.UserId);

            db.GroupMembers.Remove(member);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteGroupAsync(int groupId, string contextUserId)
        {
            var group = await db.Groups.FirstOrDefaultAsync(x => x.Id == groupId);
            if (group == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            if (group.UserId != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            group.IsDeleted = true;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<List<GroupExpenseDto>>> GetGroupExpensesAsync(int groupId, string contextUserId, DateOnly startDate, DateOnly endDate)
        {
            var isMember = await db.Groups.AnyAsync(g => g.Id == groupId && g.UserId == contextUserId)
                || await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == contextUserId && gm.IsActive);

            if (!isMember)
                return ServiceResult<List<GroupExpenseDto>>.Fail(ServiceError.Unauthorized);

            var expenses = await db.Expenses
                .Include(e => e.Category)
                .Where(e => e.GroupId == groupId && e.Date >= startDate && e.Date <= endDate)
                .ToListAsync();

            var expenseIds = expenses.Select(e => e.Id).ToList();

            var splitConfigs = await db.ExpenseSplitConfigs
                .Where(sc => expenseIds.Contains(sc.ExpenseId))
                .ToDictionaryAsync(sc => sc.ExpenseId, sc => sc.GroupSplitConfigId);

            var result = expenses.Select(e => new GroupExpenseDto
            {
                Id = e.Id,
                Description = e.Description,
                Value = e.Value,
                Date = e.Date,
                CategoryId = e.CategoryId,
                CategoryName = e.Category?.Name,
                UserId = e.UserId,
                GroupSplitConfigId = splitConfigs.TryGetValue(e.Id, out var scId) ? scId : null
            }).ToList();

            return ServiceResult<List<GroupExpenseDto>>.Ok(result);
        }

        public async Task<ServiceResult<GroupMemberDto>> InviteMemberAsync(MemberGrouToAddDto memberGroup, string contextUserId)
        {
            var userToAdd = await db.Users.FirstOrDefaultAsync(x => x.Email == memberGroup.UserEmail);
            if (userToAdd == null)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.NotFound);

            var group = await db.Groups.FindAsync(memberGroup.GroupId);
            if (group == null)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.NotFound);

            if (group.UserId != contextUserId || userToAdd.Id == contextUserId)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.Unauthorized);

            var alreadyMember = await db.GroupMembers
                .AnyAsync(x => x.GroupId == memberGroup.GroupId && x.UserId == userToAdd.Id);

            if (alreadyMember)
                return ServiceResult<GroupMemberDto>.Fail(ServiceError.Conflict);

            var newMember = new GroupMember
            {
                GroupId = memberGroup.GroupId,
                UserId = userToAdd.Id,
                IsActive = false
            };

            db.GroupMembers.Add(newMember);
            await db.SaveChangesAsync();

            return ServiceResult<GroupMemberDto>.Ok(new GroupMemberDto
            {
                Id = group.Id,
                IsActive = newMember.IsActive,
                Name = group.Name,
                UserId = newMember.UserId,
                OwnerId = contextUserId
            });
        }
    }
}
