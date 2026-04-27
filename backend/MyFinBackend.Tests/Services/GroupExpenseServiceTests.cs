using MyFinBackend.Database;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class GroupExpenseServiceTests
    {
        private static GroupService MakeService(FinanceContext db) =>
            new(db, new GroupSplitConfigService(db));

        private static User MakeUser(string id, string email) => new()
        {
            Id = id,
            Name = $"User {id}",
            Email = email
        };

        private static Group MakeGroup(string ownerId, string name = "Grupo A") => new()
        {
            Name = name,
            UserId = ownerId
        };

        private static Expense MakeExpense(string userId, int groupId, DateOnly date) => new()
        {
            Description = "Despesa teste",
            Value = 100f,
            Date = date,
            UserId = userId,
            Category = new ExpenseCategory { Name = "Categoria", UserId = userId },
            GroupId = groupId
        };

        [Fact]
        public async Task GetGroupExpenses_ReturnsUnauthorized_WhenNotMember()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "intruder",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetGroupExpenses_Succeeds_WhenOwner()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.Expenses.Add(MakeExpense("owner", group.Id, new DateOnly(2026, 1, 15)));
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "owner",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
            Assert.Equal("owner", result.Data![0].UserId);
        }

        [Fact]
        public async Task GetGroupExpenses_Succeeds_WhenActiveMember()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            db.Expenses.Add(MakeExpense("owner", group.Id, new DateOnly(2026, 1, 10)));
            db.Expenses.Add(MakeExpense("member", group.Id, new DateOnly(2026, 1, 20)));
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "member",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
        }

        [Fact]
        public async Task GetGroupExpenses_ReturnsUnauthorized_WhenInvitePending()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var invited = MakeUser("invited", "invited@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, invited);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "invited", IsActive = false });
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "invited",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetGroupExpenses_FiltersByDateRange()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.Expenses.Add(MakeExpense("owner", group.Id, new DateOnly(2026, 1, 15)));
            db.Expenses.Add(MakeExpense("owner", group.Id, new DateOnly(2026, 2, 15)));
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "owner",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task GetGroupExpenses_IncludesSplitConfigId_WhenConfigExists()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            var splitConfig = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = true };
            db.GroupSplitConfigs.Add(splitConfig);
            var expense = MakeExpense("owner", group.Id, new DateOnly(2026, 1, 15));
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { ExpenseId = expense.Id, GroupSplitConfigId = splitConfig.Id });
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "owner",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.True(result.IsSuccess);
            Assert.Equal(splitConfig.Id, result.Data![0].GroupSplitConfigId);
        }

        [Fact]
        public async Task GetGroupExpenses_SplitConfigIdIsNull_WhenNoConfigExists()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.Expenses.Add(MakeExpense("owner", group.Id, new DateOnly(2026, 1, 15)));
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var result = await service.GetGroupExpensesAsync(group.Id, "owner",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            Assert.True(result.IsSuccess);
            Assert.Null(result.Data![0].GroupSplitConfigId);
        }
    }
}