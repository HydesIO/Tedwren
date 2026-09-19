using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Application.Mobile;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies <see cref="MobileHazardService"/> (M5): reuse of the hazard domain via the repository, client-id
/// idempotency (R4/R16), company scoping (R15), capture-UTC preservation (R11) and reporter normalization.
/// </summary>
public sealed class MobileHazardServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();

    private static (MobileHazardService Service, InMemoryHazardReportRepository Repo) CreateSut()
    {
        var repo = new InMemoryHazardReportRepository();
        return (new MobileHazardService(repo), repo);
    }

    private static MobileReportHazardRequest Request(Guid clientId, DateTimeOffset? capturedUtc = null) =>
        new(clientId, "NearMiss", "Scaffold tag missing", "Level 3", 51.5, -0.1, "img-ref", "High", "Working at height",
            capturedUtc ?? DateTimeOffset.UtcNow);

    [Fact]
    public async Task Report_AppendsWithClientIdReferenceAndCapture()
    {
        var (service, _) = CreateSut();
        var clientId = Guid.NewGuid();
        var captured = DateTimeOffset.UtcNow.AddMinutes(-15);

        var dto = await service.ReportAsync(Company, "Alex Operative", Request(clientId, captured));

        Assert.Equal(clientId, dto.Id);
        Assert.StartsWith("HAZ-", dto.Reference);
        Assert.Equal("Open", dto.Status);
        Assert.Equal("NearMiss", dto.Kind);
        Assert.True(dto.HasPhoto);
        Assert.Equal(captured, dto.ReportedUtc);
        Assert.Equal("Alex Operative", dto.ReportedBy);
    }

    [Fact]
    public async Task Report_IsIdempotentOnClientId()
    {
        var (service, repo) = CreateSut();
        var clientId = Guid.NewGuid();

        var first = await service.ReportAsync(Company, "Alex", Request(clientId));
        var second = await service.ReportAsync(Company, "Alex", Request(clientId));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(await repo.GetByCompanyAsync(Company));
    }

    [Fact]
    public async Task Report_CrossCompanyClientId_IsRefused()
    {
        var (service, _) = CreateSut();
        var clientId = Guid.NewGuid();
        await service.ReportAsync(Company, "Alex", Request(clientId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReportAsync(Guid.NewGuid(), "Sam", Request(clientId)));
    }

    [Fact]
    public async Task Report_BlankReporter_BecomesSystem()
    {
        var (service, _) = CreateSut();
        var dto = await service.ReportAsync(Company, "  ", Request(Guid.NewGuid()));
        Assert.Equal("System", dto.ReportedBy);
    }

    [Fact]
    public async Task Report_RequiresCompanyAndDescription()
    {
        var (service, _) = CreateSut();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(Guid.Empty, "Alex", Request(Guid.NewGuid())));
        var blankDescription = Request(Guid.NewGuid()) with { Description = "  " };
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(Company, "Alex", blankDescription));
    }
}
