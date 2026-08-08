using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Inductions;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the induction endpoints in the API's default (mock) mode: the whole phone-browser
/// flow (start → steps → server-scored quiz → finalise), the MC-4 completion gate (409), and the R5 guarantee
/// that the device never receives quiz answers.
/// </summary>
public sealed class InductionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Matches the API mock seed (DefaultInductionTemplate).
    private static readonly Guid CompanyId = Guid.Parse("66666666-6666-4666-8666-000000000001");
    private static readonly Guid TemplateId = Guid.Parse("66666666-6666-4666-8666-000000000010");

    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so induction state does not leak between tests.</summary>
    public InductionApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static readonly Dictionary<string, int> CorrectAnswers = new() { ["q1"] = 0, ["q2"] = 1, ["q3"] = 2 };

    [Fact] // R5 — the device session response contains no correct answers
    public async Task StartedSession_DoesNotLeakQuizAnswers()
    {
        var client = CreateClient();
        var start = await client.PostAsJsonAsync("/api/inductions/sessions",
            new StartInductionRequest(CompanyId, TemplateId, Guid.NewGuid(), "M. Adeyemi"));
        var raw = await start.Content.ReadAsStringAsync();

        Assert.DoesNotContain("CorrectOptionIndex", raw, StringComparison.OrdinalIgnoreCase);
        var session = System.Text.Json.JsonSerializer.Deserialize<InductionSessionDto>(raw,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(3, session!.Questions.Count);
    }

    [Fact] // MC-4 — completion is gated, then the whole flow completes with a reference
    public async Task WholeFlow_GatesThenCompletes()
    {
        var client = CreateClient();
        var session = await (await client.PostAsJsonAsync("/api/inductions/sessions",
            new StartInductionRequest(CompanyId, TemplateId, Guid.NewGuid(), "M. Adeyemi")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();

        // Finalise before anything is done → 409 (MC-4).
        var early = await client.PostAsJsonAsync($"/api/inductions/sessions/{session!.Id}/finalize",
            new FinalizeInductionRequest("M. Adeyemi", true));
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);

        // Complete required steps.
        foreach (var step in session.Steps.Where(s => s.Required))
        {
            await client.PostAsync($"/api/inductions/sessions/{session.Id}/steps/{step.Id}/complete", null);
        }

        // Server-scored quiz.
        var quiz = await (await client.PostAsJsonAsync($"/api/inductions/sessions/{session.Id}/quiz",
            new SubmitQuizRequest(CorrectAnswers))).Content.ReadFromJsonAsync<QuizResultDto>();
        Assert.True(quiz!.Passed);

        // Finalise → passed with a completion reference (MC-5) and consent stored (MC-20).
        var done = await (await client.PostAsJsonAsync($"/api/inductions/sessions/{session.Id}/finalize",
            new FinalizeInductionRequest("M. Adeyemi", true))).Content.ReadFromJsonAsync<InductionSessionDto>();
        Assert.Equal("Passed", done!.Status);
        Assert.StartsWith("IND-", done.CompletionReference);

        var list = await client.GetFromJsonAsync<List<InductionSummaryDto>>($"/api/inductions/company/{CompanyId}");
        Assert.Contains(list!, s => s.Id == session.Id && s.ConsentGiven);
    }
}
