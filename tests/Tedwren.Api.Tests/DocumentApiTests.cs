using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Documents;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for document distribution &amp; acknowledgement (<c>/api/documents</c>, PRD §8.2). The group is
/// gated on the paid HSE module (fails closed). Each test uses its own host so in-memory state does not leak.
/// </summary>
public sealed class DocumentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public DocumentApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — document distribution is a paid module and fails closed without it
    public async Task Documents_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/documents");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — distribute to recipients, read the completion matrix, sign one receipt, see the count rise
    public async Task Documents_Distribute_Matrix_Acknowledge()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var create = await client.PostAsJsonAsync("/api/documents",
            new CreateDistributionRequest("Site safety policy", "Policy", "All operatives", null, null,
                new[] { "Jane Smith", "John Doe" }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var dto = await create.Content.ReadFromJsonAsync<DocumentDistributionDto>();
        Assert.Equal(2, dto!.TotalCount);
        Assert.Equal(0, dto.SignedCount);

        var detail = await client.GetFromJsonAsync<DocumentDistributionDetailDto>($"/api/documents/{dto.Id}");
        Assert.Equal(2, detail!.Acknowledgements.Count);

        var ackId = detail.Acknowledgements[0].Id;
        var sign = await client.PostAsync($"/api/documents/acknowledgements/{ackId}/sign", content: null);
        Assert.Equal(HttpStatusCode.NoContent, sign.StatusCode);

        var after = await client.GetFromJsonAsync<DocumentDistributionDetailDto>($"/api/documents/{dto.Id}");
        Assert.Equal(1, after!.Distribution.SignedCount);
        Assert.Single(after.Acknowledgements, a => a.Acknowledged);

        Assert.Single((await client.GetFromJsonAsync<List<DocumentDistributionDto>>("/api/documents"))!);
    }
}
