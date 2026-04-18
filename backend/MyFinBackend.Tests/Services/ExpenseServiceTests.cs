using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class ExpenseServiceTests
    {
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
            var service = new ExpenseService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-b");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetByUserId_ReturnsNotFound_WhenNoExpenses()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task GetByUserId_ReturnsExpenses_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(MakeExpense("user-a"));
            await db.SaveChangesAsync();
            var service = new ExpenseService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task GetByDateRange_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);

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
            var service = new ExpenseService(db);

            var result = await service.GetByDateRangeAsync("user-a", "user-a",
                DateOnly.Parse("2024-01-01"), DateOnly.Parse("2024-01-31"));

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);

            var result = await service.CreateAsync(MakeExpense("user-a"), "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Create_ReturnsDto_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);

            var result = await service.CreateAsync(MakeExpense("user-a"), "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal("Teste", result.Data!.Description);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenExpenseNotFound()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);

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
            var service = new ExpenseService(db);

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
            var service = new ExpenseService(db);

            var result = await service.DeleteAsync(expense.Id, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Empty(db.Expenses);
        }

        [Fact]
        public async Task CreateBulk_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);
            var bulk = new BulkExpenseToSaveDto { Expenses = [MakeExpense("user-a")] };

            var result = await service.CreateBulkAsync(bulk, "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task CreateBulk_PersistsAll_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseService(db);
            var bulk = new BulkExpenseToSaveDto
            {
                Expenses = [MakeExpense("user-a"), MakeExpense("user-a")]
            };

            var result = await service.CreateBulkAsync(bulk, "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, db.Expenses.Count());
        }
    }
}
