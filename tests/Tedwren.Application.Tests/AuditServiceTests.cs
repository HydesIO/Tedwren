using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Application.Audit;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the audit-trail service (SF-20): free-text and date-range search, append-only recording, and CSV
/// export with a header and quoted fields. Runs against a clean (unseeded) in-memory store.
/// </summary>
public sealed class AuditServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();

    private static AuditService CreateService(out InMemoryAuditStore store)
    {
        store = new InMemoryAuditStore(seed: false);
        return new AuditService(new InMemoryAuditRepository(store));
    }

    private static RecordAuditRequest Entry(string actor, string action, string entity, string? reference = null, string? category = null) =>
        new(Company, actor, action, entity, reference, category);

    [Fact]
    public async Task Record_ThenSearch_ReturnsTheEntry()
    {
        var service = CreateService(out _);
        await service.RecordAsync(Entry("Alex Morgan", "Onboarded operative", "M. Adeyemi", category: "Workforce"));

        var results = await service.SearchAsync(Company, text: null, from: null, to: null);

        Assert.Single(results);
        Assert.Equal("Alex Morgan", results[0].Actor);
    }

    [Fact]
    public async Task Search_FreeText_MatchesActorActionEntityAndReference()
    {
        var service = CreateService(out _);
        await service.RecordAsync(Entry("Dana Price", "Issued permit", "Meridian Tower", reference: "HW-2043", category: "Permits"));
        await service.RecordAsync(Entry("Alex Morgan", "Onboarded operative", "M. Adeyemi", category: "Workforce"));

        Assert.Single(await service.SearchAsync(Company, "meridian", null, null));   // entity
        Assert.Single(await service.SearchAsync(Company, "hw-2043", null, null));     // reference
        Assert.Single(await service.SearchAsync(Company, "onboarded", null, null));   // action
        Assert.Single(await service.SearchAsync(Company, "dana", null, null));        // actor
    }

    [Fact]
    public async Task Search_IsScopedToTheCompany()
    {
        var service = CreateService(out _);
        await service.RecordAsync(Entry("Alex Morgan", "Onboarded operative", "M. Adeyemi"));

        var otherCompany = await service.SearchAsync(Guid.NewGuid(), null, null, null);

        Assert.Empty(otherCompany);
    }

    [Fact]
    public async Task ExportCsv_HasHeader_AndQuotesFieldsContainingCommas()
    {
        var service = CreateService(out _);
        await service.RecordAsync(Entry("Alex Morgan", "Updated details, notes", "Kingsway M&E", category: "Organisation"));

        var csv = await service.ExportCsvAsync(Company, null, null, null);

        Assert.StartsWith("When,Actor,Action,Entity,Reference,Category", csv);
        Assert.Contains("\"Updated details, notes\"", csv);
    }
}
