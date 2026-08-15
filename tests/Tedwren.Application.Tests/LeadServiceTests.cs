using Tedwren.Abstractions.Contracts.Leads;
using Tedwren.Application.Leads;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="LeadService"/> (sales-lead pipeline, Web Plan §7). Uses the in-memory lead store
/// so the pipeline rules are verified without a database: creation logs an activity note, a status change is
/// recorded automatically, conversion sets the terminal state and links the account, and anonymous capture
/// deduplicates against an open lead.
/// </summary>
public sealed class LeadServiceTests
{
    private static LeadService CreateService() => new(new InMemoryLeadRepository());

    [Fact]
    public async Task Create_StoresLead_AndLogsCreationNote()
    {
        var service = CreateService();

        var lead = await service.CreateAsync(
            new CreateLeadRequest("ABC Construction Ltd", "Jane Doe", "jane@abc.co.uk", "Oxford", "MainContractor",
                NumberOfSites: 12, EstimatedRevenue: 15000m, AccountManager: "James Darby"), actor: "James Darby");

        Assert.Equal("ABC Construction Ltd", lead.CompanyName);
        Assert.Equal("MainContractor", lead.Model);
        Assert.Equal("New", lead.Status);

        var detail = await service.GetAsync(lead.Id);
        Assert.NotNull(detail);
        Assert.Single(detail!.Notes);
        Assert.Contains("created", detail.Notes[0].Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_RequiresCompanyName()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new CreateLeadRequest("  "), actor: null));
    }

    [Fact]
    public async Task UpdateStatus_RecordsAutomaticNote_WithTransitionLabel()
    {
        var service = CreateService();
        var lead = await service.CreateAsync(new CreateLeadRequest("XYZ Ltd"), actor: null);

        var updated = await service.UpdateStatusAsync(lead.Id, new UpdateLeadStatusRequest("Qualified"), actor: "Owner");

        Assert.Equal("Qualified", updated!.Status);
        var detail = await service.GetAsync(lead.Id);
        Assert.Contains(detail!.Notes, n => n.StatusChange == "New → Qualified");
    }

    [Fact]
    public async Task UpdateStatus_RejectsUnknownStatus()
    {
        var service = CreateService();
        var lead = await service.CreateAsync(new CreateLeadRequest("XYZ Ltd"), actor: null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateStatusAsync(lead.Id, new UpdateLeadStatusRequest("Nonsense"), actor: null));
    }

    [Fact]
    public async Task Convert_SetsConverted_LinksAccount_AndLogsIt()
    {
        var service = CreateService();
        var lead = await service.CreateAsync(new CreateLeadRequest("XYZ Ltd"), actor: null);
        var account = Guid.NewGuid();

        var converted = await service.ConvertAsync(lead.Id, new ConvertLeadRequest(account), actor: "Owner");

        Assert.Equal("Converted", converted!.Status);
        Assert.Equal(account, converted.ConvertedAccountId);
        var detail = await service.GetAsync(lead.Id);
        Assert.Contains(detail!.Notes, n => n.StatusChange == "Converted");
    }

    [Fact]
    public async Task Capture_DeduplicatesAgainstOpenLead()
    {
        var service = CreateService();

        var first = await service.CaptureAsync(new CaptureLeadRequest("ABC Ltd", "Jane", "jane@abc.co.uk", Source: "demo"));
        var second = await service.CaptureAsync(new CaptureLeadRequest("ABC Ltd", "Jane", "jane@abc.co.uk", Source: "contact", Note: "Also messaged us"));

        Assert.Equal(first.Id, second.Id); // same open lead, not a duplicate
        var all = await service.ListAsync();
        Assert.Single(all);

        // The second touch is logged against the existing lead.
        var detail = await service.GetAsync(first.Id);
        Assert.Contains(detail!.Notes, n => n.Body.Contains("Also messaged us"));
    }

    [Fact]
    public async Task Capture_CreatesNewLead_WhenNoOpenMatch()
    {
        var service = CreateService();

        await service.CaptureAsync(new CaptureLeadRequest("ABC Ltd", ContactEmail: "a@abc.co.uk"));
        await service.CaptureAsync(new CaptureLeadRequest("DEF Ltd", ContactEmail: "d@def.co.uk"));

        var all = await service.ListAsync();
        Assert.Equal(2, all.Count);
    }
}
