namespace Tedwren.Abstractions.Contracts.Identity;

/// <summary>
/// The signed-in console operator as the client needs it: display name, role (drives permission checks
/// such as SUB-22) and the company they are scoped to. Sourced from the API; a real login flow replaces
/// the current configured/dev identity in a later authentication phase.
/// </summary>
public sealed record CurrentUserDto(string Name, string Role, Guid? CompanyId);
