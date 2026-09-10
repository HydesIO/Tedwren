namespace Tedwren.Abstractions.Contracts.Trades;

/// <summary>
/// Admin request to invite a trade (subcontractor) to onboard themselves (UAT-023, SUB-4/MC-27). Creating the
/// invite also creates the trade company record; the trade then uploads their own documents from the link.
/// </summary>
public sealed record CreateTradeInviteRequest(
    string CompanyName,
    string? Type,
    string? Trade,
    string? ContactName,
    string? ContactEmail,
    bool RequirePasscode);

/// <summary>
/// A created trade invite as the admin needs to share it: the token, the plaintext passcode (shown once, if one
/// was required) and the expiry. The admin sends these to the trade (real email delivery is PRD-Phase 7).
/// </summary>
public sealed record TradeInviteLinkDto(string Token, string? Passcode, DateTimeOffset ExpiresUtc);

/// <summary>A document the trade has uploaded (or an admin recorded) against the company (UAT-023).</summary>
public sealed record TradeDocumentDto(string Name, string Type, DateOnly? ExpiresOn, bool HasFile, string? FileReference);

/// <summary>
/// What the trade sees when they open their onboarding link: who invited them, the review status (with the
/// manager's note if it was returned), the documents they've uploaded so far, and the document types requested.
/// </summary>
public sealed record TradeInviteViewDto(
    string CompanyName,
    string Status,
    string? ReviewNote,
    IReadOnlyList<TradeDocumentDto> Documents,
    IReadOnlyList<string> RequestedDocumentTypes);

/// <summary>
/// A document the trade uploads via the link (UAT-023): the category/name/expiry plus an optional base64 file
/// (its RAMS/insurance/accreditation PDF, stored privately, R9).
/// </summary>
public sealed record SubmitTradeDocumentRequest(
    string Type,
    string Name,
    DateOnly? ExpiresOn,
    string? Reference,
    string? FileBase64,
    string? FileContentType);

/// <summary>A manager's review decision note (required when rejecting or returning) (UAT-023).</summary>
public sealed record ReviewTradeRequest(string? Note);

/// <summary>A trade submission awaiting (or having had) manager review — the review-queue row (UAT-023).</summary>
public sealed record TradeReviewItemDto(
    Guid InviteId,
    Guid CompanyId,
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string Status,
    DateTimeOffset? SubmittedUtc,
    string? ReviewNote,
    IReadOnlyList<TradeDocumentDto> Documents);
