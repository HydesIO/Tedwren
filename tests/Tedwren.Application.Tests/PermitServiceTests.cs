using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Permits;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the permit lifecycle (PRD §8.2): approve/close transitions, company scoping (R15) and the
/// Expired display state derived from the valid period.
/// </summary>
public sealed class PermitServiceTests
{
    private static readonly Guid CompanyA = Guid.Parse("11111111-1111-4111-8111-000000000001");
    private static readonly Guid CompanyB = Guid.Parse("11111111-1111-4111-8111-000000000002");

    /// <summary>Fake current-user scoping the caller to a fixed company.</summary>
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _companyId;
        public FakeCurrentUser(Guid companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Tester", "Administrator", _companyId));
    }

    private static (PermitService Service, InMemoryPermitRepository Repo) CreateSut(Guid callerCompany)
    {
        var repo = new InMemoryPermitRepository();
        return (new PermitService(repo, audit: null, currentUser: new FakeCurrentUser(callerCompany)), repo);
    }

    private static async Task<Guid> SeedPermitAsync(InMemoryPermitRepository repo, Guid companyId, PermitStatus status, DateOnly? validTo = null)
    {
        var permit = new Permit { CompanyId = companyId, PermitType = "Hot Works", SiteName = "Tower A", Status = status, ValidTo = validTo };
        await repo.AddAsync(permit);
        return permit.Id;
    }

    [Fact]
    public async Task Approve_ThenClose_TransitionsThroughLifecycle()
    {
        var (service, repo) = CreateSut(CompanyA);
        var id = await SeedPermitAsync(repo, CompanyA, PermitStatus.Issued);

        Assert.True(await service.ApproveAsync(id));
        Assert.Equal(PermitStatus.Approved, (await repo.GetAsync(id))!.Status);

        Assert.True(await service.CloseAsync(id, "Work complete"));
        Assert.Equal(PermitStatus.Closed, (await repo.GetAsync(id))!.Status);
    }

    [Fact]
    public async Task Approve_OtherCompanysPermit_ReturnsFalse_AndDoesNotChangeIt()
    {
        var (service, repo) = CreateSut(CompanyA);
        var id = await SeedPermitAsync(repo, CompanyB, PermitStatus.Issued);

        // R15: the caller (Company A) cannot approve Company B's permit.
        Assert.False(await service.ApproveAsync(id));
        Assert.Equal(PermitStatus.Issued, (await repo.GetAsync(id))!.Status);
    }

    [Fact]
    public async Task Approve_ClosedPermit_Throws()
    {
        var (service, repo) = CreateSut(CompanyA);
        var id = await SeedPermitAsync(repo, CompanyA, PermitStatus.Closed);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(id));
    }

    [Fact]
    public async Task Close_DraftPermit_Throws()
    {
        var (service, repo) = CreateSut(CompanyA);
        var id = await SeedPermitAsync(repo, CompanyA, PermitStatus.Draft);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseAsync(id, null));
    }

    [Fact]
    public async Task List_ExpiredPermit_ShowsExpiredStatus()
    {
        var (service, repo) = CreateSut(CompanyA);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await SeedPermitAsync(repo, CompanyA, PermitStatus.Issued, validTo: yesterday);

        var permits = await service.ListForCompanyAsync(CompanyA);

        Assert.Equal("Expired", Assert.Single(permits).Status);
    }

    [Fact]
    public async Task Approve_MissingPermit_ReturnsFalse()
    {
        var (service, _) = CreateSut(CompanyA);

        Assert.False(await service.ApproveAsync(Guid.NewGuid()));
    }
}
