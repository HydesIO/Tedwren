using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IInductionService"/> for client <c>DataSource=Mock</c>. It holds a private template (whose quiz
/// answers are never exposed in any DTO, R5) and runs the whole flow in-proc, so start → steps → server-scored
/// quiz → finalise, plus the console records view and manager reset (MC-6), are demonstrable without a backend.
/// Ids match the API mock seed so the screen is identical across the switch.
/// </summary>
public sealed class ClientMockInductionService : IInductionService
{
    /// <summary>The demo company (matches the API mock seed).</summary>
    public static readonly Guid DemoCompanyId = Guid.Parse("66666666-6666-4666-8666-000000000001");

    /// <summary>The default template id (matches the API mock seed).</summary>
    public static readonly Guid TemplateId = Guid.Parse("66666666-6666-4666-8666-000000000010");

    private const int PassMark = 3;
    private const int ValidityDays = 365;

    private static readonly IReadOnlyList<InductionStepDto> Steps = new[]
    {
        new InductionStepDto("identity", "Identity", "Confirm your identity", true),
        new InductionStepDto("emergency", "EmergencyContact", "Add an emergency contact", true),
        new InductionStepDto("declaration", "Declaration", "Read and accept the site rules", true),
        new InductionStepDto("video", "Media", "Watch the safety induction video", true),
    };

    private static readonly IReadOnlyList<InductionQuizQuestionDto> Questions = new[]
    {
        new InductionQuizQuestionDto("q1", "What must you wear on site at all times?", new[] { "Hi-vis, hard hat and boots", "Only a hard hat", "Nothing special" }),
        new InductionQuizQuestionDto("q2", "What do you do if you hear the evacuation alarm?", new[] { "Finish your task first", "Go to the muster point", "Hide on site" }),
        new InductionQuizQuestionDto("q3", "Who do you report a near-miss to?", new[] { "No one", "A friend", "Your site supervisor" }),
    };

    // R5: the correct answers stay here (the mock plays the server) and never appear in any DTO.
    private static readonly IReadOnlyDictionary<string, int> Answers = new Dictionary<string, int> { ["q1"] = 0, ["q2"] = 1, ["q3"] = 2 };

    private readonly Dictionary<Guid, Session> _sessions = new();

    /// <summary>Creates the service and seeds a passed and a failed session for the records view.</summary>
    public ClientMockInductionService()
    {
        var passed = new Session(Guid.NewGuid(), Guid.NewGuid(), "M. Adeyemi")
        {
            Status = "Passed", CompletedStepIds = Steps.Select(s => s.Id).ToList(), LastScore = 3, AttemptCount = 1,
            CompletionReference = "IND-A1B2C3D4", CompletedUtc = DateTimeOffset.UtcNow.AddDays(-10),
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(355), ConsentGiven = true,
        };
        var failed = new Session(Guid.NewGuid(), Guid.NewGuid(), "J. Fletcher")
        {
            Status = "Failed", CompletedStepIds = Steps.Select(s => s.Id).ToList(), LastScore = 1, AttemptCount = 2,
        };
        _sessions[passed.Id] = passed;
        _sessions[failed.Id] = failed;
    }

    /// <summary>Returns the single default template (MC-3).</summary>
    public Task<IReadOnlyList<InductionTemplateDto>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InductionTemplateDto>>(new[]
        {
            new InductionTemplateDto(TemplateId, "General Site Induction", ValidityDays, PassMark, Steps, Questions.Count),
        });

    /// <summary>Demo create — sample data is static, so nothing is persisted (MC-3/SF-12).</summary>
    public Task<Guid> CreateDefaultTemplateAsync(CreateInductionTemplateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    /// <summary>Starts an induction (MC-1).</summary>
    public Task<InductionSessionDto> StartAsync(StartInductionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new Session(Guid.NewGuid(), request.PersonId, request.PersonName);
        _sessions[session.Id] = session;
        return Task.FromResult(ToDto(session));
    }

    /// <summary>Returns the device-facing session (no answers, R5).</summary>
    public Task<InductionSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? ToDto(s) : null);

