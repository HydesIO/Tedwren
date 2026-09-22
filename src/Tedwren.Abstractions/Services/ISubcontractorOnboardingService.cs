using Tedwren.Abstractions.Contracts.Subcontractors;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The main-contractor subcontractor set-up & configuration flow (Subcontractor Onboarding spec Stage 1 / §4).
/// Creates the subcontractor company + a tokenised onboarding invite and records the configuration (required
/// documents with their "required before work" flags, access period, SSSTS/SMSTS, induction settings, RAMS
/// review cycle). It reuses the trade-onboarding invite so the subcontractor uploads its documents from the same
/// link and lands in the existing review queue. All reads/writes are tenant-scoped to the inviting main
/// contractor (R15).
/// </summary>
public interface ISubcontractorOnboardingService
{
    /// <summary>Sets up & configures a subcontractor, returning the shareable onboarding link and the created ids.</summary>
    Task<SubcontractorOnboardingResultDto> SetupAsync(SetupSubcontractorRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the stored configuration for a subcontractor company (own tenant only), or null when none exists.</summary>
    Task<SubcontractorOnboardingConfigDto?> GetBySubcontractorAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates Gate 1 for a subcontractor (spec §2): whether every "required before work" document heading is
    /// present and valid. Scoped to the inviting tenant (R15); a company with no configuration clears vacuously.
    /// </summary>
    Task<Gate1StatusDto> EvaluateGate1Async(Guid subcontractorCompanyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the caller's subcontractors whose live RAMS is due for re-review under its configured cycle, as of
    /// <paramref name="asOf"/> (spec §4; beyond PRD v6.4 — informational, never expires an approval). Scoped to
    /// the inviting tenant (R15).
    /// </summary>
    Task<IReadOnlyList<RamsReviewDueDto>> GetSubcontractorsDueForRamsReviewAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
}
