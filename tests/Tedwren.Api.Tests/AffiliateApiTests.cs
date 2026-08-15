using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Affiliates;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the affiliate programme (Web Plan §7): platform-admin create/list/get and payouts, and
/// the anonymous, token-gated agreement view/sign/PDF flow. The in-memory test host acts as an Administrator
/// and uses the outbox email sender, so setup/confirmation emails don't dispatch.
/// </summary>
public sealed class AffiliateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public AffiliateApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Create_Get_Payout_MarkPaid_Flow()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/affiliates",
            new CreateAffiliateRequest("Pat Partner", "pat@partner.example", "Partner Co", "James Darby", "REF-1", 0.20m, 0.33m));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var affiliate = (await create.Content.ReadFromJsonAsync<AffiliateDto>())!;

        var detail = await client.GetFromJsonAsync<AffiliateDetailDto>($"/api/affiliates/{affiliate.Id}");
        Assert.NotNull(detail!.Agreement);

        // Raise a payout and mark it paid.
        var payout = await (await client.PostAsJsonAsync($"/api/affiliates/{affiliate.Id}/payouts",
            new CreatePayoutRequest(990m, "GBP", null, "Q1"))).Content.ReadFromJsonAsync<AffiliatePayoutDto>();
        Assert.Equal("Pending", payout!.Status);

        var paid = await (await client.PostAsync($"/api/affiliates/payouts/{payout.Id}/paid", null))
            .Content.ReadFromJsonAsync<AffiliatePayoutDto>();
        Assert.Equal("Paid", paid!.Status);

        var list = await client.GetFromJsonAsync<List<AffiliateDto>>("/api/affiliates");
        Assert.Contains(list!, a => a.Id == affiliate.Id);
    }

    [Fact]
    public async Task Agreement_IsAnonymouslyViewable_Signable_AndYieldsPdf()
    {
        var admin = _factory.CreateClient();
        var create = await admin.PostAsJsonAsync("/api/affiliates",
            new CreateAffiliateRequest("Sign Me", "sign@partner.example"));
        var affiliate = (await create.Content.ReadFromJsonAsync<AffiliateDto>())!;
        var detail = await admin.GetFromJsonAsync<AffiliateDetailDto>($"/api/affiliates/{affiliate.Id}");
        var token = detail!.Agreement!.Token;

        // Anonymous client (no auth) can view and sign the token-gated agreement.
        var anon = _factory.CreateClient();
        var view = await anon.GetFromJsonAsync<AffiliateAgreementViewDto>($"/api/affiliate-agreements/{token}");
        Assert.NotNull(view);
        Assert.True(view!.Signable);

        var sign = await anon.PostAsJsonAsync($"/api/affiliate-agreements/{token}/sign",
            new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Sign Me"));
        Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
        Assert.Equal("Signed", (await sign.Content.ReadFromJsonAsync<AffiliateAgreementViewDto>())!.Status);

        // The signed PDF is downloadable.
        var pdf = await anon.GetAsync($"/api/affiliate-agreements/{token}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True((await pdf.Content.ReadAsByteArrayAsync()).Length > 0);
    }

    [Fact]
    public async Task Agreement_UnknownToken_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/affiliate-agreements/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
