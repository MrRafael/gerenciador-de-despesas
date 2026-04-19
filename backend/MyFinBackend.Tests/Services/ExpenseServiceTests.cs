using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class ExpenseServiceTests
    {
        private static ExpenseService MakeService(FinanceContext db) =>
            new(db, new MonthCloseService(db, new SplitCalculatorService(db)));

        private static Expense MakeExpense(string userId, DateOnly? date = null) => new()
        {
            Description = "Teste",
            Value = 100f,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            UserId = userId,
            CategoryId = 1
        };

        [Fact]
        public async Task GetByUserId_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-b");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetByUserId_ReturnsEmptyList_WhenNoExpenses()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetByUserId_ReturnsExpenses_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(MakeExpense("user-a"));
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task GetByDateRange_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.GetByDateRangeAsync("user-a", "user-b",
                DateOnly.Parse("2024-01-01"), DateOnly.Parse("2024-01-31"));

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetByDateRange_ReturnsOnlyExpensesInRange()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(MakeExpense("user-a", DateOnly.Parse("2024-01-15")));
            db.Expenses.Add(MakeExpense("user-a", DateOnly.Parse("2024-02-15")));
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.GetByDateRangeAsync("user-a", "user-a",
                DateOnly.Parse("2024-01-01"), DateOnly.Parse("2024-01-31"));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.CreateAsync(MakeExpense("user-a"), "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Create_ReturnsDto_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.CreateAsync(MakeExpense("user-a"), "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal("Teste", result.Data!.Description);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenExpenseNotFound()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.DeleteAsync(999, "user-a");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenNotOwner()
        {
            using var db = DbContextFactory.Create();
            var expense = MakeExpense("user-a");
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteAsync(expense.Id, "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Delete_Succeeds_WhenOwner()
        {
            using var db = DbContextFactory.Create();
            var expense = MakeExpense("user-a");
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteAsync(expense.Id, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Empty(db.Expenses);
        }

        [Fact]
        public async Task CreateBulk_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);
            var bulk = new BulkExpenseToSaveDto { Expenses = [MakeExpense("user-a")] };

            var result = await service.CreateBulkAsync(bulk, "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task CreateBulk_PersistsAll_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);
            var bulk = new BulkExpenseToSaveDto
            {
                Expenses = [MakeExpense("user-a"), MakeExpense("user-a")]
            };

            var result = await service.CreateBulkAsync(bulk, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, db.Expenses.Count());
        }

        [Fact]
        public async Task UpdateGroup_ReturnsNotFound_WhenExpenseDoesNotExist()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.UpdateGroupAsync(999, new UpdateExpenseGroupDto { GroupId = 1 }, "user-a");

            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task UpdateGroup_ReturnsNotFound_WhenNotOwner()
        {
            using var db = DbContextFactory.Create();
            var expense = MakeExpense("user-a");
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.UpdateGroupAsync(expense.Id, new UpdateExpenseGroupDto { GroupId = 1 }, "user-b");

            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task UpdateGroup_AssignsGroupAndCreatesSplitConfig_WhenOwner()
        {
            using var db = DbContextFactory.Create();
            var group = new Group { Name = "Familia", UserId = "user-a" };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var splitConfig = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(splitConfig);
            var expense = MakeExpense("user-a");
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.UpdateGroupAsync(expense.Id,
                new UpdateExpenseGroupDto { GroupId = group.Id, GroupSplitConfigId = splitConfig.Id }, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal(group.Id, result.Data!.GroupId);
            Assert.Equal("Familia", result.Data.GroupName);
            Assert.Single(db.ExpenseSplitConfigs);
        }

        [Fact]
        public async Task UpdateGroup_RemovesGroupAndSplitConfig_WhenGroupIdIsNull()
        {
            using var db = DbContextFactory.Create();
            var group = new Group { Name = "Familia", UserId = "user-a" };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var splitConfig = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(splitConfig);
            var expense = MakeExpense("user-a");
            expense.GroupId = group.Id;
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { Expense = expense, GroupSplitConfigId = splitConfig.Id });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.UpdateGroupAsync(expense.Id,
                new UpdateExpenseGroupDto { GroupId = null }, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Null(result.Data!.GroupId);
            Assert.Empty(db.ExpenseSplitConfigs);
        }

        [Fact]
        public async Task UpdateGroup_UpdatesSplitConfig_WhenGroupAlreadyAssigned()
        {
            using var db = DbContextFactory.Create();
            var group = new Group { Name = "Familia", UserId = "user-a" };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var splitConfigA = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            var splitConfigB = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = false };
            db.GroupSplitConfigs.AddRange(splitConfigA, splitConfigB);
            var expense = MakeExpense("user-a");
            expense.GroupId = group.Id;
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { Expense = expense, GroupSplitConfigId = splitConfigA.Id });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.UpdateGroupAsync(expense.Id,
                new UpdateExpenseGroupDto { GroupId = group.Id, GroupSplitConfigId = splitConfigB.Id }, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal(splitConfigB.Id, db.ExpenseSplitConfigs.Single().GroupSplitConfigId);
        }
    }
}
