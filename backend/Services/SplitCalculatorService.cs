using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class SplitCalculatorService(FinanceContext db) : ISplitCalculatorService
    {
        public async Task<ServiceResult<List<SplitMemberResultDto>>> CalculateAsync(int groupId, int month, int year, string contextUserId)
        {
            var isMember = await db.Groups.AnyAsync(g => g.Id == groupId && g.UserId == contextUserId)
                || await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == contextUserId && gm.IsActive);

            if (!isMember)
                return ServiceResult<List<SplitMemberResultDto>>.Fail(ServiceError.Unauthorized);

            var group = await db.Groups
                .Include(g => g.User)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return ServiceResult<List<SplitMemberResultDto>>.Fail(ServiceError.NotFound);

            var activeMembers = BuildActiveMemberList(group);

            var memberConfigs = await db.GroupMemberConfigs
                .Where(c => c.GroupId == groupId)
                .ToDictionaryAsync(c => c.UserId, c => c.Salary);

            var splitConfigs = await db.GroupSplitConfigs
                .Include(c => c.Shares)
                .Where(c => c.GroupId == groupId)
                .ToListAsync();

            var defaultConfig = splitConfigs.FirstOrDefault(c => c.IsDefault)
                ?? splitConfigs.FirstOrDefault();

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var expenses = await db.Expenses
                .Where(e => e.GroupId == groupId && e.Date >= startDate && e.Date <= endDate)
                .ToListAsync();

            var expenseIds = expenses.Select(e => e.Id).ToList();

            var expenseSplitConfigMap = await db.ExpenseSplitConfigs
                .Where(sc => expenseIds.Contains(sc.ExpenseId))
                .ToDictionaryAsync(sc => sc.ExpenseId, sc => sc.GroupSplitConfigId);

            var balances = activeMembers.ToDictionary(m => m.UserId, _ => 0m);

            foreach (var expense in expenses)
            {
                var configId = expenseSplitConfigMap.TryGetValue(expense.Id, out var cid) ? cid : (int?)null;
                var config = configId.HasValue
                    ? splitConfigs.FirstOrDefault(c => c.Id == configId.Value) ?? defaultConfig
                    : defaultConfig;

                if (config == null) continue;

                var shares = CalculateShares(config, activeMembers, memberConfigs, (decimal)expense.Value);

                foreach (var (userId, shareAmount) in shares)
                {
                    if (!balances.ContainsKey(userId)) continue;
                    balances[userId] -= shareAmount;
                }

                if (balances.ContainsKey(expense.UserId))
                    balances[expense.UserId] += (decimal)expense.Value;
            }

            var result = activeMembers.Select(m => new SplitMemberResultDto
            {
                UserId = m.UserId,
                Name = m.Name,
                Balance = Math.Round(balances[m.UserId], 2),
                Direction = balances[m.UserId] >= 0 ? SplitDirection.Receiver : SplitDirection.Payer
            }).ToList();

            return ServiceResult<List<SplitMemberResultDto>>.Ok(result);
        }

        private static List<(string UserId, string Name)> BuildActiveMemberList(Group group)
        {
            var members = new List<(string UserId, string Name)>
            {
                (group.UserId, group.User!.Name)
            };

            members.AddRange(group.Members
                .Where(m => m.IsActive)
                .Select(m => (m.UserId, m.User!.Name)));

            return members;
        }

        private static Dictionary<string, decimal> CalculateShares(
            GroupSplitConfig config,
            List<(string UserId, string Name)> activeMembers,
            Dictionary<string, decimal?> memberConfigs,
            decimal totalValue)
        {
            return config.SplitType switch
            {
                SplitType.Equal => CalculateEqualShares(activeMembers, totalValue),
                SplitType.Proportional => CalculateProportionalShares(activeMembers, memberConfigs, totalValue),
                SplitType.Manual => CalculateManualShares(config.Shares, totalValue),
                _ => []
            };
        }

        private static Dictionary<string, decimal> CalculateEqualShares(
            List<(string UserId, string Name)> members,
            decimal totalValue)
        {
            if (members.Count == 0) return [];
            var share = totalValue / members.Count;
            return members.ToDictionary(m => m.UserId, _ => share);
        }

        private static Dictionary<string, decimal> CalculateProportionalShares(
            List<(string UserId, string Name)> members,
            Dictionary<string, decimal?> memberConfigs,
            decimal totalValue)
        {
            var salaries = members
                .Select(m => (m.UserId, Salary: memberConfigs.TryGetValue(m.UserId, out var s) ? s ?? 0m : 0m))
                .ToList();

            var totalSalary = salaries.Sum(s => s.Salary);
            if (totalSalary == 0) return CalculateEqualShares(members, totalValue);

            return salaries.ToDictionary(s => s.UserId, s => totalValue * (s.Salary / totalSalary));
        }

        private static Dictionary<string, decimal> CalculateManualShares(
            ICollection<GroupSplitConfigShare> shares,
            decimal totalValue)
        {
            return shares.ToDictionary(s => s.UserId, s => totalValue * (s.Percentage / 100m));
        }
    }
}
