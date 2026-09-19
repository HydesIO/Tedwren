using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) forms surface (<c>/api/mobile/forms/*</c>, M6): the forms assigned to the operative,
/// the published template to fill, and the completed-form submit. Gated by <c>RequireOperative</c> <b>and</b> the
/// <c>forms</c> module (fail-closed 403 without it, Q2). PersonId/CompanyId + the submitter's name come from the
/// operative token, never the body (R15). Submit reuses the existing engine
/// (<see cref="IFormSubmissionService.SubmitForContextAsync"/>) — full validation, file storage, red-RAG alerting
/// and PDF — and is idempotent on the request's client id (R4/R16).
/// </summary>
public static class MobileFormEndpoints
{
    /// <summary>Registers the <c>/api/mobile/forms</c> operative forms group.</summary>
    public static IEndpointRouteBuilder MapMobileFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/forms").WithTags("Mobile")
            .RequireAuthorization("RequireOperative")
            .AddEndpointFilter(ModuleGate.Require("forms"));

        group.MapGet("/assignments", async (ClaimsPrincipal user, IMobileFormService forms, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId, out _))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await forms.GetAssignmentsAsync(companyId, personId, cancellationToken));
            })
            .WithName("MobileFormAssignments");

        group.MapGet("/templates/{id:guid}", async (Guid id, IFormTemplateService templates, CancellationToken cancellationToken) =>
            {
                // Tenant-scoped + published-only via the service (reads the operative's company claim). Null → 404.
                var template = await templates.GetTemplateForFillAsync(id, cancellationToken);
                return template is null ? Results.NotFound() : Results.Ok(template);
            })
            .WithName("MobileFormTemplate");

        group.MapPost("/submissions", async (CreateFormSubmissionRequest request, ClaimsPrincipal user, IFormSubmissionService submissions, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId, out var name))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    var id = await submissions.SubmitForContextAsync(companyId, personId, name, request, cancellationToken);
                    return Results.Ok(new { Id = id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("MobileFormSubmit");

        return app;
    }

    /// <summary>Reads the operative's CompanyId (company claim), PersonId (sub) and display name from the token — never the body (R15).</summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out Guid personId, out string name)
    {
        companyId = Guid.Empty;
        name = user.FindFirstValue(ClaimTypes.Name) ?? "Operative";
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId)
            && Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }
}
