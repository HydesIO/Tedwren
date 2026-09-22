using Tedwren.Abstractions.Contracts.Rams;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The RAMS submission &amp; approval workflow (PRD §8.2): a contractor submits (with a reference), the site
/// manager reviews the queue and approves / rejects / returns with a written reason, and a resubmission is a new
/// version leaving earlier versions intact. Scoped to the reviewing company (R15); part of the paid HSE module.
/// </summary>
public interface IRamsService
{
    /// <summary>Submits a RAMS (or a new version when the request carries a family id) and returns it with its reference.</summary>
    Task<RamsSubmissionDto> SubmitAsync(Guid companyId, SubmitRamsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's RAMS submissions, newest first.</summary>
    Task<IReadOnlyList<RamsSubmissionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the submissions awaiting review (oldest first), each flagged when awaiting more than 48 hours.</summary>
    Task<IReadOnlyList<RamsSubmissionDto>> GetReviewQueueAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Approves a submitted RAMS and makes it the live version (spec Stage 3). Returns false when it is missing/cross-tenant; throws if it is not awaiting review.</summary>
    Task<bool> ApproveAsync(Guid companyId, Guid id, string reviewer, CancellationToken cancellationToken = default);

    /// <summary>Approves a submitted RAMS with reviewer comments (a required note) and makes it the live version (spec Stage 3). Returns false when it is missing/cross-tenant.</summary>
    Task<bool> ApproveWithCommentsAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default);

    /// <summary>Rejects a submitted RAMS with a required note. Returns false when it is missing/cross-tenant.</summary>
    Task<bool> RejectAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default);

    /// <summary>Returns a submitted RAMS for changes with a required note. Returns false when it is missing/cross-tenant.</summary>
    Task<bool> ReturnAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a RAMS from an already-stored document (spec Stage 2→3): the subcontractor's uploaded RAMS is
    /// bridged into the review queue, reusing the stored file reference. A supplied family id seeds the family on
    /// the first upload and resubmits into it (next version) thereafter. Scoped to the reviewing company (R15).
    /// </summary>
    Task<RamsSubmissionDto> RegisterFromDocumentAsync(Guid companyId, RegisterRamsFromDocumentRequest request, CancellationToken cancellationToken = default);
}
