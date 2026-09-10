using Tedwren.Abstractions.Contracts.Account;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Account;

/// <summary>
/// Self-service account service (SF-20). Resolves the caller from <see cref="ICurrentUserService"/> and acts
/// only on that user's own record — a client-supplied id is never trusted — so a user can read and edit their
/// own details, change their own password and set their own avatar, but never their role or status (R15).
/// </summary>
public sealed class ProfileService : IProfileService
{
    /// <summary>Minimum password length, matching the invite-acceptance rule in <see cref="AuthService"/>.</summary>
    private const int MinimumPasswordLength = 8;

    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly IImageStore _images;

    /// <summary>Creates the service over the current-user resolver, user/company repositories and image store.</summary>
    public ProfileService(
        ICurrentUserService currentUser,
        IUserRepository users,
        ICompanyRepository companies,
        IImageStore images)
    {
        _currentUser = currentUser;
        _users = users;
        _companies = companies;
        _images = images;
    }

    /// <summary>Returns the caller's own profile, or null when there is no authenticated user.</summary>
    public async Task<MyProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await ResolveCallerAsync(cancellationToken);
        return user is null ? null : await ToDtoAsync(user, cancellationToken);
    }

    /// <summary>Updates the caller's own name, email and mobile, then returns the refreshed profile.</summary>
    public async Task<MyProfileDto?> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await ResolveCallerAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        var email = (request.Email ?? string.Empty).Trim();
        if (!EmailValidation.IsValid(email))
        {
            throw new ArgumentException("A valid email address is required.", nameof(request));
        }

        // Email is the sign-in identity: allow the change but never let it collide with another account's email.
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _users.GetByEmailAsync(email, cancellationToken);
            if (existing is not null && existing.Id != user.Id)
            {
                throw new ArgumentException("That email address is already in use.", nameof(request));
            }
        }

        user.Name = name;
        user.Email = email;
        user.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        user.LastActiveUtc = DateTimeOffset.UtcNow;
        await _users.UpdateProfileAsync(user, cancellationToken);

        return await ToDtoAsync(user, cancellationToken);
    }

    /// <summary>Changes the caller's own password after verifying the current one. False when the current password is wrong.</summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await ResolveCallerAsync(cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (!PasswordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            return false;
        }

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < MinimumPasswordLength)
        {
            throw new ArgumentException($"The new password must be at least {MinimumPasswordLength} characters.", nameof(request));
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.PasswordSetUtc = DateTimeOffset.UtcNow;
        user.LastActiveUtc = DateTimeOffset.UtcNow;
        await _users.UpdateProfileAsync(user, cancellationToken);

        return true;
    }

    /// <summary>Stores a new avatar image for the caller and returns the refreshed profile.</summary>
    public async Task<MyProfileDto?> UpdateAvatarAsync(UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        var user = await ResolveCallerAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            throw new ArgumentException("An image is required.", nameof(request));
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("The image data is not valid base64.", nameof(request));
        }

        if (bytes.Length == 0)
        {
            throw new ArgumentException("An image is required.", nameof(request));
        }

        var reference = await _images.SaveAsync(bytes, request.ContentType ?? "image/jpeg", cancellationToken);
        user.AvatarImageReference = reference;
        user.LastActiveUtc = DateTimeOffset.UtcNow;
        await _users.UpdateProfileAsync(user, cancellationToken);

        return await ToDtoAsync(user, cancellationToken);
    }

    /// <summary>Loads the authenticated caller's own <see cref="User"/> record, or null when unauthenticated/unknown.</summary>
    private async Task<User?> ResolveCallerAsync(CancellationToken cancellationToken)
    {
        var current = await _currentUser.GetCurrentAsync(cancellationToken);
        return current.UserId is { } id ? await _users.GetByIdAsync(id, cancellationToken) : null;
    }

    /// <summary>Projects a user to the profile DTO, resolving the company name and avatar URL.</summary>
    private async Task<MyProfileDto> ToDtoAsync(User user, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(user.CompanyId, cancellationToken);
        return new MyProfileDto(
            user.Name,
            user.Email,
            user.Mobile,
            user.Role.ToString(),
            RoleLabel(user.Role),
            user.CompanyId,
            company?.Name,
            AvatarUrl(user.AvatarImageReference),
            user.Role == AccessRole.Administrator);
    }

    /// <summary>Builds the authorised avatar URL from an image reference, or null when unset.</summary>
    public static string? AvatarUrl(string? imageReference) =>
        string.IsNullOrWhiteSpace(imageReference) ? null : $"/api/images/{imageReference}";

    /// <summary>The display label for a role (mirrors the Users service labels).</summary>
    private static string RoleLabel(AccessRole role) => role switch
    {
        AccessRole.Administrator => "Administrator",
        AccessRole.ComplianceManager => "Compliance Manager",
        AccessRole.SiteManager => "Site Manager",
        AccessRole.Auditor => "Auditor (read-only)",
        _ => role.ToString(),
    };
}
