namespace Tedwren.Abstractions.Contracts.Identity;

/// <summary>
/// The signed-in console operator as the client needs it: display name, role (drives permission checks
/// such as SUB-22) and the company they are scoped to. Sourced from the API; a real login flow replaces
/// the current configured/dev identity in a later authentication phase.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Role">Console access role (e.g. Administrator/Auditor); drives permission checks.</param>
/// <param name="CompanyId">The tenant company the operator is scoped to (R15).</param>
/// <param name="IsPlatformAdmin">
/// True when the operator is a Tedwren platform administrator — an <c>Administrator</c> in the Tedwren
/// tenant — as opposed to a customer's own company administrator. Gates the admin area. Computed
/// server-side from the request claims; never trusted from the client.
/// </param>
public sealed record CurrentUserDto(string Name, string Role, Guid? CompanyId, bool IsPlatformAdmin = false);
