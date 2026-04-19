using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class GroupSplitConfigServiceTests
    {
        private static User MakeUser(string id, string email) => new() { Id = id, Name = $"User {id}", Email = email };

        private static Group MakeGroup(string ownerId) => new() { Name = "Grupo", UserId = ownerId };

        private static async Task<(FinanceContext db, Group group)> SetupGroupWithOwnerAsync(string ownerId)
        {
            var db = DbContextFactory.Create();
            db.Users.Add(MakeUser(ownerId, $"{ownerId}@test.com"));
            var group = MakeGroup(ownerId);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            return (db, group);
        }

        [Fact]
        public async Task GetByGroupId_ReturnsUnauthorized_WhenNotMember()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var service = new GroupSplitConfigService(db);

            var result = await service.GetByGroupIdAsync(group.Id, "stranger");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetByGroupId_ReturnsEmptyList_WhenNoConfigs()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var service = new GroupSplitConfigService(db);

            var result = await service.GetByGroupIdAsync(group.Id, "owner");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetByGroupId_ReturnsConfigs_WhenMember()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.Users.Add(MakeUser("member", "member@test.com"));
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.GetByGroupIdAsync(group.Id, "member");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenNotOwner()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "member", new CreateGroupSplitConfigDto { SplitType = SplitType.Equal });

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenTypeAlreadyExists()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "owner", new CreateGroupSplitConfigDto { SplitType = SplitType.Equal });

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenManualSharesDontSumTo100()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "owner", new CreateGroupSplitConfigDto
            {
                SplitType = SplitType.Manual,
                Shares = [new GroupSplitConfigShareDto { UserId = "owner", Percentage = 60 }]
            });

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Create_Succeeds_ForEqual()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "owner", new CreateGroupSplitConfigDto
            {
                SplitType = SplitType.Equal,
                IsDefault = true
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(SplitType.Equal, result.Data!.SplitType);
            Assert.True(result.Data.IsDefault);
        }

        [Fact]
        public async Task Create_Succeeds_ForManualWithValidShares()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.Users.Add(MakeUser("member", "member@test.com"));
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "owner", new CreateGroupSplitConfigDto
            {
                SplitType = SplitType.Manual,
                Shares =
                [
                    new GroupSplitConfigShareDto { UserId = "owner", Percentage = 60 },
                    new GroupSplitConfigShareDto { UserId = "member", Percentage = 40 }
                ]
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Shares.Count);
        }

        [Fact]
        public async Task Create_ClearsOtherDefault_WhenIsDefaultIsTrue()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.CreateAsync(group.Id, "owner", new CreateGroupSplitConfigDto
            {
                SplitType = SplitType.Proportional,
                IsDefault = true
            });

            Assert.True(result.IsSuccess);
            var configs = db.GroupSplitConfigs.ToList();
            Assert.Single(configs.Where(c => c.IsDefault));
            Assert.Equal(SplitType.Proportional, configs.Single(c => c.IsDefault).SplitType);
        }

        [Fact]
        public async Task Update_ReturnsUnauthorized_WhenNotOwner()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var configId = db.GroupSplitConfigs.First().Id;
            var service = new GroupSplitConfigService(db);

            var result = await service.UpdateAsync(configId, "member", new UpdateGroupSplitConfigDto { IsDefault = true });

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task Update_ReturnsConflict_WhenManualSharesDontSumTo100()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true });
            await db.SaveChangesAsync();
            var configId = db.GroupSplitConfigs.First().Id;
            var service = new GroupSplitConfigService(db);

            var result = await service.UpdateAsync(configId, "owner", new UpdateGroupSplitConfigDto
            {
                IsDefault = true,
                Shares = [new GroupSplitConfigShareDto { UserId = "owner", Percentage = 50 }]
            });

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Update_ReplacesShares_ForManual()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();
            db.GroupSplitConfigShares.Add(new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 100 });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.UpdateAsync(config.Id, "owner", new UpdateGroupSplitConfigDto
            {
                IsDefault = true,
                Shares =
                [
                    new GroupSplitConfigShareDto { UserId = "owner", Percentage = 70 },
                    new GroupSplitConfigShareDto { UserId = "member", Percentage = 30 }
                ]
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(2, db.GroupSplitConfigShares.Count());
        }

        [Fact]
        public async Task Delete_ReturnsConflict_WhenOnlyConfig()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            await db.SaveChangesAsync();
            var configId = db.GroupSplitConfigs.First().Id;
            var service = new GroupSplitConfigService(db);

            var result = await service.DeleteAsync(configId, "owner");

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task Delete_Succeeds_WhenMultipleConfigs()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = false });
            await db.SaveChangesAsync();
            var configId = db.GroupSplitConfigs.First(c => c.SplitType == SplitType.Proportional).Id;
            var service = new GroupSplitConfigService(db);

            var result = await service.DeleteAsync(configId, "owner");

            Assert.True(result.IsSuccess);
            Assert.Single(db.GroupSplitConfigs);
        }

        [Fact]
        public async Task Delete_SoftDeletes_RecordRemainsInDb()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = false });
            await db.SaveChangesAsync();
            var configId = db.GroupSplitConfigs.First(c => c.SplitType == SplitType.Proportional).Id;
            var service = new GroupSplitConfigService(db);

            await service.DeleteAsync(configId, "owner");

            var raw = db.GroupSplitConfigs.IgnoreQueryFilters().Single(c => c.Id == configId);
            Assert.True(raw.IsDeleted);
        }

        [Fact]
        public async Task Delete_Succeeds_WhenConfigIsInUseByExpense()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Equal, IsDefault = true });
            db.GroupSplitConfigs.Add(new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Proportional, IsDefault = false });
            await db.SaveChangesAsync();
            var configToDelete = db.GroupSplitConfigs.First(c => c.SplitType == SplitType.Proportional);
            var expense = new Expense { Description = "Teste", Value = 100, Date = new DateOnly(2026, 1, 1), UserId = "owner", CategoryId = 1, GroupId = group.Id };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { ExpenseId = expense.Id, GroupSplitConfigId = configToDelete.Id });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            var result = await service.DeleteAsync(configToDelete.Id, "owner");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task OnMemberJoined_Adds0PercentShare_InManualConfigs()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();
            db.GroupSplitConfigShares.Add(new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 100 });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            await service.OnMemberJoinedAsync(group.Id, "new-member");

            var shares = db.GroupSplitConfigShares.ToList();
            Assert.Equal(2, shares.Count);
            Assert.Equal(0, shares.First(s => s.UserId == "new-member").Percentage);
        }

        [Fact]
        public async Task OnMemberLeft_TransfersPercentageToOwner()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();
            db.GroupSplitConfigShares.AddRange(
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 60 },
                new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "member", Percentage = 40 }
            );
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            await service.OnMemberLeftAsync(group.Id, "member");

            var shares = db.GroupSplitConfigShares.ToList();
            Assert.Single(shares);
            Assert.Equal(100, shares.Single(s => s.UserId == "owner").Percentage);
        }

        [Fact]
        public async Task OnMemberJoined_DoesNotAddDuplicate_WhenShareAlreadyExists()
        {
            var (db, group) = await SetupGroupWithOwnerAsync("owner");
            var config = new GroupSplitConfig { GroupId = group.Id, SplitType = SplitType.Manual, IsDefault = true };
            db.GroupSplitConfigs.Add(config);
            await db.SaveChangesAsync();
            db.GroupSplitConfigShares.Add(new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "owner", Percentage = 100 });
            db.GroupSplitConfigShares.Add(new GroupSplitConfigShare { GroupSplitConfigId = config.Id, UserId = "member", Percentage = 0 });
            await db.SaveChangesAsync();
            var service = new GroupSplitConfigService(db);

            await service.OnMemberJoinedAsync(group.Id, "member");

            Assert.Equal(2, db.GroupSplitConfigShares.Count());
        }
    }
}
