using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the current-operator endpoint (<c>/api/me</c>). Returns the signed-in console user (name + role)
/// that drives display and permission checks (e.g. SUB-22). A configured/dev identity until authentication.
/// </summary>
public static class CurrentUserEndpoints
{
    /// <summary>Registers the <c>/api/me</c> endpoint.</summary>
    public static IEndpointRouteBuilder MapCurrentUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (ICurrentUserService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCurrentAsync(cancellationToken)))
            .WithName("GetCurrentUser")
            .WithTags("Identity");

        return app;
    }
}
