using Tedwren.Abstractions.Contracts.LaunchList;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the launch-list endpoints (<c>/api/launch-signups</c>): the marketing landing page adds an email
/// anonymously; the platform-admin console lists subscribers and sends the launch announcement. The public
/// signup is a separate, explicitly anonymous group; the admin reads/actions require the PlatformAdmin policy.
/// </summary>
public static class LaunchListEndpoints
{
    /// <summary>Registers the <c>/api/launch-signups</c> endpoint groups.</summary>
    public static IEndpointRouteBuilder MapLaunchListEndpoints(this IEndpointRouteBuilder app)
    {
        // Public signup — anonymous (the landing page has no account behind it).
        var publicGroup = app.MapGroup("/api/launch-signups").WithTags("LaunchList").AllowAnonymous();

        publicGroup.MapPost("/", async (CreateLaunchSignupRequest request, ILaunchListService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.SignUpAsync(request, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateLaunchSignup");

        // Admin surface — platform-admin only.
        var admin = app.MapGroup("/api/launch-signups").WithTags("LaunchList").RequireAuthorization("PlatformAdmin");

        admin.MapGet("/", async (ILaunchListService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .WithName("ListLaunchSignups");

        admin.MapPost("/notify", async (NotifyLaunchRequest? request, ILaunchListService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.NotifyAsync(request ?? new NotifyLaunchRequest(), cancellationToken)))
            .WithName("NotifyLaunchList");

        return app;
    }
}
