using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Inductions;

/// <summary>
/// The digital-induction business service (MC-1–MC-7, MC-15, MC-20, R5). Runs the induction in a phone browser
/// and — crucially — <b>scores the quiz on the server</b>, so the correct answers never reach the device (R5).
/// It gates completion on the required steps and a passed quiz (MC-4/MC-5), records failed attempts with a
/// manager reset path (MC-6), issues a completion reference with configurable validity and supersedes the prior
/// induction on re-induction (MC-7), and captures a separate optional consent (MC-20). Store-agnostic.
/// </summary>
public sealed class InductionService : IInductionService
{
    private readonly IInductionTemplateRepository _templates;
    private readonly IInductionSessionRepository _sessions;

    /// <summary>Creates the service over its repositories.</summary>
    public InductionService(IInductionTemplateRepository templates, IInductionSessionRepository sessions)
    {
        _templates = templates;
        _sessions = sessions;
    }

    /// <summary>Lists a company's induction templates (MC-3).</summary>
    public async Task<IReadOnlyList<InductionTemplateDto>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var templates = await _templates.GetByCompanyAsync(companyId, cancellationToken);
        return templates.Select(ToTemplateDto).ToList();
    }

    /// <summary>Creates an induction template for a company, seeded from the shipped default (MC-3/SF-12), and returns its id.</summary>
    public async Task<Guid> CreateDefaultTemplateAsync(CreateInductionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A template name is required.", nameof(request));
        }

        // Clone the shipped default's steps and quiz into a new template owned by the company (R15). The
        // steps/questions are value records, so reusing the same lists is safe (they are immutable content).
        var template = new InductionTemplate
        {
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            ValidityDays = request.ValidityDays > 0 ? request.ValidityDays : DefaultInductionTemplate.Template.ValidityDays,
            PassMark = request.PassMark > 0 ? request.PassMark : DefaultInductionTemplate.Template.PassMark,
            Steps = DefaultInductionTemplate.Template.Steps.ToList(),
            Questions = DefaultInductionTemplate.Template.Questions.ToList(),
        };

        await _templates.AddAsync(template, cancellationToken);
        return template.Id;
    }

    /// <summary>Starts an induction, superseding the operative's prior induction for the same template (MC-1/MC-7).</summary>
    public async Task<InductionSessionDto> StartAsync(StartInductionRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(request.TemplateId, cancellationToken)
            ?? throw new InvalidOperationException("Induction template not found.");

        // MC-7: a re-induction supersedes the operative's previous induction for this template.
        var prior = await _sessions.GetLatestForPersonAsync(request.CompanyId, request.TemplateId, request.PersonId, cancellationToken);

        var session = new InductionSession
        {
            TemplateId = request.TemplateId,
            CompanyId = request.CompanyId,
            PersonId = request.PersonId,
            PersonName = request.PersonName,
        };
        await _sessions.AddAsync(session, cancellationToken);

        if (prior is not null)
        {
            prior.Status = InductionStatus.Superseded;
            prior.SupersededBySessionId = session.Id;
            await _sessions.UpdateAsync(prior, cancellationToken);
        }

        return ToSessionDto(session, template);
    }

    /// <summary>Returns the device-facing session (steps + quiz without answers), or null (R5).</summary>
    public async Task<InductionSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(session.TemplateId, cancellationToken);
        return template is null ? null : ToSessionDto(session, template);
    }

    /// <summary>Marks a step completed (MC-4).</summary>
    public async Task<InductionSessionDto?> CompleteStepAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(session.TemplateId, cancellationToken);
        if (template is null || template.Steps.All(s => s.Id != stepId))
        {
            return null;
        }

        if (!session.CompletedStepIds.Contains(stepId))
        {
            session.CompletedStepIds.Add(stepId);
            await _sessions.UpdateAsync(session, cancellationToken);
        }

        return ToSessionDto(session, template);
    }

    /// <summary>Scores a quiz submission on the server and records the attempt (R5, MC-6).</summary>
    public async Task<QuizResultDto?> SubmitQuizAsync(Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(session.TemplateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        // R5: the correct answers live in the template on the server; scoring happens here, not on the device.
        var result = InductionQuiz.Score(template.Questions, request.Answers, template.PassMark);
        session.AttemptCount++;
        session.LastScore = result.Correct;
        session.Status = result.Passed ? InductionStatus.InProgress : InductionStatus.Failed;
        await _sessions.UpdateAsync(session, cancellationToken);

        return new QuizResultDto(result.Correct, result.Total, result.Passed, session.AttemptCount);
    }

    /// <summary>Finalises the induction once required steps are done and the quiz passed (MC-4/MC-5/MC-20).</summary>
    public async Task<InductionSessionDto?> FinalizeAsync(Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(session.TemplateId, cancellationToken)
            ?? throw new InvalidOperationException("Induction template not found.");

        // MC-4: cannot complete until every required step is done and the quiz has been passed.
        var requiredDone = template.Steps.Where(s => s.Required).All(s => session.CompletedStepIds.Contains(s.Id));
        var quizPassed = session.LastScore is { } score && score >= template.PassMark;
        if (!requiredDone || !quizPassed)
        {
            throw new InvalidOperationException("The induction cannot be completed until all required steps are done and the quiz is passed.");
        }

        session.Status = InductionStatus.Passed;
        session.SignatureName = request.SignatureName;
        session.ConsentGiven = request.ConsentGiven;   // MC-20: stored exactly as given (separate, optional)
        session.CompletedUtc = DateTimeOffset.UtcNow;
        session.CompletionReference = "IND-" + session.Id.ToString("N")[..8].ToUpperInvariant();
        session.ExpiresUtc = session.CompletedUtc.Value.AddDays(template.ValidityDays);
        await _sessions.UpdateAsync(session, cancellationToken);

        return ToSessionDto(session, template);
    }

    /// <summary>Resets a failed induction so it can be retaken, recording the reason (MC-6).</summary>
    public async Task<InductionSessionDto?> ResetAsync(Guid sessionId, ResetInductionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(session.TemplateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        session.Status = InductionStatus.InProgress;
        session.LastScore = null;
        session.ResetReason = request.Reason;
        await _sessions.UpdateAsync(session, cancellationToken);

        return ToSessionDto(session, template);
    }

    /// <summary>Lists a company's induction sessions for the console (MC-15).</summary>
    public async Task<IReadOnlyList<InductionSummaryDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessions.GetByCompanyAsync(companyId, cancellationToken);
        var templates = (await _templates.GetByCompanyAsync(companyId, cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        return sessions.Select(s => new InductionSummaryDto(
            s.Id, s.PersonId, s.PersonName,
            templates.TryGetValue(s.TemplateId, out var name) ? name : "Induction",
            s.Status.ToString(), s.CompletionReference, s.CompletedUtc, s.ExpiresUtc, s.ConsentGiven, s.AttemptCount)).ToList();
    }

    /// <summary>Maps a template to its management DTO (never exposes quiz answers, R5).</summary>
    private static InductionTemplateDto ToTemplateDto(InductionTemplate t) => new(
        t.Id, t.Name, t.ValidityDays, t.PassMark,
        t.Steps.Select(ToStepDto).ToList(), t.Questions.Count);

    /// <summary>Maps a session to the device-facing DTO — quiz questions carry no correct answers (R5).</summary>
    private static InductionSessionDto ToSessionDto(InductionSession s, InductionTemplate t) => new(
        s.Id, t.Id, t.Name, s.PersonId, s.PersonName, s.Status.ToString(),
        t.Steps.Select(ToStepDto).ToList(),
        s.CompletedStepIds.ToList(),
        t.Questions.Select(q => new InductionQuizQuestionDto(q.Id, q.Prompt, q.Options)).ToList(),
        t.PassMark, s.CompletionReference, s.ExpiresUtc, s.AttemptCount);

    /// <summary>Maps a step to its DTO.</summary>
    private static InductionStepDto ToStepDto(InductionStep s) => new(s.Id, s.Kind.ToString(), s.Label, s.Required);
}