    /// <summary>Marks a step completed (MC-4).</summary>
    public Task<InductionSessionDto?> CompleteStepAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var s) || Steps.All(x => x.Id != stepId))
        {
            return Task.FromResult<InductionSessionDto?>(null);
        }

        if (!s.CompletedStepIds.Contains(stepId))
        {
            s.CompletedStepIds.Add(stepId);
        }

        return Task.FromResult<InductionSessionDto?>(ToDto(s));
    }

    /// <summary>Scores the quiz here (playing the server) — answers are never sent to the device (R5).</summary>
    public Task<QuizResultDto?> SubmitQuizAsync(Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var s))
        {
            return Task.FromResult<QuizResultDto?>(null);
        }

        var correct = Questions.Count(q => request.Answers.TryGetValue(q.Id, out var a) && a == Answers[q.Id]);
        var passed = correct >= PassMark;
        s.AttemptCount++;
        s.LastScore = correct;
        s.Status = passed ? "InProgress" : "Failed";
        return Task.FromResult<QuizResultDto?>(new QuizResultDto(correct, Questions.Count, passed, s.AttemptCount));
    }

    /// <summary>Finalises the induction once required steps are done and the quiz passed (MC-4/MC-5/MC-20).</summary>
    public Task<InductionSessionDto?> FinalizeAsync(Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var s))
        {
            return Task.FromResult<InductionSessionDto?>(null);
        }

        var requiredDone = Steps.Where(x => x.Required).All(x => s.CompletedStepIds.Contains(x.Id));
        if (!requiredDone || s.LastScore is not { } score || score < PassMark)
        {
            return Task.FromResult<InductionSessionDto?>(null);   // not ready (the API surfaces this as 409)
        }

        s.Status = "Passed";
        s.ConsentGiven = request.ConsentGiven;
        s.CompletedUtc = DateTimeOffset.UtcNow;
        s.CompletionReference = "IND-" + s.Id.ToString("N")[..8].ToUpperInvariant();
        s.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(ValidityDays);
        return Task.FromResult<InductionSessionDto?>(ToDto(s));
    }

    /// <summary>Resets a failed induction with a reason (MC-6).</summary>
    public Task<InductionSessionDto?> ResetAsync(Guid sessionId, ResetInductionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var s))
        {
            return Task.FromResult<InductionSessionDto?>(null);
        }

        s.Status = "InProgress";
        s.LastScore = null;
        return Task.FromResult<InductionSessionDto?>(ToDto(s));
    }

    /// <summary>Lists the company's sessions for the console (MC-15).</summary>
    public Task<IReadOnlyList<InductionSummaryDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InductionSummaryDto> items = _sessions.Values
            .OrderByDescending(s => s.CompletedUtc ?? DateTimeOffset.MinValue)
            .Select(s => new InductionSummaryDto(s.Id, s.PersonId, s.PersonName, "General Site Induction", s.Status,
                s.CompletionReference, s.CompletedUtc, s.ExpiresUtc, s.ConsentGiven, s.AttemptCount))
            .ToList();
        return Task.FromResult(items);
    }

    private InductionSessionDto ToDto(Session s) => new(
        s.Id, TemplateId, "General Site Induction", s.PersonId, s.PersonName, s.Status,
        Steps, s.CompletedStepIds.ToList(), Questions, PassMark, s.CompletionReference, s.ExpiresUtc, s.AttemptCount);

    /// <summary>Internal mutable session state for the mock.</summary>
    private sealed class Session
    {
        public Session(Guid id, Guid personId, string personName)
        {
            Id = id; PersonId = personId; PersonName = personName;
        }

        public Guid Id { get; }
        public Guid PersonId { get; }
        public string PersonName { get; }
        public string Status { get; set; } = "InProgress";
        public List<string> CompletedStepIds { get; set; } = new();
        public int? LastScore { get; set; }
        public int AttemptCount { get; set; }
        public string? CompletionReference { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public DateTimeOffset? ExpiresUtc { get; set; }
        public bool ConsentGiven { get; set; }
    }
}
