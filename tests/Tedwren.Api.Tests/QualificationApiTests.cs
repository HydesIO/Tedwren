using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Api.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the qualification endpoints, driven through <see cref="WebApplicationFactory{TEntryPoint}"/>
/// in the API's default (mock) mode — no database required, so these run in CI and locally.
/// </summary>
public sealed class QualificationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public QualificationApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact] // SF-12
    public async Task GetTypes_ReturnsDefaultLibrary()
    {
        var client = _factory.CreateClient();

        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");

        Assert.NotNull(types);
        Assert.Contains(types!, t => t.Name == "CSCS Card");
    }

    [Fact] // SF-5 → SF-6
    public async Task CaptureThenConfirm_MovesCardFromUncheckedToChecked()
    {
        var client = _factory.CreateClient();
        var person = Guid.NewGuid();
        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var typeId = types![0].Id;

        // Capture (SF-5): created, unchecked.
        var capture = await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(person, typeId, "CS999", "Test Holder", null, null, NeedsReview: true));
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var afterCapture = await client.GetFromJsonAsync<List<QualificationCardDto>>($"/api/qualifications/people/{person}/cards");
        var card = Assert.Single(afterCapture!);
        Assert.Equal(Tedwren.Abstractions.Common.CardVerificationState.ReadUnchecked, card.VerificationState);

        // Confirm (SF-6): now checked.
        var confirm = await client.PostAsJsonAsync($"/api/qualifications/cards/{card.Id}/confirm", new { ConfirmedBy = "Alex Morgan" });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var afterConfirm = await client.GetFromJsonAsync<List<QualificationCardDto>>($"/api/qualifications/people/{person}/cards");
        Assert.Equal(Tedwren.Abstractions.Common.CardVerificationState.CustomerChecked, afterConfirm!.Single().VerificationState);
    }

    [Fact] // SF-12/SF-11 CRUD over HTTP: create a type, map it to a trade, and the guard blocks deleting a mapped type (Q21).
    public async Task LibraryManagement_CreateMapAndGuardedDelete()
    {
        var client = _factory.CreateClient();   // TestBypass identity is an Administrator of the seed company (platform admin)

        var createType = await client.PostAsJsonAsync("/api/qualifications/types",
            new CreateQualificationTypeRequest("PASMA", "Access", "PASMA", 60, false, Global: true));
        Assert.Equal(HttpStatusCode.Created, createType.StatusCode);
        var typeId = (await createType.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var manage = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types/manage");
        Assert.Contains(manage!, t => t.Id == typeId && t.Name == "PASMA" && t.IsGlobal);

        var createReq = await client.PostAsJsonAsync("/api/qualifications/requirements",
            new CreateTradeRequirementRequest("Tower Erector", typeId, LegalMandatory: true, ClientRequired: false, Global: true));
        Assert.Equal(HttpStatusCode.Created, createReq.StatusCode);
        var reqId = (await createReq.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var reqs = await client.GetFromJsonAsync<List<TradeQualificationRequirementDto>>("/api/qualifications/requirements");
        Assert.Contains(reqs!, r => r.Id == reqId && r.Accreditation == "PASMA" && r.LegalMandatory);

        // Deleting a mapped type is guarded (409); after the mapping is removed it deletes cleanly.
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/qualifications/types/{typeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/qualifications/requirements/{reqId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/qualifications/types/{typeId}")).StatusCode);
    }

    [Fact] // The library-management endpoints require console write access — an operative token is forbidden (secure-by-default).
    public async Task Manage_endpoints_reject_an_operative_token()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Auth:TestBypass", "false"));
        var client = factory.CreateClient();
        var token = new JwtOperativeTokenIssuer(new JwtOptions())
            .IssueAccessToken(Guid.NewGuid(), Guid.NewGuid(), "Alex Operative", "device-1").Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var manage = await client.GetAsync("/api/qualifications/types/manage");
        var create = await client.PostAsJsonAsync("/api/qualifications/types",
            new CreateQualificationTypeRequest("Blocked", null, null, 0, false, Global: true));

        Assert.Equal(HttpStatusCode.Forbidden, manage.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    /// <summary>The created-id envelope the write endpoints return (<c>{ id }</c>).</summary>
    private sealed record IdResponse(Guid Id);
}
