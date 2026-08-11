using Tedwren.Abstractions.Contracts.Identity;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Supplies the current console operator (name + role) to the UI, replacing the sample shell user. Until a
/// real authentication phase lands, the server returns a configured/dev identity; the contract is stable so
/// the client does not change when login is added.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Returns the current operator's identity and role.</summary>
    Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default);
}
