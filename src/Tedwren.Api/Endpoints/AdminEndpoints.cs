using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the Tedwren platform admin-area HTTP endpoints (<c>/api/admin</c>). The whole group is gated by the
/// <c>PlatformAdmin</c> policy — Administrator in the Tedwren tenant — so a customer's own administrator can
/// never reach these cross-company reads. Delegates to the existing tenant services, which already return
/// all-company data, rather than duplicating query logic. Billing/mandate/payment endpoints join this group
/// in later phases.
/// </summary>
public static class AdminEndpoints
{
    /// <summary>Registers the platform-admin endpoint group.</summary>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("PlatformAdmin");

        group.MapGet("/companies", async (IOrganisationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCompaniesAsync(cancellationToken)))
            .WithName("AdminGetCompanies");

        group.MapGet("/users", async (IUserService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetUsersAsync(cancellationToken)))
            .WithName("AdminGetUsers");

        // Platform-admin edit of any console user: name/role, suspend state, and an optional password reset.
        // Orchestrates the existing IUserService operations so the write logic is not duplicated.
        group.MapPut("/users/{id:guid}", async (Guid id, AdminUpdateUserRequest request, IUserService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var updated = await service.UpdateUserAsync(id, new UpdateUserRequest(request.Name, request.Role), cancellationToken);
                    if (updated is null)
                    {
                        return Results.NotFound();
                    }

                    var isSuspended = string.Equals(updated.Status, "Suspended", StringComparison.OrdinalIgnoreCase);
                    if (request.Suspended && !isSuspended)
                    {
                        updated = await service.SuspendUserAsync(id, cancellationToken) ?? updated;
                    }
                    else if (!request.Suspended && isSuspended)
                    {
                        updated = await service.ReactivateUserAsync(id, cancellationToken) ?? updated;
                    }

                    if (!string.IsNullOrWhiteSpace(request.NewPassword))
                    {
                        updated = await service.SetPasswordAsync(id, request.NewPassword!, cancellationToken) ?? updated;
                    }

                    return Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("AdminUpdateUser");

        return app;
    }
}
