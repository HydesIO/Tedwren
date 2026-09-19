namespace Tedwren.Abstractions.Contracts.Qualifications;

/// <summary>The raw outcome of a live CSCS Smart Check lookup for a single card (PRD-Phase 1, §8.1).</summary>
public enum CscsVerificationStatus
{
    /// <summary>The card is recognised and currently valid.</summary>
    Verified = 0,

    /// <summary>The card is recognised but has expired.</summary>
    Expired = 1,

    /// <summary>The card number is not recognised by CSCS.</summary>
    NotFound = 2,

    /// <summary>CSCS could not be reached — the caller falls back to a person checking the card (§8.1).</summary>
    Unavailable = 3,
}

/// <summary>The raw result of a CSCS Smart Check lookup, before the §8.1 decision rules are applied.</summary>
public sealed record CscsVerificationResult(CscsVerificationStatus Status, DateOnly? ExpiryDate = null, string? Message = null);

/// <summary>Request to run a CSCS check for a card on behalf of the caller's company.</summary>
public sealed record CscsCheckRequest(string CardNumber, string? Scheme);

/// <summary>
/// The wire result of a CSCS check after the §8.1 rules: the decision (<paramref name="Outcome"/>), the resulting
/// card verification state, whether it blocks the induction or site entry, an optional expiry, and a message.
/// Enums are carried as their names, matching the app's DTO convention.
/// </summary>
public sealed record CscsCheckResponse(
    string Outcome,
    string State,
    bool BlocksInduction,
    bool BlocksEntry,
    DateOnly? Expiry,
    string Message);
