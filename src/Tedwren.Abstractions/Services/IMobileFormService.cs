using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Server-only composition for the operative (mobile) forms surface (M6): the forms assigned to one operative,
/// resolved to their latest published template version. Keyed by ids from the operative token (never client-supplied),
/// so an operative only ever sees their own company's assignments (R15). Kept separate from the console
/// <c>IFormAssignmentService</c> (which returns the whole company and is client-implemented) so that interface stays
/// cohesive (SRP). Template fetch + submission reuse the existing <c>IFormTemplateService</c>/<c>IFormSubmissionService</c>.
/// </summary>
public interface IMobileFormService
{
    /// <summary>The forms assigned to this operative — organisation-wide, operator-scoped to them, or at a site they attend.</summary>
    Task<IReadOnlyList<MobileFormAssignmentDto>> GetAssignmentsAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default);
}
