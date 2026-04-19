using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using MyFinBackend.Tests.Helpers;

namespace MyFinBackend.Tests.Services
{
    public class GroupServiceTests
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

        [Fact]
        public async Task GetGroupsByUserId_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.GetGroupsByUserIdAsync("user-a", "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task GetGroupsByUserId_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.GetGroupsByUserIdAsync("user-a", "user-a");

            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task GetGroupsByUserId_ReturnsOwnedGroups()
        {
            using var db = DbContextFactory.Create();
            var user = MakeUser("user-a", "a@test.com");
            var group = MakeGroup("user-a");
            db.Users.Add(user);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.GetGroupsByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task GetGroupsByUserId_ExcludesInactiveMemberships()
        {
            using var db = DbContextFactory.Create();
            var userA = MakeUser("user-a", "a@test.com");
            var userB = MakeUser("user-b", "b@test.com");
            var group = MakeGroup("user-b");
            db.Users.AddRange(userA, userB);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "user-a", IsActive = false });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.GetGroupsByUserIdAsync("user-a", "user-a");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task CreateGroup_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.CreateGroupAsync(MakeGroup("user-a"), "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task CreateGroup_Succeeds_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.CreateGroupAsync(MakeGroup("user-a"), "user-a");

            Assert.True(result.IsSuccess);
            Assert.Equal("Grupo A", result.Data!.Name);
            Assert.Single(db.Groups);
        }

        [Fact]
        public async Task InviteMember_ReturnsNotFound_WhenUserEmailNotFound()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = group.Id, UserEmail = "notfound@test.com" }, "owner");

            Assert.Equal(ServiceError.NotFound, result.Error);
        }

        [Fact]
        public async Task InviteMember_ReturnsUnauthorized_WhenNotGroupOwner()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var other = MakeUser("other", "other@test.com");
            var intruder = MakeUser("intruder", "intruder@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, other, intruder);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = group.Id, UserEmail = "other@test.com" }, "intruder");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task InviteMember_ReturnsUnauthorized_WhenInvitingSelf()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var group = MakeGroup("owner");
            db.Users.Add(owner);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = group.Id, UserEmail = "owner@test.com" }, "owner");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task InviteMember_ReturnsConflict_WhenAlreadyMember()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = group.Id, UserEmail = "member@test.com" }, "owner");

            Assert.Equal(ServiceError.Conflict, result.Error);
        }

        [Fact]
        public async Task InviteMember_Succeeds_WhenValid()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.InviteMemberAsync(
                new MemberGrouToAddDto { GroupId = group.Id, UserEmail = "member@test.com" }, "owner");

            Assert.True(result.IsSuccess);
            Assert.False(result.Data!.IsActive);
            Assert.Single(db.GroupMembers);
        }

        [Fact]
        public async Task AcceptInvite_ReturnsUnauthorized_WhenUserMismatch()
        {
            using var db = DbContextFactory.Create();
            var service = MakeService(db);

            var result = await service.AcceptInviteAsync(
                new GroupMember { GroupId = 1, UserId = "user-a" }, "user-b");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task AcceptInvite_SetsIsActiveTrue()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = false });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.AcceptInviteAsync(
                new GroupMember { GroupId = group.Id, UserId = "member" }, "member");

            Assert.True(result.IsSuccess);
            Assert.True(db.GroupMembers.First().IsActive);
        }

        [Fact]
        public async Task DeleteGroup_ReturnsUnauthorized_WhenNotOwner()
        {
            using var db = DbContextFactory.Create();
            var group = MakeGroup("owner");
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteGroupAsync(group.Id, "intruder");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task DeleteGroup_Succeeds_WhenOwner()
        {
            using var db = DbContextFactory.Create();
            var group = MakeGroup("owner");
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteGroupAsync(group.Id, "owner");

            Assert.True(result.IsSuccess);
            Assert.Empty(db.Groups);
        }

        [Fact]
        public async Task DeleteMember_ReturnsUnauthorized_WhenNeitherOwnerNorSelf()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteMemberAsync("member", group.Id, "intruder");

            Assert.Equal(ServiceError.Unauthorized, result.Error);
        }

        [Fact]
        public async Task DeleteMember_Succeeds_WhenOwnerRemovesMember()
        {
            using var db = DbContextFactory.Create();
            var owner = MakeUser("owner", "owner@test.com");
            var member = MakeUser("member", "member@test.com");
            var group = MakeGroup("owner");
            db.Users.AddRange(owner, member);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member", IsActive = true });
            await db.SaveChangesAsync();
            var service = MakeService(db);

            var result = await service.DeleteMemberAsync("member", group.Id, "owner");

            Assert.True(result.IsSuccess);
            Assert.Empty(db.GroupMembers);
        }
    }
}
