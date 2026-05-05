using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class ExpenseCategoryServiceTests
    {
        private static ExpenseCategory MakeCategory(string userId, string name = "Alimentação") => new()
        {
            Name = name,
            UserId = userId
        };

        [Fact]
        public async Task GetByUserId_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseCategoryService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetByUserId_ReturnsEmptyList_WhenNoCategories()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseCategoryService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetByUserId_ReturnsCategories_WhenExist()
        {
            using var db = DbContextFactory.Create();
            db.ExpenseCategories.Add(MakeCategory("user-a", "Transporte"));
            db.ExpenseCategories.Add(MakeCategory("user-a", "Alimentação"));
            await db.SaveChangesAsync();
            var service = new ExpenseCategoryService(db);

            var result = await service.GetByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Alimentação", result.Data[0].Name);
            Assert.Equal("Transporte", result.Data[1].Name);
        }

        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseCategoryService(db);

            var result = await service.CreateAsync(MakeCategory("user-a"), "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenCategoryAlreadyExists()
        {
            using var db = DbContextFactory.Create();
            db.ExpenseCategories.Add(MakeCategory("user-a"));
            await db.SaveChangesAsync();
            var service = new ExpenseCategoryService(db);

            var result = await service.CreateAsync(MakeCategory("user-a"), "user-a");

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Create_Succeeds_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = new ExpenseCategoryService(db);

            var result = await service.CreateAsync(MakeCategory("user-a"), "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal("Alimentação", result.Data!.Name);
            Assert.Single(db.ExpenseCategories);
        }

        [Fact]
        public async Task Create_AllowsSameCategoryName_ForDifferentUsers()
        {
            using var db = DbContextFactory.Create();
            db.ExpenseCategories.Add(MakeCategory("user-a"));
            await db.SaveChangesAsync();
            var service = new ExpenseCategoryService(db);

            var result = await service.CreateAsync(MakeCategory("user-b"), "user-b");

            Assert.True(result.IsSuccess);
        }
    }
}
