using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Billing;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the platform admin billing surface (<c>/api/admin/billing</c>). The test host runs
/// as a platform admin (TestBypass) with the in-memory billing store and no GoCardless token, so read surfaces
/// return 200 and collection actions return 503 ("not configured") rather than calling out.
/// </summary>
public sealed class AdminBillingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses the shared test host.</summary>
    public AdminBillingApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact]
    public async Task Mandates_AsPlatformAdmin_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/billing/mandates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mandates = await response.Content.ReadFromJsonAsync<List<MandateDto>>();
        Assert.NotNull(mandates);
    }

    [Fact]
    public async Task Payments_AsPlatformAdmin_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/billing/payments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StartMandateSetup_WithoutGoCardlessConfigured_Returns503()
    {
        var client = CreateClient();

        var response = await client.PostAsync($"/api/admin/billing/companies/{Guid.NewGuid()}/mandate/setup", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Events_AsPlatformAdmin_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/billing/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WithoutValidSignature_Returns401()
    {
        var client = CreateClient();

        // No configured secret and no valid Webhook-Signature header: the endpoint fails closed.
        using var content = new StringContent("{\"events\":[]}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/webhooks/gocardless", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetSubscription_ThenReadOverview_PersistsMeter()
    {
        var client = CreateClient();
        var companyId = Guid.NewGuid();

        var put = await client.PutAsJsonAsync(
            $"/api/admin/billing/companies/{companyId}/subscription",
            new SetSubscriptionRequest("operatives", null, "Subcontractor plan"));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var overview = await client.GetFromJsonAsync<CompanyBillingOverviewDto>($"/api/admin/billing/companies/{companyId}/overview");
        Assert.NotNull(overview);
        Assert.Equal("operatives", overview!.Subscription?.MeterKey);
    }
}
