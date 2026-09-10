using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Contracts.Workforce;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the org-wide workforce endpoints (<c>/api/workforce</c>): an operative added
/// to a company appears in the register and resolves to a profile by slug.
/// </summary>
public sealed class WorkforceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public WorkforceApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AddedOperative_AppearsInRegister_AndResolvesBySlug()
    {
        var client = _factory.CreateClient();
        // R15: the register is tenant-scoped, so add the operative to the signed-in caller's own company.
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;
        var name = "Workforce Tester " + Guid.NewGuid().ToString("N")[..6];

        var add = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900742", "Scaffolding", null));
        Assert.True(add.IsSuccessStatusCode);

        var register = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        var row = Assert.Single(register!, o => o.Name == name);
        Assert.Equal("Scaffolding", row.Trade);
        Assert.Equal(ComplianceState.Pending, row.State);   // no cards yet — pending, never invented (SF-8)

        var detail = await client.GetFromJsonAsync<OperativeDetailDto>($"/api/workforce/{row.Slug}");
        Assert.NotNull(detail);
        Assert.Equal(name, detail!.Name);
        Assert.Empty(detail.Qualifications);
    }

    [Fact]
    public async Task UnknownSlug_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workforce/no-such-operative");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // UAT-007 — an operative in a company other than the caller's tenant resolves by (company, engagement)
    public async Task Operative_InAnotherCompany_ResolvesByEngagementId()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var myCompany = me!.CompanyId!.Value;

        // Find a company that is NOT the caller's tenant and has at least one operative (a subcontractor the
        // main contractor manages) — exactly the drill-down that produced "no operative found".
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var otherCompany = companies!.First(c => c.Id != myCompany && c.Operatives > 0);
        var detail = await client.GetFromJsonAsync<CompanyDetailDto>($"/api/organisation/companies/{otherCompany.Slug}");
        var op = detail!.Operatives[0];

        // The by-engagement lookup resolves the operative even though they are outside the caller's tenant.
        var profile = await client.GetFromJsonAsync<OperativeDetailDto>(
            $"/api/workforce/by-engagement/{otherCompany.Id}/{op.EngagementId}");

        Assert.NotNull(profile);
        Assert.Equal(op.Name, profile!.Name);
        Assert.Equal(otherCompany.Id, profile.CompanyId);
    }

    [Fact] // a mismatched (company, engagement) pair is a 404, never another company's operative
    public async Task ByEngagement_WithUnknownIds_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/workforce/by-engagement/{Guid.NewGuid()}/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // UAT-010 (SF-5) — the captured card image reference reaches the operative's qualifications
    public async Task OperativeDetail_ExposesQualificationImageReference()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;
        var name = "Evidence Case " + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900611", "Plasterer", null));
        var row = (await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce"))!.Single(o => o.Name == name);

        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var cscs = types!.First(t => t.Name == "CSCS Card");
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        const string image = "https://evidence.example/cscs-card.png";
        await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(row.PersonId, cscs.Id, "CS-IMG", name, null, future, NeedsReview: false, ImageReference: image));

        var detail = await client.GetFromJsonAsync<OperativeDetailDto>($"/api/workforce/{row.Slug}");
        var qualification = Assert.Single(detail!.Qualifications);
        Assert.Equal(image, qualification.ImageReference);
    }

    [Fact] // UAT-014 (MC-8) — a card-compliant main-contractor operative with no induction is not "Compliant"
    public async Task Operative_WithValidCardButNoInduction_ShowsInductionRequired()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;   // the caller's tenant is a main contractor in the seed
        var name = "Induction Case " + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900555", "Groundworker", null));

        var register = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        var personId = register!.Single(o => o.Name == name).PersonId;

        // Give them one valid, in-date card so card compliance alone would read "Compliant".
        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var cscs = types!.First(t => t.Name == "CSCS Card");
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        var capture = await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(personId, cscs.Id, "CS-UAT14", name, null, future, NeedsReview: false));
        Assert.True(capture.IsSuccessStatusCode);

        // With no induction the worker is not site-ready (MC-8): reported as at risk / "Induction required",
        // never "Compliant" (the misleading status the tester reported).
        var after = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        var updated = after!.Single(o => o.Name == name);
        Assert.NotEqual(ComplianceState.Compliant, updated.State);
        Assert.Equal("Induction required", updated.StatusLabel);
    }
}
