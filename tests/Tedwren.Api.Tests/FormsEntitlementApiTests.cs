using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Api.Endpoints;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the server-side Forms module gate (SF-22, §10.1 Q2): the Forms endpoints are reachable only when
/// the caller's company holds the "forms" entitlement, and fail closed (403) when it does not. Uses its own
/// web host so toggling the demo company's entitlement never affects the other Forms API test classes.
/// </summary>
public sealed class FormsEntitlementApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // The demo company the test auth-bypass authenticates as (seeded with the Forms module enabled).
    private static readonly Guid DemoCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");
    private readonly WebApplicationFactory<Program> _factory;

    public FormsEntitlementApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Forms_Endpoints_AreGatedByTheModuleEntitlement()
    {
        var client = _factory.CreateClient();

        // Enabled by default for the demo company → reachable.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/forms/templates")).StatusCode);

        // Disable the module → the group fails closed with 403.
        var off = await client.PutAsJsonAsync($"/api/entitlements/{DemoCompanyId}/forms", new EntitlementEndpoints.SetEntitlementBody(false));
        Assert.Equal(HttpStatusCode.NoContent, off.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/forms/templates")).StatusCode);

        // Re-enable → reachable again.
        await client.PutAsJsonAsync($"/api/entitlements/{DemoCompanyId}/forms", new EntitlementEndpoints.SetEntitlementBody(true));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/forms/templates")).StatusCode);
    }
}
