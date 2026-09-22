using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Inductions;

/// <summary>
/// The operative-facing induction facade for the mobile app (Subcontractor Onboarding spec Stage 4 / Gate 4). It
/// resolves <b>which</b> induction an operative must complete — always the main contractor's own template (§6.1):
/// the one their subcontractor was configured with, or the operative's own company template as a fallback — and
/// then drives the shared <see cref="IInductionService"/> take-flow. Every mutating call is guarded so an operative
/// can only act on their own session (R15); the person id comes from the authenticated token, never the request.
/// </summary>
public sealed class OperativeInductionService
{
    private readonly IInductionService _inductions;
    private readonly ISubcontractorOnboardingConfigRepository _configs;

    /// <summary>Creates the facade over the induction service and the subcontractor onboarding-config repository.</summary>
    public OperativeInductionService(IInductionService inductions, ISubcontractorOnboardingConfigRepository configs)
    {
        _inductions = inductions;
        _configs = configs;
    }

    /// <summary>Returns (resuming or starting) the operative's current induction session, or null when no induction is configured for them.</summary>
    public async Task<InductionSessionDto?> GetCurrentAsync(Guid operativeCompanyId, Guid personId, string personName, CancellationToken cancellationToken = default)
    {
        var (templateId, inductionCompanyId) = await ResolveTemplateAsync(operativeCompanyId, cancellationToken);
        if (templateId is null || inductionCompanyId is null)
        {
            return null;
        }

        return await _inductions.GetOrStartForPersonAsync(inductionCompanyId.Value, templateId.Value, personId, personName, cancellationToken);
    }

    /// <summary>Marks a step complete on the operative's own session (R15). Null when the session is missing or another person's.</summary>
    public async Task<InductionSessionDto?> CompleteStepAsync(Guid personId, Guid sessionId, string stepId, CancellationToken cancellationToken = default) =>
        await OwnsAsync(personId, sessionId, cancellationToken)
            ? await _inductions.CompleteStepAsync(sessionId, stepId, cancellationToken)
            : null;

    /// <summary>Scores a quiz submission for the operative's own session (server-side, R5). Null when the session is missing or another person's.</summary>
    public async Task<QuizResultDto?> SubmitQuizAsync(Guid personId, Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default) =>
        await OwnsAsync(personId, sessionId, cancellationToken)
            ? await _inductions.SubmitQuizAsync(sessionId, request, cancellationToken)
            : null;

    /// <summary>Finalises the operative's own induction (signature + consent). Null when the session is missing or another person's; throws when not ready.</summary>
    public async Task<InductionSessionDto?> FinalizeAsync(Guid personId, Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default) =>
        await OwnsAsync(personId, sessionId, cancellationToken)
            ? await _inductions.FinalizeAsync(sessionId, request, cancellationToken)
            : null;

    /// <summary>
    /// Resolves the induction template the operative must complete and the company that owns it (§6.1). Primary:
    /// the induction their subcontractor was configured with. Fallback: the operative's own company template (a
    /// direct main-contractor operative). Returns (null, null) when no induction is configured for them yet.
    /// </summary>
    private async Task<(Guid? templateId, Guid? companyId)> ResolveTemplateAsync(Guid operativeCompanyId, CancellationToken cancellationToken)
    {
        var config = await _configs.GetBySubcontractorCompanyAsync(operativeCompanyId, cancellationToken);
        if (config?.InductionTemplateId is { } configured)
        {
            return (configured, config.InviterCompanyId);
        }

        var own = await _inductions.GetTemplatesAsync(operativeCompanyId, cancellationToken);
        return own.Count > 0 ? (own[0].Id, operativeCompanyId) : (null, null);
    }

    /// <summary>Whether the session exists and belongs to the given operative (R15).</summary>
    private async Task<bool> OwnsAsync(Guid personId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _inductions.GetSessionAsync(sessionId, cancellationToken);
        return session is not null && session.PersonId == personId;
    }
}
