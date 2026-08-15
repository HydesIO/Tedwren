using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Leads;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the lead pipeline (Web Plan §7): anonymous capture from the marketing site, and the
/// platform-admin CRUD/status/notes/convert flow. The in-memory test host acts as an Administrator.
/// </summary>
public sealed class LeadApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public LeadApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Admin_Create_Get_Status_Note_Convert_Flow()
    {
        var client = _factory.CreateClient();
        var company = $"Lead {Guid.NewGuid():N}";

        // Create.
        var create = await client.PostAsJsonAsync("/api/leads",
            new CreateLeadRequest(company, "Jane", "jane@example.com", "Oxford", "MainContractor", 12, 15000m, "James Darby"));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var lead = (await create.Content.ReadFromJsonAsync<LeadDto>())!;
        Assert.Equal("New", lead.Status);

        // Get with activity log (creation note present).
        var detail = await client.GetFromJsonAsync<LeadDetailDto>($"/api/leads/{lead.Id}");
        Assert.NotNull(detail);
        Assert.NotEmpty(detail!.Notes);

        // Status change.
        var status = await client.PostAsJsonAsync($"/api/leads/{lead.Id}/status", new UpdateLeadStatusRequest("Qualified"));
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Equal("Qualified", (await status.Content.ReadFromJsonAsync<LeadDto>())!.Status);

        // Add a note.
        var note = await client.PostAsJsonAsync($"/api/leads/{lead.Id}/notes", new AddLeadNoteRequest("Called, sending a proposal."));
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);

        // Convert.
        var accountId = Guid.NewGuid();
        var convert = await client.PostAsJsonAsync($"/api/leads/{lead.Id}/convert", new ConvertLeadRequest(accountId));
        Assert.Equal(HttpStatusCode.OK, convert.StatusCode);
        var converted = (await convert.Content.ReadFromJsonAsync<LeadDto>())!;
        Assert.Equal("Converted", converted.Status);
        Assert.Equal(accountId, converted.ConvertedAccountId);

        // Appears in the list.
        var list = await client.GetFromJsonAsync<List<LeadDto>>("/api/leads");
        Assert.Contains(list!, l => l.Id == lead.Id);
    }

    [Fact]
    public async Task Capture_IsAnonymous_AndDeduplicatesOpenLead()
    {
        var client = _factory.CreateClient();
        var company = $"Capture {Guid.NewGuid():N}";
        var email = "dup@example.com";

        var first = await client.PostAsJsonAsync("/api/leads/capture", new CaptureLeadRequest(company, "Sam", email, Source: "demo"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstLead = (await first.Content.ReadFromJsonAsync<LeadDto>())!;

        var second = await client.PostAsJsonAsync("/api/leads/capture", new CaptureLeadRequest(company, "Sam", email, Source: "contact"));
        var secondLead = (await second.Content.ReadFromJsonAsync<LeadDto>())!;

        Assert.Equal(firstLead.Id, secondLead.Id); // deduplicated onto the open lead
    }

    [Fact]
    public async Task Get_MissingLead_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/leads/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
