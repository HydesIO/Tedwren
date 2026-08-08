using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Timesheets;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the timesheet endpoints in the API's default (mock) mode. Exercises the
/// valuation-period list (MC-24), the approval lifecycle (SUB-9/SUB-12) and CSV/Excel export (SUB-10) over the
/// seeded demo week.
/// </summary>
public sealed class TimesheetApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Matches InMemoryTimesheetStore's demo seed.
    private static readonly Guid CompanyId = Guid.Parse("44444444-4444-4444-8444-000000000001");
    private const string Week = "2026-08-03";

    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so timesheet state does not leak between tests.</summary>
    public TimesheetApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // MC-24 — valuation-period view
    public async Task CompanyWeek_ListsSeededTimesheets()
    {
        var client = CreateClient();

        var list = await client.GetFromJsonAsync<List<TimesheetSummaryDto>>($"/api/timesheets/company/{CompanyId}?week={Week}");

        Assert.Equal(3, list!.Count);
        Assert.Contains(list, t => t.OperativeName == "M. Adeyemi" && t.Status == TimesheetState.Submitted);
    }

    [Fact] // SUB-9 + SUB-12 — approve a submitted timesheet, and returning it is guarded once approved
    public async Task Submitted_CanBeApproved_AtChosenScope()
    {
        var client = CreateClient();
        var list = await client.GetFromJsonAsync<List<TimesheetSummaryDto>>($"/api/timesheets/company/{CompanyId}?week={Week}");
        var submitted = list!.Single(t => t.Status == TimesheetState.Submitted);

        var approved = await (await client.PostAsJsonAsync(
            $"/api/timesheets/{submitted.Id}/approve", new ApproveTimesheetRequest("Priya Shah", ApprovalScope.Project)))
            .Content.ReadFromJsonAsync<TimesheetDto>();

        Assert.Equal(TimesheetState.Approved, approved!.Status);
        Assert.Equal(ApprovalScope.Project, approved.DecisionScope);

        // An approved timesheet cannot then be returned (guarded) → NotFound.
        var returned = await client.PostAsJsonAsync(
            $"/api/timesheets/{submitted.Id}/return", new ReturnTimesheetRequest("Priya Shah", "too late"));
        Assert.Equal(HttpStatusCode.NotFound, returned.StatusCode);
    }

    [Fact] // SUB-10 — CSV and Excel export
    public async Task Export_ReturnsCsvAndXlsx()
    {
        var client = CreateClient();

        var csv = await client.GetStringAsync($"/api/timesheets/company/{CompanyId}/export.csv?week={Week}");
        Assert.StartsWith("Operative,Site,Week starting,Status,Hours", csv);

        var xlsx = await client.GetByteArrayAsync($"/api/timesheets/company/{CompanyId}/export.xlsx?week={Week}");
        Assert.True(xlsx.Length > 0);
        Assert.Equal((byte)'P', xlsx[0]);
        Assert.Equal((byte)'K', xlsx[1]);   // ZIP signature — a valid .xlsx
    }
}
