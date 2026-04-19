using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class GroupSplitConfigService(FinanceContext db) : IGroupSplitConfigService
    {
        public async Task<ServiceResult<List<GroupSplitConfigReturnDto>>> GetByGroupIdAsync(int groupId, string contextUserId)
        {
            var isMember = await IsMemberAsync(groupId, contextUserId);
            if (!isMember)
                return ServiceResult<List<GroupSplitConfigReturnDto>>.Fail(ServiceError.Unauthorized);

            var configs = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId)
                .ToListAsync();

            return ServiceResult<List<GroupSplitConfigReturnDto>>.Ok(configs.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<GroupSplitConfigReturnDto>> CreateAsync(int groupId, string contextUserId, CreateGroupSplitConfigDto dto)
        {
            var group = await db.Groups.FindAsync(groupId);
            if (group == null)
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.NotFound);

            if (group.UserId != contextUserId)
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.Unauthorized);

            var alreadyExists = await db.GroupSplitConfigs
                .AnyAsync(c => c.GroupId == groupId && c.SplitType == dto.SplitType);

            if (alreadyExists)
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.Conflict);

            if (dto.SplitType == SplitType.Manual && !SharesSumTo100(dto.Shares))
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.Conflict);

            if (dto.IsDefault)
                await ClearDefaultFlagAsync(groupId);

            var config = new GroupSplitConfig
            {
                GroupId = groupId,
                SplitType = dto.SplitType,
                IsDefault = dto.IsDefault
            };

            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();

            if (dto.SplitType == SplitType.Manual)
            {
                config.Shares = dto.Shares.Select(s => new GroupSplitConfigShare
                {
                    GroupSplitConfigId = config.Id,
                    UserId = s.UserId,
                    Percentage = s.Percentage
                }).ToList();
                await db.SaveChangesAsync();
            }

            return ServiceResult<GroupSplitConfigReturnDto>.Ok(ToDto(config));
        }

        public async Task<ServiceResult<GroupSplitConfigReturnDto>> UpdateAsync(int configId, string contextUserId, UpdateGroupSplitConfigDto dto)
        {
            var config = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == configId);

            if (config == null)
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.NotFound);

            if (config.Group!.UserId != contextUserId)
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.Unauthorized);

            if (config.SplitType == SplitType.Manual && !SharesSumTo100(dto.Shares))
                return ServiceResult<GroupSplitConfigReturnDto>.Fail(ServiceError.Conflict);

            if (dto.IsDefault && !config.IsDefault)
                await ClearDefaultFlagAsync(config.GroupId);

            config.IsDefault = dto.IsDefault;

            if (config.SplitType == SplitType.Manual)
            {
                db.GroupSplitConfigShares.RemoveRange(config.Shares);
                config.Shares = dto.Shares.Select(s => new GroupSplitConfigShare
                {
                    GroupSplitConfigId = config.Id,
                    UserId = s.UserId,
                    Percentage = s.Percentage
                }).ToList();
            }

            await db.SaveChangesAsync();
            return ServiceResult<GroupSplitConfigReturnDto>.Ok(ToDto(config));
        }

        public async Task<ServiceResult> DeleteAsync(int configId, string contextUserId)
        {
            var config = await db.GroupSplitConfigs
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == configId);

            if (config == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            if (config.Group!.UserId != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            var totalConfigs = await db.GroupSplitConfigs.CountAsync(c => c.GroupId == config.GroupId);
            if (totalConfigs <= 1)
                return ServiceResult.Fail(ServiceError.Conflict);

            db.GroupSplitConfigs.Remove(config);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task OnMemberJoinedAsync(int groupId, string newUserId)
        {
            var manualConfigs = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId && c.SplitType == SplitType.Manual)
                .ToListAsync();

            foreach (var config in manualConfigs)
            {
                var alreadyHasShare = config.Shares.Any(s => s.UserId == newUserId);
                if (!alreadyHasShare)
                {
                    db.GroupSplitConfigShares.Add(new GroupSplitConfigShare
                    {
                        GroupSplitConfigId = config.Id,
                        UserId = newUserId,
                        Percentage = 0
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task OnMemberLeftAsync(int groupId, string userId)
        {
            var group = await db.Groups.FindAsync(groupId);
            if (group == null) return;

            var manualConfigs = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId && c.SplitType == SplitType.Manual)
                .ToListAsync();

            foreach (var config in manualConfigs)
            {
                var leavingShare = config.Shares.FirstOrDefault(s => s.UserId == userId);
                if (leavingShare == null) continue;

                var ownerShare = config.Shares.FirstOrDefault(s => s.UserId == group.UserId);
                if (ownerShare != null)
                    ownerShare.Percentage += leavingShare.Percentage;

                db.GroupSplitConfigShares.Remove(leavingShare);
            }

            await db.SaveChangesAsync();
        }

        private async Task<bool> IsMemberAsync(int groupId, string userId)
        {
            return await db.Groups.AnyAsync(g => g.Id == groupId && g.UserId == userId)
                || await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);
        }

        private async Task ClearDefaultFlagAsync(int groupId)
        {
            var currentDefault = await db.GroupSplitConfigs
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.IsDefault);

            if (currentDefault != null)
                currentDefault.IsDefault = false;
        }

        private static bool SharesSumTo100(List<GroupSplitConfigShareDto> shares)
        {
            if (shares.Count == 0) return false;
            return shares.Sum(s => s.Percentage) == 100;
        }

        private static GroupSplitConfigReturnDto ToDto(GroupSplitConfig config) => new()
        {
            Id = config.Id,
            GroupId = config.GroupId,
            SplitType = config.SplitType,
            IsDefault = config.IsDefault,
            Shares = config.Shares.Select(s => new GroupSplitConfigShareDto
            {
                UserId = s.UserId,
                Percentage = s.Percentage
            }).ToList()
        };
    }
}
