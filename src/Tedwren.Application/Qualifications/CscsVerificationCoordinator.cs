using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Qualifications;

/// <summary>The decision a CSCS check yields for a card, after applying the §8.1 rules.</summary>
public enum CscsCheckOutcome
{
    /// <summary>The company does not hold the CSCS module: no call is made and the card stays customer-checked (§8.1).</summary>
    NotEntitled,

    /// <summary>Verified live against CSCS — the highest assurance state.</summary>
    VerifiedLive,

    /// <summary>Recognised but expired: the induction is blocked (§8.1).</summary>
    ExpiredBlocksInduction,

    /// <summary>Not recognised: the induction may continue but site entry is blocked pending a manual check (§8.1).</summary>
    UnrecognisedBlocksEntry,

    /// <summary>CSCS was unreachable: fall back to a person checking the card; never blocks the induction (§8.1).</summary>
    HumanFallback,
}

/// <summary>The result of running the §8.1 decision rules over a CSCS lookup.</summary>
public sealed record CscsCheckResult(
    CscsCheckOutcome Outcome,
    CardVerificationState State,
    bool BlocksInduction,
    bool BlocksEntry,
    DateOnly? Expiry,
    string Message);

/// <summary>
/// Applies the PRD §8.1 decision rules over a raw CSCS lookup. It gates on the company holding the paid CSCS
/// module (Q2, fails closed), calls <see cref="ICscsVerificationService"/>, and maps the outcome to what the
/// induction and site-entry flows must do: verified live, expired (blocks the induction), unrecognised (blocks
/// entry pending a manual check) or unavailable (human fallback, never blocks). Encoding the rules here keeps
/// them testable now; the real CSCS client and the induction/gate wiring are the post-agreement Phase 1 build.
/// </summary>
public sealed class CscsVerificationCoordinator
{
    /// <summary>The catalogue key for the paid CSCS verification module.</summary>
    public const string ModuleKey = "cscs";

    private readonly ICscsVerificationService _verifier;
    private readonly IEntitlementService _entitlements;

    /// <summary>Creates the coordinator over the verifier and the entitlement service.</summary>
    public CscsVerificationCoordinator(ICscsVerificationService verifier, IEntitlementService entitlements)
    {
        _verifier = verifier;
        _entitlements = entitlements;
    }

    /// <summary>Runs the §8.1 decision rules for a card on behalf of a company.</summary>
    public async Task<CscsCheckResult> CheckAsync(Guid companyId, string cardNumber, string? scheme, CancellationToken cancellationToken = default)
    {
        // §8.1: without the module, no call is made and the product describes the card as checked by the customer.
        if (!await _entitlements.IsEnabledAsync(companyId, ModuleKey, cancellationToken))
        {
            return new CscsCheckResult(CscsCheckOutcome.NotEntitled, CardVerificationState.CustomerChecked,
                BlocksInduction: false, BlocksEntry: false, Expiry: null,
                "CSCS verification is not enabled — the card is recorded as checked by the customer.");
        }

        var result = await _verifier.VerifyAsync(cardNumber, scheme, cancellationToken);
        return result.Status switch
        {
            CscsVerificationStatus.Verified => new CscsCheckResult(CscsCheckOutcome.VerifiedLive,
                CardVerificationState.CscsVerified, BlocksInduction: false, BlocksEntry: false, result.ExpiryDate,
                "Verified live against CSCS."),

            // An expired card blocks the induction (§8.1) — and therefore entry.
            CscsVerificationStatus.Expired => new CscsCheckResult(CscsCheckOutcome.ExpiredBlocksInduction,
                CardVerificationState.ReadUnchecked, BlocksInduction: true, BlocksEntry: true, result.ExpiryDate,
                "The CSCS card has expired — the induction cannot continue."),

            // Unrecognised: the induction continues but entry is blocked pending a manual check (§8.1).
            CscsVerificationStatus.NotFound => new CscsCheckResult(CscsCheckOutcome.UnrecognisedBlocksEntry,
                CardVerificationState.ReadUnchecked, BlocksInduction: false, BlocksEntry: true, Expiry: null,
                "The CSCS card was not recognised — a manager must check it before site entry."),

            // Unavailable: fall back to a person checking the card; never blocks the induction (§8.1).
            _ => new CscsCheckResult(CscsCheckOutcome.HumanFallback, CardVerificationState.CustomerChecked,
                BlocksInduction: false, BlocksEntry: false, Expiry: null,
                "CSCS could not be reached — the card falls back to a manual check."),
        };
    }
}
