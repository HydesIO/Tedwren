using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Contracts.Qualifications;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the compliance-pack endpoints in the API's default (mock) mode: the readiness
/// gate (SUB-14), account-free token + passcode access (R8), download (SUB-16), tracking (SUB-20) and revoke
/// (SUB-21). A card is captured with a past expiry so the readiness gate has a real blocking issue to surface.
/// </summary>
public sealed class CompliancePackApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so pack state does not leak between tests.</summary>
    public CompliancePackApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    // Captures a card with a past expiry, so the person is not site-ready (a blocking readiness issue).
    private static async Task<Guid> CaptureExpiredCardAsync(HttpClient client)
    {
        var personId = Guid.NewGuid();
        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var typeId = types![0].Id;
        var request = new CaptureCardRequest(personId, typeId, "CARD-1", "Holder",
            new DateOnly(2024, 1, 1), new DateOnly(2026, 6, 1), false);   // expired
        var response = await client.PostAsJsonAsync("/api/qualifications/cards", request);
        response.EnsureSuccessStatusCode();
        return personId;
    }

    [Fact] // SUB-14 — readiness gate blocks, acknowledgement sends
    public async Task Build_IsBlockedByReadiness_ThenSendsWhenAcknowledged()
    {
        var client = CreateClient();
        var companyId = Guid.NewGuid();
        var personId = await CaptureExpiredCardAsync(client);

        var blockedRequest = new BuildPackRequest(companyId, "Pack", "Alex", null, null, null, new[] { personId }, null, null, false, null);
        var blocked = await client.PostAsJsonAsync("/api/packs", blockedRequest);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var blockedResult = await blocked.Content.ReadFromJsonAsync<BuildPackResult>();
        Assert.Contains(blockedResult!.Issues, i => i.Blocking);

        var ackRequest = blockedRequest with { AcknowledgeIssues = true };
        var sent = await (await client.PostAsJsonAsync("/api/packs", ackRequest)).Content.ReadFromJsonAsync<BuildPackResult>();
        Assert.True(sent!.Sent);
        Assert.False(string.IsNullOrWhiteSpace(sent.Token));
    }

    [Fact] // R8 + SUB-16 + SUB-20 + SUB-21 — recipient access, download, tracking, revoke
    public async Task Recipient_ViewsWithPasscode_Downloads_ThenRevokeBlocks()
    {
        var client = CreateClient();
        var companyId = Guid.NewGuid();
        var personId = await CaptureExpiredCardAsync(client);
        var built = await (await client.PostAsJsonAsync("/api/packs",
            new BuildPackRequest(companyId, "Pack", "Alex", null, null, null, new[] { personId }, "SECRET1", 30, true, null)))
            .Content.ReadFromJsonAsync<BuildPackResult>();

        // Wrong passcode is refused (R8/SUB-18).
        var refused = await client.GetAsync($"/api/packs/view?token={built!.Token}&passcode=nope");
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        // Correct passcode grants the fixed snapshot.
        var view = await client.GetFromJsonAsync<CompliancePackViewDto>($"/api/packs/view?token={built.Token}&passcode=SECRET1");
        Assert.Single(view!.Subjects);

        // A PDF download is a valid document and is tracked.
        var pdf = await client.GetByteArrayAsync($"/api/packs/download.pdf?token={built.Token}&passcode=SECRET1");
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);

        var list = await client.GetFromJsonAsync<List<PackListItemDto>>($"/api/packs/company/{companyId}");
        Assert.Equal(1, list!.Single().DownloadedCount);
        Assert.True(list.Single().OpenedCount >= 1);

        // Revoke, then the link no longer works (SUB-21).
        var revoke = await client.PostAsync($"/api/packs/company/{companyId}/{built.PackId}/revoke", null);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        var afterRevoke = await client.GetAsync($"/api/packs/view?token={built.Token}&passcode=SECRET1");
        Assert.Equal(HttpStatusCode.Forbidden, afterRevoke.StatusCode);
    }
}
