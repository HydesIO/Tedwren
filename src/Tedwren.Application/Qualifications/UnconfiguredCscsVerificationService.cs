using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;

namespace Tedwren.Application.Qualifications;

/// <summary>
/// Default <see cref="ICscsVerificationService"/> used until the CSCS Smart Check integration is configured
/// (PRD-Phase 1). It reports every lookup as <see cref="CscsVerificationStatus.Unavailable"/>, so the
/// verification coordinator falls back to a person checking the card (§8.1) rather than blocking anyone. The real
/// HTTP client replaces this in the composition root once the CSCS agreement is in place — mirroring the
/// <c>UnconfiguredGoCardlessClient</c> default on the billing side.
/// </summary>
public sealed class UnconfiguredCscsVerificationService : ICscsVerificationService
{
    /// <summary>Reports the service as unavailable so callers use the human-check fallback (§8.1).</summary>
    public Task<CscsVerificationResult> VerifyAsync(string cardNumber, string? scheme, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CscsVerificationResult(CscsVerificationStatus.Unavailable, null, "CSCS verification is not configured."));
}
