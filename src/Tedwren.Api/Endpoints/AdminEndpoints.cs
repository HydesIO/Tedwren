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

        return app;
    }
}
