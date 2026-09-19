using Tedwren.Abstractions.Contracts.Qualifications;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Live CSCS Smart Check card verification (PRD-Phase 1). A provider interface so the real integration slots in
/// behind it once the CSCS commercial agreement — the project's longest external lead time (§11) — is in place.
/// Until then the default implementation reports the service as unavailable, and callers fall back to a person
/// checking the card (§8.1: an unavailable third-party service must never stop a worker completing an induction).
/// </summary>
public interface ICscsVerificationService
{
    /// <summary>Verifies a card number (and optional scheme) against CSCS Smart Check.</summary>
    Task<CscsVerificationResult> VerifyAsync(string cardNumber, string? scheme, CancellationToken cancellationToken = default);
}
