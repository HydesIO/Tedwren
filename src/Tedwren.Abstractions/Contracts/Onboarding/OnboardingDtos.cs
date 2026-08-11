namespace Tedwren.Abstractions.Contracts.Onboarding;

/// <summary>Admin request to create a self-service onboarding link (SF-4, SUB-2).</summary>
public sealed record CreateOnboardingLinkRequest(Guid CompanyId, string? Name, string? Trade, bool RequirePasscode);

/// <summary>
/// The created link as the admin needs to share it: the token, the plaintext passcode (shown once, if one
/// was required) and the expiry. The admin sends these to the operative.
/// </summary>
public sealed record OnboardingLinkDto(string Token, string? Passcode, DateTimeOffset ExpiresUtc);

/// <summary>What the operative sees on the onboarding page: who invited them, prefilled fields and progress.</summary>
public sealed record OnboardingViewDto(
    string CompanyName,
    string? Name,
    string? Trade,
    string Status,
    bool DetailsSubmitted,
    IReadOnlyList<OnboardingCardDto> Cards);

/// <summary>A card the operative has uploaded via the link (unconfirmed until an admin confirms it, SF-6).</summary>
public sealed record OnboardingCardDto(string QualificationName, string? CardNumber, DateOnly? ExpiresOn);

/// <summary>The operative's own details, submitted from the link (creates person/engagement, SF-1/SF-2).</summary>
public sealed record SubmitOnboardingDetailsRequest(string Name, string MobileNumber, string? Trade);

/// <summary>A card the operative captures via the link — number/expiry plus an optional base64 photo (SF-5, R9).</summary>
public sealed record CaptureOnboardingCardRequest(
    Guid QualificationTypeId,
    string? CardNumber,
    string? HolderName,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    string? ImageBase64,
    string? ImageContentType);
