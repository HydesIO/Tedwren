using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the dashboard aggregation endpoints (<c>/api/dashboard</c>): KPI counts and the
/// compliance breakdown reflect operatives added through the organisation endpoints.
/// </summary>
public sealed class DashboardApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public DashboardApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Summary_ReflectsAddedOperative_AndTalliesCompliance()
    {
        var client = _factory.CreateClient();
        // R15: the compliance tally is tenant-scoped, so add the operative to the caller's own company.
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;

        var add = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, "Dash Tester " + Guid.NewGuid().ToString("N")[..6], "07700900733", "Roofing", null));
        Assert.True(add.IsSuccessStatusCode);

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard");

        Assert.NotNull(summary);
        Assert.True(summary!.Kpis.Companies >= 1);
        Assert.True(summary.Kpis.Operatives >= 1);
        Assert.Equal(summary.Kpis.Operatives, summary.Compliance.Total);
        // No cards captured yet, so the new operative is pending — never counted as compliant (SF-8).
        Assert.True(summary.Compliance.Pending >= 1);
    }

    [Fact]
    public async Task Compliance_ReturnsBreakdown()
    {
        var client = _factory.CreateClient();

        var breakdown = await client.GetFromJsonAsync<ComplianceBreakdownDto>("/api/dashboard/compliance");

        Assert.NotNull(breakdown);
        Assert.Equal(breakdown!.Total, breakdown.Compliant + breakdown.AtRisk + breakdown.NonCompliant + breakdown.Pending);
    }
}
