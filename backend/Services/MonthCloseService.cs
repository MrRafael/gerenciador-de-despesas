using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class MonthCloseService(FinanceContext db, ISplitCalculatorService calculator) : IMonthCloseService
    {
        public async Task<ServiceResult<List<PendingMonthDto>>> GetPendingMonthsAsync(int groupId, string contextUserId)
        {
            if (!await IsMemberAsync(groupId, contextUserId))
                return ServiceResult<List<PendingMonthDto>>.Fail(ServiceError.Unauthorized);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentMonthStart = new DateOnly(today.Year, today.Month, 1);

            var closed = await db.GroupMonthCloses
                .Where(c => c.GroupId == groupId && c.ClosedAt != null)
                .Select(c => new { c.Month, c.Year })
                .ToListAsync();

            var firstExpense = await db.Expenses
                .Where(e => e.GroupId == groupId)
                .OrderBy(e => e.Date)
                .FirstOrDefaultAsync();

            if (firstExpense == null)
                return ServiceResult<List<PendingMonthDto>>.Ok([]);

            var start = new DateOnly(firstExpense.Date.Year, firstExpense.Date.Month, 1);
            var pending = new List<PendingMonthDto>();

            for (var month = start; month < currentMonthStart; month = month.AddMonths(1))
            {
                var alreadyClosed = closed.Any(c => c.Month == month.Month && c.Year == month.Year);
                if (!alreadyClosed)
                    pending.Add(new PendingMonthDto { Month = month.Month, Year = month.Year });
            }

            return ServiceResult<List<PendingMonthDto>>.Ok(pending);
        }

        public async Task<ServiceResult<MonthCloseStatusDto>> GetStatusAsync(int groupId, int month, int year, string contextUserId)
        {
            if (!await IsMemberAsync(groupId, contextUserId))
                return ServiceResult<MonthCloseStatusDto>.Fail(ServiceError.Unauthorized);

            var allMembers = await GetAllMembersAsync(groupId);

            var monthClose = await db.GroupMonthCloses
                .Include(c => c.Confirmations)
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Month == month && c.Year == year);

            var confirmations = allMembers.Select(m => new MonthCloseConfirmationDto
            {
                UserId = m.UserId,
                Name = m.Name,
                Confirmed = monthClose?.Confirmations.Any(conf => conf.UserId == m.UserId) ?? false
            }).ToList();

            return ServiceResult<MonthCloseStatusDto>.Ok(new MonthCloseStatusDto
            {
                Month = month,
                Year = year,
                IsClosed = monthClose?.ClosedAt != null,
                Confirmations = confirmations
            });
        }

        public async Task<ServiceResult<bool>> ConfirmAsync(int groupId, string userId, int month, int year)
        {
            if (!await IsMemberAsync(groupId, userId))
                return ServiceResult<bool>.Fail(ServiceError.Unauthorized);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var targetMonth = new DateOnly(year, month, 1);
            if (targetMonth >= new DateOnly(today.Year, today.Month, 1))
                return ServiceResult<bool>.Fail(ServiceError.Conflict);

            var monthClose = await db.GroupMonthCloses
                .Include(c => c.Confirmations)
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Month == month && c.Year == year);

            if (monthClose == null)
            {
                monthClose = new GroupMonthClose { GroupId = groupId, Month = month, Year = year };
                db.GroupMonthCloses.Add(monthClose);
                await db.SaveChangesAsync();
            }

            if (monthClose.ClosedAt != null)
                return ServiceResult<bool>.Fail(ServiceError.Conflict);

            var alreadyConfirmed = monthClose.Confirmations.Any(c => c.UserId == userId);
            if (!alreadyConfirmed)
            {
                db.GroupMonthCloseConfirmations.Add(new GroupMonthCloseConfirmation
                {
                    GroupMonthCloseId = monthClose.Id,
                    UserId = userId,
                    ConfirmedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();

                await db.Entry(monthClose).Collection(c => c.Confirmations).LoadAsync();
            }

            var allMembers = await GetAllMembersAsync(groupId);
            var allConfirmed = allMembers.All(m => monthClose.Confirmations.Any(c => c.UserId == m.UserId));

            if (allConfirmed)
                await TryCloseAsync(monthClose, groupId, month, year);

            return ServiceResult<bool>.Ok(allConfirmed);
        }

        public async Task<ServiceResult> UnconfirmAsync(int groupId, string userId, int month, int year)
        {
            if (!await IsMemberAsync(groupId, userId))
                return ServiceResult.Fail(ServiceError.Unauthorized);

            var monthClose = await db.GroupMonthCloses
                .Include(c => c.Confirmations)
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Month == month && c.Year == year);

            if (monthClose == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            if (monthClose.ClosedAt != null)
                return ServiceResult.Fail(ServiceError.Conflict);

            var confirmation = monthClose.Confirmations.FirstOrDefault(c => c.UserId == userId);
            if (confirmation == null)
                return ServiceResult.Fail(ServiceError.NotFound);

            db.GroupMonthCloseConfirmations.Remove(confirmation);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<bool> IsClosedMonthAsync(int groupId, int month, int year)
        {
            return await db.GroupMonthCloses
                .AnyAsync(c => c.GroupId == groupId && c.Month == month && c.Year == year && c.ClosedAt != null);
        }

        private async Task TryCloseAsync(GroupMonthClose monthClose, int groupId, int month, int year)
        {
            var summaryResult = await calculator.CalculateAsync(groupId, month, year, monthClose.Confirmations.First().UserId);
            if (!summaryResult.IsSuccess) return;

            var expenses = await db.Expenses
                .Where(e => e.GroupId == groupId && e.Date.Month == month && e.Date.Year == year)
                .ToListAsync();

            var splitConfigMap = await db.ExpenseSplitConfigs
                .Where(sc => expenses.Select(e => e.Id).Contains(sc.ExpenseId))
                .ToDictionaryAsync(sc => sc.ExpenseId, sc => sc.GroupSplitConfigId);

            var defaultConfig = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId)
                .OrderByDescending(c => c.IsDefault)
                .FirstOrDefaultAsync();

            var allConfigs = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId)
                .ToListAsync();

            var memberConfigs = await db.GroupMemberConfigs
                .Where(c => c.GroupId == groupId)
                .ToDictionaryAsync(c => c.UserId, c => c.Salary);

            var group = await db.Groups
                .Include(g => g.User)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstAsync(g => g.Id == groupId);

            var activeMembers = new List<(string UserId, string Name)> { (group.UserId, group.User!.Name) };
            activeMembers.AddRange(group.Members.Where(m => m.IsActive).Select(m => (m.UserId, m.User!.Name)));

            foreach (var expense in expenses)
            {
                var configId = splitConfigMap.TryGetValue(expense.Id, out var cid) ? cid : (int?)null;
                var config = configId.HasValue
                    ? allConfigs.FirstOrDefault(c => c.Id == configId.Value) ?? defaultConfig
                    : defaultConfig;

                if (config == null) continue;

                var shares = CalculateSharesForExpense(config, activeMembers, memberConfigs, (decimal)expense.Value);

                foreach (var (userId, shareAmount) in shares)
                {
                    db.ExpenseSplitShares.Add(new ExpenseSplitShare
                    {
                        ExpenseId = expense.Id,
                        UserId = userId,
                        Percentage = config.SplitType == SplitType.Manual
                            ? config.Shares.FirstOrDefault(s => s.UserId == userId)?.Percentage ?? 0
                            : activeMembers.Count > 0 ? 100m / activeMembers.Count : 0,
                        Amount = Math.Round(shareAmount, 2)
                    });
                }
            }

            monthClose.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        private static Dictionary<string, decimal> CalculateSharesForExpense(
            GroupSplitConfig config,
            List<(string UserId, string Name)> activeMembers,
            Dictionary<string, decimal?> memberConfigs,
            decimal totalValue)
        {
            return config.SplitType switch
            {
                SplitType.Equal => activeMembers.Count > 0
                    ? activeMembers.ToDictionary(m => m.UserId, _ => totalValue / activeMembers.Count)
                    : [],
                SplitType.Proportional => CalculateProportional(activeMembers, memberConfigs, totalValue),
                SplitType.Manual => config.Shares.ToDictionary(s => s.UserId, s => totalValue * (s.Percentage / 100m)),
                _ => []
            };
        }

        private static Dictionary<string, decimal> CalculateProportional(
            List<(string UserId, string Name)> members,
            Dictionary<string, decimal?> configs,
            decimal totalValue)
        {
            var salaries = members
                .Select(m => (m.UserId, Salary: configs.TryGetValue(m.UserId, out var s) ? s ?? 0m : 0m))
                .ToList();

            var total = salaries.Sum(s => s.Salary);
            if (total == 0)
                return members.Count > 0
                    ? members.ToDictionary(m => m.UserId, _ => totalValue / members.Count)
                    : [];

            return salaries.ToDictionary(s => s.UserId, s => totalValue * (s.Salary / total));
        }

        private async Task<bool> IsMemberAsync(int groupId, string userId)
        {
            return await db.Groups.AnyAsync(g => g.Id == groupId && g.UserId == userId)
                || await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);
        }

        private async Task<List<(string UserId, string Name)>> GetAllMembersAsync(int groupId)
        {
            var group = await db.Groups
                .Include(g => g.User)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return [];

            var members = new List<(string UserId, string Name)> { (group.UserId, group.User!.Name) };
            members.AddRange(group.Members.Where(m => m.IsActive).Select(m => (m.UserId, m.User!.Name)));
            return members;
        }
    }
}
