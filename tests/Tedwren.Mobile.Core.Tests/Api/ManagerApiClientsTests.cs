using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the manager API clients map responses correctly and surface write failures as <see cref="ApiException"/>.</summary>
public class ManagerApiClientsTests
{
    [Fact]
    public async Task Dashboard_maps_the_summary()
    {
        var summary = new DashboardSummaryDto(
            new DashboardKpisDto(2, 7, 3, 91.5, 4),
            new ComplianceBreakdownDto(91.5, 7, 0, 0, 1, 8),
            Array.Empty<SiteRiskRowDto>());
        var client = new ManagerApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(summary)));

        var dto = await client.GetDashboardAsync();

        Assert.Equal(7, dto!.Kpis.Operatives);
        Assert.Equal(91.5, dto.Compliance.CompliancePercent);
    }

    [Fact]
    public async Task Muster_maps_people_and_data_age()
    {
        var siteId = Guid.NewGuid();
        var generated = DateTimeOffset.UtcNow.AddMinutes(-3);
        var muster = new MusterDto(siteId, generated,
            new[] { new MusterPersonDto(Guid.NewGuid(), "Alex", null, null, generated) },
            new[] { new CompetencyCoverDto("First Aid", true, 1) });
        var client = new ManagerSiteEntryApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(muster)));

        var dto = await client.GetMusterAsync(siteId);

        Assert.Equal(siteId, dto!.SiteId);
        Assert.Single(dto.People);
        Assert.Equal(generated, dto.GeneratedUtc);
    }

    [Fact]
    public async Task Decide_posts_and_maps_the_result()
    {
        var result = new EntryDecisionResultDto(true, null, true, Guid.NewGuid(), 42, Array.Empty<DecisionCheckResultDto>());
        var client = new ManagerSiteEntryApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(result)));

        var dto = await client.DecideAsync(new ManagerDecideRequest(Guid.NewGuid(), Guid.NewGuid(), null, "reason"));

        Assert.True(dto!.Admitted);
        Assert.True(dto.WasOverridden);
    }

    [Fact]
    public async Task Decide_throws_on_a_non_success_status()
    {
        var client = new ManagerSiteEntryApiClient(FakeHttp.Returning(HttpStatusCode.Forbidden, new StringContent(string.Empty)));

        await Assert.ThrowsAsync<ApiException>(() =>
            client.DecideAsync(new ManagerDecideRequest(Guid.NewGuid(), Guid.NewGuid(), null, "reason")));
    }

    [Fact]
    public async Task Operatives_maps_the_register()
    {
        var rows = new[]
        {
            new OperativeListItemDto(Guid.NewGuid(), Guid.NewGuid(), "alex-op", "Alex", "Electrician", "Acme", ComplianceState.Compliant, "Compliant", null),
        };
        var client = new ManagerWorkforceApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(rows)));

        var list = await client.GetOperativesAsync();

        Assert.Single(list);
        Assert.Equal("Alex", list[0].Name);
    }

    [Fact]
    public async Task Approve_throws_when_forbidden_for_a_read_only_auditor()
    {
        var client = new ManagerFormsApiClient(FakeHttp.Returning(HttpStatusCode.Forbidden, new StringContent(string.Empty)));

        await Assert.ThrowsAsync<ApiException>(() => client.ApproveAsync(Guid.NewGuid(), "looks good"));
    }

    [Fact]
    public async Task Image_returns_bytes_on_success_and_null_when_missing()
    {
        var ok = new ManagerImageApiClient(FakeHttp.Returning(HttpStatusCode.OK, new ByteArrayContent(new byte[] { 1, 2, 3 })));
        Assert.Equal(3, (await ok.GetImageAsync("ref"))!.Length);

        var missing = new ManagerImageApiClient(FakeHttp.Returning(HttpStatusCode.NotFound, new StringContent(string.Empty)));
        Assert.Null(await missing.GetImageAsync("ref"));
    }

    [Fact]
    public async Task Evidence_captures_pass_the_person_filter_on_the_query_string()
    {
        var person = Guid.NewGuid();
        string? requestedPath = null;
        var http = FakeHttp.Routed(request =>
        {
            requestedPath = request.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<EvidenceCaptureDto>()),
            };
        });
        var client = new ManagerEvidenceApiClient(http);

        await client.GetCapturesAsync(person);

        Assert.Contains($"personId={person}", requestedPath);
    }
}
