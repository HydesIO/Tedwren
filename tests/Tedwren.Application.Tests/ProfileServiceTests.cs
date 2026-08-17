using Tedwren.Abstractions.Contracts.Account;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Account;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the self-service account rules (SF-20) on <see cref="ProfileService"/> over clean in-memory stores:
/// the caller only ever sees/edits their own record (R15), change-password verifies the current secret and
/// rehashes, avatars are stored and resolved to an image URL, and a user can never self-change their role or
/// status. Every test uses a purpose-created store, mutating no shared data.
/// </summary>
public sealed class ProfileServiceTests
{
    private const string CurrentPassword = "CurrentPass1";
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    /// <summary>Builds the service over clean stores, seeding one active user as the caller, and returns its id.</summary>
    private static ProfileService CreateSut(
        out InMemoryUserStore users,
        out Guid callerId,
        AccessRole role = AccessRole.Administrator)
    {
        users = new InMemoryUserStore(seed: false);
        var caller = new User
        {
            CompanyId = CompanyId,
            Name = "Alex Caller",
            Email = "alex.caller@example.com",
            Role = role,
            Status = UserStatus.Active,
            PasswordHash = PasswordHasher.Hash(CurrentPassword),
            PasswordSetUtc = DateTimeOffset.UtcNow.AddDays(-30),
        };
        users.Users[caller.Id] = caller;
        callerId = caller.Id;

        var companies = new InMemoryOrganisationStore(seed: false);
        companies.Companies[CompanyId] = new Company { Id = CompanyId, Name = "Meridian Construction" };

        return new ProfileService(
            new FakeCurrentUserService(callerId, role),
            new InMemoryUserRepository(users),
            new InMemoryCompanyRepository(companies),
            new InMemoryImageStore());
    }

    [Fact] // SF-20 — the caller sees their own record, with the resolved company name
    public async Task GetMyProfile_ReturnsCallersOwnRecord()
    {
        var sut = CreateSut(out _, out _);

        var me = await sut.GetMyProfileAsync();

        Assert.NotNull(me);
        Assert.Equal("alex.caller@example.com", me!.Email);
        Assert.Equal("Meridian Construction", me.CompanyName);
        Assert.True(me.IsAdministrator);
    }

    [Fact] // R15 — an unauthenticated caller resolves to no profile
    public async Task GetMyProfile_Unauthenticated_ReturnsNull()
    {
        var users = new InMemoryUserStore(seed: false);
        var companies = new InMemoryOrganisationStore(seed: false);
        var sut = new ProfileService(
            new FakeCurrentUserService(userId: null, "Auditor"),
            new InMemoryUserRepository(users),
            new InMemoryCompanyRepository(companies),
            new InMemoryImageStore());

        Assert.Null(await sut.GetMyProfileAsync());
    }

    [Fact] // SF-20 — name, email and mobile round-trip
    public async Task UpdateMyProfile_ChangesDetails()
    {
        var sut = CreateSut(out var users, out var id);

        var updated = await sut.UpdateMyProfileAsync(new UpdateMyProfileRequest("Alex Renamed", "alex.new@example.com", "07700 900123"));

        Assert.NotNull(updated);
        Assert.Equal("Alex Renamed", updated!.Name);
        Assert.Equal("alex.new@example.com", updated.Email);
        Assert.Equal("07700 900123", updated.Mobile);
        Assert.Equal("07700 900123", users.Users[id].Mobile);
    }

    [Fact] // email is the sign-in identity: it must not collide with another account
    public async Task UpdateMyProfile_DuplicateEmail_Throws()
    {
        var sut = CreateSut(out var users, out _);
        users.Users[Guid.NewGuid()] = new User
        {
            CompanyId = CompanyId, Name = "Someone Else", Email = "taken@example.com", Status = UserStatus.Active,
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateMyProfileAsync(new UpdateMyProfileRequest("Alex Caller", "taken@example.com", null)));
    }

    [Fact] // an invalid email is rejected before it is stored
    public async Task UpdateMyProfile_InvalidEmail_Throws()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateMyProfileAsync(new UpdateMyProfileRequest("Alex Caller", "not-an-email", null)));
    }

    [Fact] // R15 — a profile update never changes the caller's role or status
    public async Task UpdateMyProfile_DoesNotChangeRoleOrStatus()
    {
        var sut = CreateSut(out var users, out var id, role: AccessRole.ComplianceManager);

        await sut.UpdateMyProfileAsync(new UpdateMyProfileRequest("Alex Caller", "alex.caller@example.com", "123"));

        Assert.Equal(AccessRole.ComplianceManager, users.Users[id].Role);
        Assert.Equal(UserStatus.Active, users.Users[id].Status);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsFalse()
    {
        var sut = CreateSut(out var users, out var id);
        var before = users.Users[id].PasswordHash;

        var changed = await sut.ChangePasswordAsync(new ChangePasswordRequest("wrong-password", "BrandNewPass1"));

        Assert.False(changed);
        Assert.Equal(before, users.Users[id].PasswordHash); // unchanged
    }

    [Fact]
    public async Task ChangePassword_Correct_RehashesAndVerifies()
    {
        var sut = CreateSut(out var users, out var id);

        var changed = await sut.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, "BrandNewPass1"));

        Assert.True(changed);
        Assert.True(PasswordHasher.Verify("BrandNewPass1", users.Users[id].PasswordHash));
        Assert.False(PasswordHasher.Verify(CurrentPassword, users.Users[id].PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_TooShort_Throws()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, "short")));
    }

    [Fact] // SF-20/R9 — an avatar is stored and resolved to the authorised image URL
    public async Task UpdateAvatar_StoresImage_AndSetsUrl()
    {
        var sut = CreateSut(out var users, out var id);
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var updated = await sut.UpdateAvatarAsync(new UpdateAvatarRequest(base64, "image/png"));

        Assert.NotNull(updated);
        Assert.NotNull(updated!.AvatarUrl);
        Assert.StartsWith("/api/images/", updated.AvatarUrl);
        Assert.NotNull(users.Users[id].AvatarImageReference);
    }

    [Fact]
    public async Task UpdateAvatar_InvalidBase64_Throws()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateAvatarAsync(new UpdateAvatarRequest("!!!not-base64!!!", "image/png")));
    }

    /// <summary>A stub current-user resolver returning a fixed caller id and role.</summary>
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        private readonly Guid? _userId;
        private readonly string _role;

        public FakeCurrentUserService(Guid? userId, string role)
        {
            _userId = userId;
            _role = role;
        }

        public FakeCurrentUserService(Guid userId, AccessRole role) : this(userId, role.ToString())
        {
        }

        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Alex Caller", _role, CompanyId, UserId: _userId));
    }
}
