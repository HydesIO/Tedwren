using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Leads;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the lead-pipeline endpoints (<c>/api/leads</c>): platform-admin CRUD, status changes, activity notes
/// and conversion, plus a separate anonymous <c>/capture</c> used by the marketing site's inbound forms. The
/// admin surface requires the PlatformAdmin policy; capture is explicitly anonymous.
/// </summary>
public static class LeadEndpoints
{
    /// <summary>Registers the <c>/api/leads</c> endpoint groups.</summary>
    public static IEndpointRouteBuilder MapLeadEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous inbound capture (marketing site forms) — a separate, explicitly anonymous route.
        app.MapPost("/api/leads/capture", async (CaptureLeadRequest request, ILeadService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.CaptureAsync(request, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .AllowAnonymous()
            .RequireRateLimiting("public")
            .WithTags("Leads")
            .WithName("CaptureLead");

        var admin = app.MapGroup("/api/leads").WithTags("Leads").RequireAuthorization("PlatformAdmin");

        admin.MapGet("/", async (ILeadService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .WithName("ListLeads");

        admin.MapGet("/{id:guid}", async (Guid id, ILeadService service, CancellationToken cancellationToken) =>
                await service.GetAsync(id, cancellationToken) is { } detail ? Results.Ok(detail) : Results.NotFound())
            .WithName("GetLead");

        admin.MapPost("/", async (CreateLeadRequest request, ClaimsPrincipal user, ILeadService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.CreateAsync(request, Actor(user), cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateLead");

        admin.MapPut("/{id:guid}", async (Guid id, UpdateLeadRequest request, ILeadService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.UpdateAsync(id, request, cancellationToken) is { } lead ? Results.Ok(lead) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateLead");

        admin.MapPost("/{id:guid}/status", async (Guid id, UpdateLeadStatusRequest request, ClaimsPrincipal user, ILeadService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.UpdateStatusAsync(id, request, Actor(user), cancellationToken) is { } lead ? Results.Ok(lead) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateLeadStatus");

        admin.MapPost("/{id:guid}/notes", async (Guid id, AddLeadNoteRequest request, ClaimsPrincipal user, ILeadService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.AddNoteAsync(id, request, Actor(user), cancellationToken) is { } note ? Results.Ok(note) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("AddLeadNote");

        admin.MapPost("/{id:guid}/convert", async (Guid id, ConvertLeadRequest request, ClaimsPrincipal user, ILeadService service, CancellationToken cancellationToken) =>
                await service.ConvertAsync(id, request, Actor(user), cancellationToken) is { } lead ? Results.Ok(lead) : Results.NotFound())
            .WithName("ConvertLead");

        admin.MapPost("/{id:guid}/affiliate", async (Guid id, AssignLeadAffiliateRequest request, ClaimsPrincipal user, ILeadService service, CancellationToken cancellationToken) =>
                await service.AssignAffiliateAsync(id, request, Actor(user), cancellationToken) is { } lead ? Results.Ok(lead) : Results.NotFound())
            .WithName("AssignLeadAffiliate");

        return app;
    }

    /// <summary>The acting product owner's display name for activity notes (name claim, falling back to email).</summary>
    private static string? Actor(ClaimsPrincipal user) =>
        user.Identity?.Name
        ?? user.FindFirstValue(ClaimTypes.Name)
        ?? user.FindFirstValue(ClaimTypes.Email);
}
