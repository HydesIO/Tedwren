namespace Tedwren.Abstractions.Contracts.Account;

/// <summary>
/// The signed-in user's own account as the self-service Profile page needs it (SF-20). Sourced from the
/// authenticated caller server-side — never from a client-supplied id — so a user only ever sees and edits
/// their own record (R15). <see cref="AvatarUrl"/> resolves to the authorised <c>/api/images/{ref}</c> address
/// (or null); <see cref="IsAdministrator"/> gates the Administrator-only company-editing section.
/// </summary>
public sealed record MyProfileDto(
    string Name,
    string Email,
    string? Mobile,
    string Role,
    string RoleLabel,
    Guid CompanyId,
    string? CompanyName,
    string? AvatarUrl,
    bool IsAdministrator);

/// <summary>Request to update the caller's own personal details. Role and status are never self-editable (R15).</summary>
public sealed record UpdateMyProfileRequest(string Name, string Email, string? Mobile);

/// <summary>
/// Request to change the caller's own password. The current password is verified before the new one is
/// stored, so a hijacked but unauthenticated session cannot silently reset it.
/// </summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// Request to set the caller's avatar image, mirroring the onboarding card-capture shape: base64 bytes plus
/// their content type. The bytes are stored via <c>IImageStore</c> and served only through the authorised
/// image endpoint (R9).
/// </summary>
public sealed record UpdateAvatarRequest(string ImageBase64, string? ContentType);
