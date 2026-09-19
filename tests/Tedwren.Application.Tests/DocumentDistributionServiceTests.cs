using Tedwren.Abstractions.Contracts.Documents;
using Tedwren.Application.Documents;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for document distribution &amp; acknowledgement (PRD §8.2): the per-recipient completion matrix,
/// recipient de-duplication, validation, idempotent acknowledgement, and company scoping (R15).
/// </summary>
public sealed class DocumentDistributionServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static DocumentDistributionService CreateSut() =>
        new(new InMemoryDocumentDistributionRepository(), new InMemoryImageStore());

    private static CreateDistributionRequest NewRequest(params string[] recipients) =>
        new("Site safety policy", "Policy", "All operatives", null, null, recipients);

    [Fact]
    public async Task Create_BuildsMatrix_OneRowPerDistinctRecipient_NoneSigned()
    {
        var service = CreateSut();

        var dto = await service.CreateAsync(CompanyA, "Site Manager", NewRequest("Jane Smith", "John Doe"));

        Assert.Equal(2, dto.TotalCount);
        Assert.Equal(0, dto.SignedCount);
        Assert.Equal("Site Manager", dto.SentBy);
    }

    [Fact]
    public async Task Create_DeDuplicatesRecipients_CaseInsensitive()
    {
        var service = CreateSut();

        var dto = await service.CreateAsync(CompanyA, "Mgr", NewRequest("Jane Smith", "jane smith", "  Jane Smith  "));

        Assert.Equal(1, dto.TotalCount);
    }

    [Fact]
    public async Task Create_NoRecipients_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(CompanyA, "Mgr", NewRequest("   ")));
    }

    [Fact]
    public async Task Create_BlankTitle_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            CompanyA, "Mgr", new CreateDistributionRequest("  ", null, null, null, null, new[] { "Jane" })));
    }

    [Fact]
    public async Task Get_ReturnsMatrix_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.CreateAsync(CompanyA, "Mgr", NewRequest("Jane Smith", "John Doe"));

        Assert.Null(await service.GetAsync(CompanyB, dto.Id));   // R15 — cross-tenant is invisible

        var detail = await service.GetAsync(CompanyA, dto.Id);
        Assert.NotNull(detail);
        Assert.Equal(2, detail!.Acknowledgements.Count);
        Assert.All(detail.Acknowledgements, a => Assert.False(a.Acknowledged));
    }

    [Fact]
    public async Task Acknowledge_Idempotent_AndScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.CreateAsync(CompanyA, "Mgr", NewRequest("Jane Smith", "John Doe"));
        var row = (await service.GetAsync(CompanyA, dto.Id))!.Acknowledgements[0];

        Assert.False(await service.AcknowledgeAsync(CompanyB, row.Id));   // R15 — cannot sign cross-tenant
        Assert.True(await service.AcknowledgeAsync(CompanyA, row.Id));
        Assert.True(await service.AcknowledgeAsync(CompanyA, row.Id));    // idempotent — still succeeds

        var detail = await service.GetAsync(CompanyA, dto.Id);
        Assert.Equal(1, detail!.Distribution.SignedCount);               // signed once, not twice
        var signed = Assert.Single(detail.Acknowledgements, a => a.Acknowledged);
        Assert.NotNull(signed.AcknowledgedUtc);
    }

    [Fact]
    public async Task List_ReturnsCompanyDistributions_WithCounts_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.CreateAsync(CompanyA, "Mgr", NewRequest("Jane Smith", "John Doe"));
        await service.CreateAsync(CompanyB, "Other", NewRequest("Someone Else"));
        var row = (await service.GetAsync(CompanyA, dto.Id))!.Acknowledgements[0];
        await service.AcknowledgeAsync(CompanyA, row.Id);

        var list = await service.ListAsync(CompanyA);

        var only = Assert.Single(list);                                  // R15 — CompanyB's distribution excluded
        Assert.Equal(2, only.TotalCount);
        Assert.Equal(1, only.SignedCount);
    }
}
