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
/// <param name="AvatarUrl">
/// The operator's avatar address (the authorised <c>/api/images/{ref}</c> endpoint), or null when none is set,
/// so the top-bar avatar can render from the single <c>/api/me</c> call without a second request.
/// </param>
/// <param name="UserId">
/// The operator's own user id, resolved server-side from the authenticated request. Lets the client link to
/// its own profile; the server never trusts a client-supplied id when acting on the caller's behalf (R15).
/// </param>
public sealed record CurrentUserDto(
    string Name,
    string Role,
    Guid? CompanyId,
    bool IsPlatformAdmin = false,
    string? AvatarUrl = null,
    Guid? UserId = null);
