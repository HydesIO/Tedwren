using Tedwren.Abstractions.Contracts.Account;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The signed-in user's self-service account service (SF-20): read and edit your own details, change your own
/// password and set your own avatar. Every operation resolves the caller from the authenticated request and
/// acts only on that user's own record — never a client-supplied id — and can never change the caller's role
/// or status (R15).
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the caller's own profile, or null when there is no authenticated user.</summary>
    Task<MyProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the caller's own name, email and mobile. Returns the refreshed profile, or null when there is no
    /// authenticated user. Throws <see cref="ArgumentException"/> for invalid input (empty name, bad email).
    /// </summary>
    Task<MyProfileDto?> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the caller's own password after verifying the current one. Returns false when the current
    /// password is wrong or there is no authenticated user. Throws <see cref="ArgumentException"/> when the new
    /// password fails the minimum-strength check.
    /// </summary>
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new avatar image for the caller and returns the refreshed profile (with the resolved avatar
    /// URL), or null when there is no authenticated user. Throws <see cref="ArgumentException"/> for invalid
    /// image data.
    /// </summary>
    Task<MyProfileDto?> UpdateAvatarAsync(UpdateAvatarRequest request, CancellationToken cancellationToken = default);
}
