using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class SplitCalculatorServiceTests
    {
        private static User MakeUser(string id) => new() { Id = id, Name = $"User {id}", Email = $"{id}@test.com" };

        private static async Task<(FinanceContext db, Group group)> SetupGroupAsync(string ownerId, string? memberId = null)
        {
            var db = DbContextFactory.Create();
            db.Users.Add(MakeUser(ownerId));
            if (memberId != null) db.Users.Add(MakeUser(memberId));

            var group = new Group { Name = "Grupo", UserId = ownerId };
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            if (memberId != null)
            {
                db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = memberId, IsActive = true });
                await db.SaveChangesAsync();
            }

            return (db, group);
        }

        private static Expense MakeExpense(string userId, int groupId, decimal value, int month = 1, int year = 2026) => new()
        {
            Description = "Teste",
            Value = (float)value,
            Date = new DateOnly(year, month, 15),
            UserId = userId,
            CategoryId = 1,
            GroupId = groupId
        };

        [Fact]
        public async Task Calculate_ReturnsUnauthorized_WhenNotMember()
        {
            var (db, group) = await SetupGroupAsync("owner");
            var service = new SplitCalculatorService(db);

            var result = await service.CalculateAsync(group.Id, 1, 2026, "stranger");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Calculate_ReturnsZeroBalance_WhenNoExpenses()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var service = new SplitCalculatorService(db);

            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            Assert.All(result.Data!, m => Assert.Equal(0, m.Balance));
        }

        [Fact]
        public async Task Calculate_Equal_SplitsEvenly()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();

            var expense = MakeExpense("owner", group.Id, 100m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var owner = result.Data!.Single(m => m.UserId == "owner");
            var member = result.Data!.Single(m => m.UserId == "member");

            Assert.Equal(50m, owner.Balance);
            Assert.Equal(-50m, member.Balance);
            Assert.Equal("receiver", owner.Direction);
            Assert.Equal("payer", member.Direction);
        }

        [Fact]
        public async Task Calculate_Proportional_SplitsBySalary()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();

            db.GroupMemberConfigs.AddRange(
                new GroupMemberConfig { GroupId = group.Id, UserId = "owner", Salary = 6000m },
                new GroupMemberConfig { GroupId = group.Id, UserId = "member", Salary = 4000m }
            );

            var expense = MakeExpense("owner", group.Id, 1000m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var owner = result.Data!.Single(m => m.UserId == "owner");
            var member = result.Data!.Single(m => m.UserId == "member");

            Assert.Equal(400m, owner.Balance);
            Assert.Equal(-400m, member.Balance);
        }

        [Fact]
        public async Task Calculate_Manual_SplitsByPercentage()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();

            db.GroupSplitConfigShares.AddRange(
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 70 },
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "member", Percentage = 30 }
            );

            var expense = MakeExpense("owner", group.Id, 200m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var owner = result.Data!.Single(m => m.UserId == "owner");
            var member = result.Data!.Single(m => m.UserId == "member");

            Assert.Equal(60m, owner.Balance);
            Assert.Equal(-60m, member.Balance);
        }

        [Fact]
        public async Task Calculate_UsesExpenseSpecificConfig_WhenSet()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var equalConfig = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            var manualConfig = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = false };
            db.GroupSplitConfigs.AddRange(equalConfig, manualConfig);
            await db.SaveChangesAsync();

            db.GroupSplitConfigShares.AddRange(
                new GroupSplitConfigShare { GroupSplitConfigId = manualConfig.Id, UserId = "owner", Percentage = 80 },
                new GroupSplitConfigShare { GroupSplitConfigId = manualConfig.Id, UserId = "member", Percentage = 20 }
            );

            var expense = MakeExpense("owner", group.Id, 100m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { ExpenseId = expense.Id, GroupSplitConfigId = manualConfig.Id });
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var member = result.Data!.Single(m => m.UserId == "member");
            Assert.Equal(-20m, member.Balance);
        }

        [Fact]
        public async Task Calculate_UsesDefaultConfig_WhenExpenseHasNoConfig()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(config);

            var expense = MakeExpense("owner", group.Id, 200m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var member = result.Data!.Single(m => m.UserId == "member");
            Assert.Equal(-100m, member.Balance);
        }

        [Fact]
        public async Task Calculate_SkipsExpenses_WhenNoConfigAvailable()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var expense = MakeExpense("owner", group.Id, 100m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            Assert.All(result.Data!, m => Assert.Equal(0, m.Balance));
        }

        [Fact]
        public async Task Calculate_FiltersExpensesByMonthAndYear()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(config);

            db.Expenses.Add(MakeExpense("owner", group.Id, 100m, month: 1, year: 2026));
            db.Expenses.Add(MakeExpense("owner", group.Id, 200m, month: 2, year: 2026));
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var member = result.Data!.Single(m => m.UserId == "member");
            Assert.Equal(-50m, member.Balance);
        }

        [Fact]
        public async Task Calculate_ReturnsFrozenResult_WhenMonthIsClosed()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            db.Expenses.Add(MakeExpense("owner", group.Id, 100m, month: 1, year: 2025));
            await db.SaveChangesAsync();

            var closeService = new MonthCloseService(db, new SplitCalculatorService(db));
            await closeService.ConfirmAsync(group.Id, "owner", 1, 2025);
            await closeService.ConfirmAsync(group.Id, "member", 1, 2025);

            config.SplitType = SplitType.Manual;
            db.GroupSplitConfigShares.AddRange(
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 90 },
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "member", Percentage = 10 }
            );
            await db.SaveChangesAsync();

            var result = await new SplitCalculatorService(db).CalculateAsync(group.Id, 1, 2025, "owner");

            Assert.True(result.IsSuccess);
            var owner = result.Data!.Single(m => m.UserId == "owner");
            var member = result.Data!.Single(m => m.UserId == "member");
            Assert.Equal(50m, owner.AmountOwed);
            Assert.Equal(50m, member.AmountOwed);
        }

        [Fact]
        public async Task Calculate_Proportional_FallsBackToEqual_WhenNoSalaries()
        {
            var (db, group) = await SetupGroupAsync("owner", "member");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = true };
            db.GroupSplitConfigs.Add(config);

            var expense = MakeExpense("owner", group.Id, 100m);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            var service = new SplitCalculatorService(db);
            var result = await service.CalculateAsync(group.Id, 1, 2026, "owner");

            Assert.True(result.IsSuccess);
            var member = result.Data!.Single(m => m.UserId == "member");
            Assert.Equal(-50m, member.Balance);
        }
    }
}
