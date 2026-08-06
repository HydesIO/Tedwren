using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Qualifications;
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
}
