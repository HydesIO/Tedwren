using Tedwren.Abstractions.Contracts.Account;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the signed-in user's self-service account endpoints (<c>/api/me/*</c>): read/update own profile, change
/// own password and set own avatar (SF-20). The whole group is authenticated — it carries no
/// <c>.AllowAnonymous()</c>, so the secure-by-default fallback policy applies (401 for anonymous). Every action
/// resolves the caller server-side and acts only on that user's own record (R15).
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>Registers the <c>/api/me/*</c> self-service endpoints.</summary>
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("Profile");

        group.MapGet("/profile", async (IProfileService profile, CancellationToken ct) =>
            {
                var me = await profile.GetMyProfileAsync(ct);
                return me is null ? Results.NotFound() : Results.Ok(me);
            })
            .WithName("GetMyProfile");

        group.MapPut("/profile", async (UpdateMyProfileRequest request, IProfileService profile, CancellationToken ct) =>
            {
                try
                {
                    var updated = await profile.UpdateMyProfileAsync(request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateMyProfile");

        group.MapPost("/password", async (ChangePasswordRequest request, IProfileService profile, CancellationToken ct) =>
            {
                try
                {
                    var changed = await profile.ChangePasswordAsync(request, ct);
                    return changed
                        ? Results.NoContent()
                        : Results.BadRequest(new { error = "The current password is incorrect." });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("ChangeMyPassword");

        group.MapPost("/avatar", async (UpdateAvatarRequest request, IProfileService profile, CancellationToken ct) =>
            {
                try
                {
                    var updated = await profile.UpdateAvatarAsync(request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateMyAvatar");

        return app;
    }
}
