using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;

namespace Tedwren.Application.Identity;

/// <summary>
/// Configurable options for the current-console-operator identity. A dev/stand-in until a real
/// authentication phase; bound from the <c>CurrentUser</c> configuration section on the API.
/// </summary>
public sealed class CurrentUserOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "CurrentUser";

    /// <summary>Operator display name.</summary>
    public string Name { get; set; } = "Alex Morgan";

    /// <summary>Operator role — drives permission checks such as SUB-22 (pack sending).</summary>
    public string Role { get; set; } = "Compliance Manager";
}

/// <summary>
/// Returns the current console operator (SF-20/SF-23 roles). Until authentication lands this yields a
/// configured identity; the interface is stable so the client is unaffected when a login flow replaces it.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly CurrentUserOptions _options;

    /// <summary>Creates the service over the configured identity options.</summary>
    public CurrentUserService(CurrentUserOptions options) => _options = options;

    /// <summary>Returns the configured operator identity and role.</summary>
    public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CurrentUserDto(_options.Name, _options.Role, CompanyId: null));
}
