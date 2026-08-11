using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Onboarding;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Contracts.Workforce;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the self-service onboarding flow (D2, SF-4/SUB-2): admin creates a link, the
/// recipient view is passcode-gated, submitting details creates an operative, and a captured card lands
/// unconfirmed.
/// </summary>
public sealed class OnboardingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host (auth bypass on → requests are Administrator).</summary>
    public OnboardingApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Create_View_Submit_Capture_Flow()
    {
        var client = _factory.CreateClient();
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var companyId = companies![0].Id;

        var create = await client.PostAsJsonAsync("/api/onboarding",
            new CreateOnboardingLinkRequest(companyId, "Jordan Fields", "Groundworks", RequirePasscode: true));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var link = (await create.Content.ReadFromJsonAsync<OnboardingLinkDto>())!;
        Assert.False(string.IsNullOrEmpty(link.Token));
        Assert.False(string.IsNullOrEmpty(link.Passcode));

        // Wrong/absent passcode is refused (SUB-18).
        var forbidden = await client.GetAsync($"/api/onboarding/view?token={link.Token}&passcode=WRONG");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        // Correct passcode returns the view.
        var view = await client.GetFromJsonAsync<OnboardingViewDto>($"/api/onboarding/view?token={link.Token}&passcode={link.Passcode}");
        Assert.NotNull(view);
        Assert.False(view!.DetailsSubmitted);

        // Submitting details creates the operative (SF-1/SF-2).
        var submitted = await client.PostAsJsonAsync(
            $"/api/onboarding/submit?token={link.Token}&passcode={link.Passcode}",
            new SubmitOnboardingDetailsRequest("Jordan Fields", "07700900901", "Groundworks"));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var afterSubmit = (await submitted.Content.ReadFromJsonAsync<OnboardingViewDto>())!;
        Assert.True(afterSubmit.DetailsSubmitted);

        var register = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        Assert.Contains(register!, o => o.Name == "Jordan Fields");

        // Capturing a card records it (unconfirmed).
        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var typeId = types![0].Id;
        var captured = await client.PostAsJsonAsync(
            $"/api/onboarding/cards?token={link.Token}&passcode={link.Passcode}",
            new CaptureOnboardingCardRequest(typeId, "CARD-123", "Jordan Fields", null, new DateOnly(2027, 1, 1), null, null));
        Assert.Equal(HttpStatusCode.OK, captured.StatusCode);
        var afterCard = (await captured.Content.ReadFromJsonAsync<OnboardingViewDto>())!;
        Assert.Single(afterCard.Cards);
    }

    [Fact]
    public async Task View_UnknownToken_IsForbidden()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/onboarding/view?token=nope&passcode=");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
