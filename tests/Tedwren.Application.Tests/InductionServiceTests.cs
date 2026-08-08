using System.Text.Json;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Application.Inductions;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the induction service (MC-1–MC-7, MC-20, R5): server-side quiz scoring with no answer leakage,
/// required-step + pass gating before completion, failed-attempt + manager reset, completion reference with
/// validity, separate consent, and re-induction supersedes.
/// </summary>
public sealed class InductionServiceTests
{
    private static readonly Guid Company = DefaultInductionTemplate.DemoCompanyId;
    private static readonly Guid Template = DefaultInductionTemplate.TemplateId;

    private static readonly IReadOnlyDictionary<string, int> CorrectAnswers = new Dictionary<string, int> { ["q1"] = 0, ["q2"] = 1, ["q3"] = 2 };
    private static readonly IReadOnlyDictionary<string, int> WrongAnswers = new Dictionary<string, int> { ["q1"] = 1, ["q2"] = 0, ["q3"] = 0 };

    private static InductionService CreateService()
    {
        var store = new InMemoryInductionStore();   // seeds the default template
        return new InductionService(new InMemoryInductionTemplateRepository(store), new InMemoryInductionSessionRepository(store));
    }

    private static Task<InductionSessionDto> StartAsync(InductionService service) =>
        service.StartAsync(new StartInductionRequest(Company, Template, Guid.NewGuid(), "M. Adeyemi"));

    [Fact] // R5 — the device session never carries the correct answers
    public async Task DeviceSession_NeverExposesQuizAnswers()
    {
        var service = CreateService();

        var session = await StartAsync(service);
        var json = JsonSerializer.Serialize(session);

        Assert.NotEmpty(session.Questions);
        Assert.DoesNotContain("CorrectOptionIndex", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correctoption", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // R5 + MC-6 — scoring happens on the server; a wrong submission fails and records the attempt
    public async Task Quiz_IsScoredServerSide_FailThenPass()
    {
        var service = CreateService();
        var session = await StartAsync(service);

        var failed = await service.SubmitQuizAsync(session.Id, new SubmitQuizRequest(WrongAnswers));
        Assert.False(failed!.Passed);
        Assert.Equal(1, failed.AttemptCount);

        var passed = await service.SubmitQuizAsync(session.Id, new SubmitQuizRequest(CorrectAnswers));
        Assert.True(passed!.Passed);
        Assert.Equal(3, passed.Correct);
        Assert.Equal(2, passed.AttemptCount);
    }

    [Fact] // MC-4 — cannot complete until required steps done and quiz passed
    public async Task Finalize_IsBlocked_UntilStepsDoneAndQuizPassed()
    {
        var service = CreateService();
        var session = await StartAsync(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinalizeAsync(session.Id, new FinalizeInductionRequest("M. Adeyemi", true)));

        // Pass the quiz but skip steps → still blocked.
        await service.SubmitQuizAsync(session.Id, new SubmitQuizRequest(CorrectAnswers));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinalizeAsync(session.Id, new FinalizeInductionRequest("M. Adeyemi", true)));
    }

    [Fact] // MC-5 + MC-7 + MC-20 — completion reference, validity and consent are recorded
    public async Task Finalize_WhenReady_IssuesReferenceValidityAndConsent()
    {
        var service = CreateService();
        var session = await StartAsync(service);
        foreach (var step in session.Steps.Where(s => s.Required))
        {
            await service.CompleteStepAsync(session.Id, step.Id);
        }

        await service.SubmitQuizAsync(session.Id, new SubmitQuizRequest(CorrectAnswers));
        var done = await service.FinalizeAsync(session.Id, new FinalizeInductionRequest("M. Adeyemi", ConsentGiven: true));

        Assert.Equal("Passed", done!.Status);
        Assert.StartsWith("IND-", done.CompletionReference);
        Assert.NotNull(done.ExpiresUtc);

        var summary = (await service.GetForCompanyAsync(Company)).Single(s => s.Id == session.Id);
        Assert.True(summary.ConsentGiven);
    }

    [Fact] // MC-6 — a manager reset clears the failed attempt for a retake
    public async Task Reset_ReturnsAFailedInductionToInProgress()
    {
        var service = CreateService();
        var session = await StartAsync(service);
        await service.SubmitQuizAsync(session.Id, new SubmitQuizRequest(WrongAnswers));

        var reset = await service.ResetAsync(session.Id, new ResetInductionRequest("Retake after coaching"));

        Assert.Equal("InProgress", reset!.Status);
    }

    [Fact] // MC-7 — re-induction supersedes the prior session
    public async Task Reinduction_SupersedesThePriorSession()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();
        var first = await service.StartAsync(new StartInductionRequest(Company, Template, personId, "M. Adeyemi"));

        await service.StartAsync(new StartInductionRequest(Company, Template, personId, "M. Adeyemi"));

        var priorSummary = (await service.GetForCompanyAsync(Company)).Single(s => s.Id == first.Id);
        Assert.Equal("Superseded", priorSummary.Status);
    }
}
