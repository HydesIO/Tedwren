using Tedwren.Abstractions.Contracts.Onboarding;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The self-service operative onboarding flow (SF-4, SUB-2): an administrator creates a link; the operative
/// opens it (token + optional passcode), submits their own details and uploads card photographs. The details
/// create a person/engagement (SF-1/SF-2) and the cards are captured unconfirmed for an administrator to
/// confirm later (SF-5/SF-6).
/// </summary>
public interface IOnboardingService
{
    /// <summary>Creates an onboarding link for a company and returns the token + (optional) passcode to share.</summary>
    Task<OnboardingLinkDto> CreateAsync(CreateOnboardingLinkRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns the operative-facing view for a token, or null when the link/passcode is invalid or expired.</summary>
    Task<OnboardingViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default);

    /// <summary>Submits the operative's details (creates/reuses person + engagement). Null when the link is invalid.</summary>
    Task<OnboardingViewDto?> SubmitDetailsAsync(string token, string? passcode, SubmitOnboardingDetailsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Captures a card (with optional photo) against the submitted operative. Null when invalid or details not yet submitted.</summary>
    Task<OnboardingViewDto?> CaptureCardAsync(string token, string? passcode, CaptureOnboardingCardRequest request, CancellationToken cancellationToken = default);
}
