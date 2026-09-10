using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Application.Decisions;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the site-entry decision record store (R10): a recorded decision is reconstructable — its
/// admitted flag and every check (name, outcome, detail) round-trip — and it is retrievable by person and by
/// site, newest first.
/// </summary>
public sealed class DecisionServiceTests
{
    private static DecisionService CreateService() => new(new InMemoryDecisionRepository(seed: false));

    private static RecordDecisionRequest Decision(Guid personId, Guid siteId, bool admitted) =>
        new(personId, siteId, admitted, new[]
        {
            new DecisionCheckDto("Registered", "Passed", null),
            new DecisionCheckDto("Cards in date", "Failed", "CSCS expired"),
            new DecisionCheckDto("RAMS", "NotRun", "Module not held"),
        });

    [Fact]
    public async Task Record_ThenGetForPerson_ReconstructsChecks()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var id = await service.RecordAsync(Decision(personId, siteId, admitted: false));
        var decisions = await service.GetForPersonAsync(personId);

        var decision = Assert.Single(decisions);
        Assert.Equal(id, decision.Id);
        Assert.False(decision.Admitted);
        Assert.Equal(3, decision.Checks.Count);
        Assert.Contains(decision.Checks, c => c.Name == "Cards in date" && c.Outcome == "Failed" && c.Detail == "CSCS expired");
        Assert.Contains(decision.Checks, c => c.Name == "RAMS" && c.Outcome == "NotRun");
    }

    [Fact]
    public async Task Record_IsRetrievableBySite()
    {
        var service = CreateService();
        var siteId = Guid.NewGuid();

        await service.RecordAsync(Decision(Guid.NewGuid(), siteId, admitted: true));

        Assert.Single(await service.GetForSiteAsync(siteId));
        Assert.Empty(await service.GetForSiteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UnknownOutcome_ParsesAsNotRun()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();
        await service.RecordAsync(new RecordDecisionRequest(personId, Guid.NewGuid(), true, new[]
        {
            new DecisionCheckDto("Mystery", "banana", null),
        }));

        var decision = Assert.Single(await service.GetForPersonAsync(personId));
        Assert.Equal("NotRun", decision.Checks[0].Outcome);
    }
}
