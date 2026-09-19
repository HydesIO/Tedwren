using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Application.Mobile;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies <see cref="MobileEvidenceService"/> (M5): append-only capture, client-id idempotency (R4/R16),
/// company scoping (R15) and capture-UTC preservation (R11).
/// </summary>
public sealed class MobileEvidenceServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();

    private static (MobileEvidenceService Service, InMemoryEvidenceItemRepository Repo) CreateSut()
    {
        var repo = new InMemoryEvidenceItemRepository();
        return (new MobileEvidenceService(repo), repo);
    }

    private static MobileReportEvidenceRequest Request(Guid clientId, DateTimeOffset? capturedUtc = null) =>
        new(clientId, "Cracked slab", 51.5, -0.1, "img-ref", capturedUtc ?? DateTimeOffset.UtcNow);

    [Fact]
    public async Task Report_AppendsWithClientIdAndCapture()
    {
        var (service, repo) = CreateSut();
        var clientId = Guid.NewGuid();
        var captured = DateTimeOffset.UtcNow.AddMinutes(-30);

        var dto = await service.ReportAsync(Company, Person, Request(clientId, captured));

        Assert.Equal(clientId, dto.Id);
        Assert.True(dto.HasPhoto);
        Assert.Equal(captured, dto.CapturedUtc);
        var stored = await repo.GetAsync(clientId);
        Assert.Equal(Company, stored!.CompanyId);
        Assert.Equal(Person, stored.PersonId);
        Assert.Equal("img-ref", stored.PhotoReference);
    }

    [Fact]
    public async Task Report_IsIdempotentOnClientId()
    {
        var (service, repo) = CreateSut();
        var clientId = Guid.NewGuid();

        var first = await service.ReportAsync(Company, Person, Request(clientId));
        var second = await service.ReportAsync(Company, Person, Request(clientId));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(await repo.GetByCompanyAsync(Company));
    }

    [Fact]
    public async Task Report_CrossCompanyClientId_IsRefused()
    {
        var (service, _) = CreateSut();
        var clientId = Guid.NewGuid();
        await service.ReportAsync(Company, Person, Request(clientId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReportAsync(Guid.NewGuid(), Person, Request(clientId)));
    }

    [Fact]
    public async Task Report_RequiresCompanyAndPerson()
    {
        var (service, _) = CreateSut();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(Guid.Empty, Person, Request(Guid.NewGuid())));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(Company, Guid.Empty, Request(Guid.NewGuid())));
    }
}
