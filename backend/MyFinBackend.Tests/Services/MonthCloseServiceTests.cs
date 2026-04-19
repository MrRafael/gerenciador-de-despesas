using MyFinBackend.Database;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class MonthCloseServiceTests
    {
        private static User MakeUser(string id) => new() { Id = id, Name = $"User {id}", Email = $"{id}@test.com" };

        private static async Task<(FinanceContext db, Group group)> SetupGroupWithTwoMembersAsync()
        {
            var db = DbContextFactory.Create();
            db.Users.AddRange(MakeUser("owner"), MakeUser("member"));
            var group = new Group { Name = "Grupo", UserId = "owner" };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            await db.SaveChangesAsync();
            return (db, group);
        }

        private static MonthCloseService MakeService(FinanceContext db)
        {
            var calculator = new SplitCalculatorService(db);
            return new MonthCloseService(db, calculator);
        }

        [Fact]
        public async Task GetPendingMonths_ReturnsUnauthorized_WhenNotMember()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.GetPendingMonthsAsync(group.Id, "stranger");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetPendingMonths_ReturnsEmpty_WhenNoExpenses()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.GetPendingMonthsAsync(group.Id, "owner");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetStatus_ReturnsUnauthorized_WhenNotMember()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.GetStatusAsync(group.Id, 1, 2026, "stranger");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetStatus_ReturnsAllMembersNotConfirmed_WhenNoConfirmations()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.GetStatusAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Confirmations.Count);
            Assert.All(result.Data.Confirmations, c => Assert.False(c.Confirmed));
            Assert.False(result.Data.IsClosed);
        }

        [Fact]
        public async Task Confirm_ReturnsConflict_WhenCurrentMonth()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var today = DateTime.UtcNow;
            var service = MakeService(db);

            var result = await service.ConfirmAsync(group.Id, "owner", today.Month, today.Year);

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Confirm_ReturnsUnauthorized_WhenNotMember()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.ConfirmAsync(group.Id, "stranger", 1, 2025);

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Confirm_ReturnsFalse_WhenNotLastToConfirm()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.ConfirmAsync(group.Id, "owner", 1, 2025);

            Assert.True(result.IsSuccess);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task Confirm_ReturnsTrue_WhenLastToConfirm()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();

            var service = MakeService(db);
            await service.ConfirmAsync(group.Id, "owner", 1, 2025);

            var result = await service.ConfirmAsync(group.Id, "member", 1, 2025);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.True(await service.IsClosedMonthAsync(group.Id, 1, 2025));
        }

        [Fact]
        public async Task Confirm_IsIdempotent_WhenAlreadyConfirmed()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);
            await service.ConfirmAsync(group.Id, "owner", 1, 2025);

            var result = await service.ConfirmAsync(group.Id, "owner", 1, 2025);

            Assert.True(result.IsSuccess);
            Assert.Single(db.GroupMonthCloseConfirmations.Where(c => c.UserId == "owner"));
        }

        [Fact]
        public async Task Unconfirm_ReturnsNotFound_WhenNoConfirmationExists()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            var result = await service.UnconfirmAsync(group.Id, "owner", 1, 2025);

            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task Unconfirm_Succeeds_WhenConfirmationExists()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);
            await service.ConfirmAsync(group.Id, "owner", 1, 2025);

            var result = await service.UnconfirmAsync(group.Id, "owner", 1, 2025);

            Assert.True(result.IsSuccess);
            Assert.Empty(db.GroupMonthCloseConfirmations.Where(c => c.UserId == "owner"));
        }

        [Fact]
        public async Task Unconfirm_ReturnsConflict_WhenMonthAlreadyClosed()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();

            var service = MakeService(db);
            await service.ConfirmAsync(group.Id, "owner", 1, 2025);
            await service.ConfirmAsync(group.Id, "member", 1, 2025);

            var result = await service.UnconfirmAsync(group.Id, "owner", 1, 2025);

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task IsClosedMonth_ReturnsFalse_WhenNotClosed()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            var service = MakeService(db);

            Assert.False(await service.IsClosedMonthAsync(group.Id, 1, 2025));
        }

        [Fact]
        public async Task Confirm_WritesExpenseSplitShares_WhenClosing()
        {
            var (db, group) = await SetupGroupWithTwoMembersAsync();
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            db.Expenses.Add(new Expense
            {
                Description = "Teste",
                Value = 100f,
                Date = new DateOnly(2025, 1, 15),
                UserId = "owner",
                CategoryId = 1,
                GroupId = group.Id
            });
            await db.SaveChangesAsync();

            var service = MakeService(db);
            await service.ConfirmAsync(group.Id, "owner", 1, 2025);
            await service.ConfirmAsync(group.Id, "member", 1, 2025);

            Assert.Equal(2, db.ExpenseSplitShares.Count());
        }
    }
}
