using Tedwren.Abstractions.Contracts.Trades;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The trade self-service onboarding workflow (UAT-023, SUB-4/MC-27): a main contractor invites a trade
/// (subcontractor), which uploads its own registration/RAMS/insurance/accreditations from a tokenised link
/// without a console account; a manager then approves, rejects or returns the submission. Company reads/writes
/// are tenant-scoped (R15); the manual add-company path is unaffected.
/// </summary>
public interface ITradeOnboardingService
{
    /// <summary>Creates the trade company and its invitation link, returning the token + (optional) passcode to share.</summary>
    Task<TradeInviteLinkDto> InviteTradeAsync(CreateTradeInviteRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns the trade-facing view for a token, or null when the link/passcode is invalid or expired.</summary>
    Task<TradeInviteViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default);

    /// <summary>Uploads a document (optionally with a file) against the invited trade. Null when the link is invalid.</summary>
    Task<TradeInviteViewDto?> SubmitDocumentAsync(string token, string? passcode, SubmitTradeDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Submits the trade's documents for manager review (Invited/Returned → Submitted). Null when the link is invalid; throws when not submittable.</summary>
    Task<TradeInviteViewDto?> SubmitForReviewAsync(string token, string? passcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an operative to the invited subcontractor from the link (spec Stage 2). Null when the link is invalid;
    /// throws when the invite has no onboarding configuration, when Gate 1 has not cleared (fail-closed, R2), or
    /// when the operative cannot be added (SF-2). Returns the refreshed view on success.
    /// </summary>
    Task<TradeInviteViewDto?> AddOperativeByLinkAsync(string token, string? passcode, AddTradeOperativeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists the trade submissions for the caller's tenant that a manager should review (MC-27, R15).</summary>
    Task<IReadOnlyList<TradeReviewItemDto>> GetReviewQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>Approves a submission awaiting review. Null when not found/out of scope; throws when not in a reviewable state.</summary>
    Task<TradeReviewItemDto?> ApproveAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rejects a submission awaiting review (a note is required). Null when not found/out of scope; throws when not reviewable or the note is missing.</summary>
    Task<TradeReviewItemDto?> RejectAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a submission to the trade for changes (a note is required). Null when not found/out of scope; throws when not reviewable or the note is missing.</summary>
    Task<TradeReviewItemDto?> ReturnAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default);
}
