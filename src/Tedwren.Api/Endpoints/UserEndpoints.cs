using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the console user-management HTTP endpoints (SF-20, SF-23, Q2). Each delegates to
/// <see cref="IUserService"/>, so the same behaviour is served whether the data is mock or database, and by
/// the web client or a future mobile app. Access is withdrawn by suspension (POST .../suspend), never by a
/// hard delete, so the audit trail is preserved.
/// </summary>
public static class UserEndpoints
{
    /// <summary>Registers the <c>/api/users</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", async (IUserService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetUsersAsync(cancellationToken)))
            .WithName("GetUsers");

        group.MapGet("/roles", async (IUserService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetRolesAsync(cancellationToken)))
            .WithName("GetUserRoles");

        group.MapGet("/{id:guid}", async (Guid id, IUserService service, CancellationToken cancellationToken) =>
            {
                var user = await service.GetUserAsync(id, cancellationToken);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .WithName("GetUser");

        group.MapPost("/", async (InviteUserRequest request, IUserService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.InviteUserAsync(request, cancellationToken);
                    return Results.Created($"/api/users/{result.UserId}", result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("InviteUser");

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, IUserService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await service.UpdateUserAsync(id, request, cancellationToken);
                    return user is null ? Results.NotFound() : Results.Ok(user);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateUser");

        group.MapPut("/{id:guid}/password", async (Guid id, SetPasswordBody body, IUserService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await service.SetPasswordAsync(id, body.NewPassword, cancellationToken);
                    return user is null ? Results.NotFound() : Results.Ok(user);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SetUserPassword");

        group.MapPost("/{id:guid}/suspend", async (Guid id, IUserService service, CancellationToken cancellationToken) =>
            {
                var user = await service.SuspendUserAsync(id, cancellationToken);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .WithName("SuspendUser");

        group.MapPost("/{id:guid}/reactivate", async (Guid id, IUserService service, CancellationToken cancellationToken) =>
            {
                var user = await service.ReactivateUserAsync(id, cancellationToken);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .WithName("ReactivateUser");

        return app;
    }

    /// <summary>Body for setting a user's password.</summary>
    public sealed record SetPasswordBody(string NewPassword);
}
