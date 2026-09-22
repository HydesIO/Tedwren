using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the qualification-card and competency HTTP endpoints (SF-5–SF-8, SF-10–SF-12). Each delegates to
/// <see cref="IQualificationService"/>, so the same behaviour is served whether the data is mock or
/// database. Designed for the web client and a future mobile app alike.
/// </summary>
public static class QualificationEndpoints
{
    /// <summary>Registers the <c>/api/qualifications</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapQualificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/qualifications").WithTags("Qualifications");

        // The qualification-type library is a global reference list (SF-12), also needed by the anonymous
        // self-service onboarding page, so it is readable without a console account.
        group.MapGet("/types", async (IQualificationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetQualificationTypesAsync(cancellationToken)))
            .WithName("GetQualificationTypes").AllowAnonymous();

        group.MapGet("/people/{personId:guid}/cards",
                async (Guid personId, IQualificationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetCardsForPersonAsync(personId, cancellationToken)))
            .WithName("GetPersonCards");

        group.MapPost("/cards", async (CaptureCardRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CaptureCardAsync(request, cancellationToken);
                return Results.Created($"/api/qualifications/cards/{id}", new { id });
            })
            .WithName("CaptureCard").RequireAuthorization("RequireWrite");

        group.MapPost("/cards/{cardId:guid}/confirm",
                async (Guid cardId, ConfirmCardBody body, IQualificationService service, CancellationToken cancellationToken) =>
                    await service.ConfirmCardAsync(new ConfirmCardRequest(cardId, body.ConfirmedBy), cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .WithName("ConfirmCard").RequireAuthorization("RequireWrite");

        group.MapPost("/cards/{cardId:guid}/renew",
                async (Guid cardId, RenewCardBody body, IQualificationService service, CancellationToken cancellationToken) =>
                {
                    var id = await service.RenewCardAsync(
                        new RenewCardRequest(cardId, body.CardNumber, body.IssuedOn, body.ExpiresOn), cancellationToken);
                    return id is null ? Results.NotFound() : Results.Ok(new { id });
                })
            .WithName("RenewCard").RequireAuthorization("RequireWrite");

        group.MapGet("/people/{personId:guid}/shortfall",
                async (Guid personId, string trade, IQualificationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetShortfallAsync(personId, trade, cancellationToken: cancellationToken)))
            .WithName("GetShortfall");

        // Gate 3 (SF-11): whether the person holds every legally-mandatory accreditation for the trade (spec §2).
        group.MapGet("/people/{personId:guid}/gate3",
                async (Guid personId, string trade, IQualificationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.EvaluateGate3Async(personId, trade, cancellationToken: cancellationToken)))
            .WithName("EvaluateGate3");

        // CSCS live verification (PRD-Phase 1). Resolves the caller's company server-side (R15) and applies the
        // §8.1 decision rules; the coordinator fails closed when the company does not hold the paid CSCS module.
        group.MapPost("/cscs-check",
                async (CscsCheckRequest request, ICurrentUserService currentUser,
                    Tedwren.Application.Qualifications.CscsVerificationCoordinator coordinator, CancellationToken cancellationToken) =>
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    var result = await coordinator.CheckAsync(companyId, request.CardNumber, request.Scheme, cancellationToken);
                    return Results.Ok(new CscsCheckResponse(
                        result.Outcome.ToString(), result.State.ToString(),
                        result.BlocksInduction, result.BlocksEntry, result.Expiry, result.Message));
                })
            .WithName("CscsCheck").RequireAuthorization("RequireWrite");

        MapLibraryManagementEndpoints(group);
        return app;
    }

    /// <summary>
    /// Maps the accreditation-library management endpoints (the qualification-type library SF-12 and the
    /// trade→accreditation map SF-11; Q21). All writes need console write access; the platform-admin-vs-tenant
    /// ownership split (shared rows are platform-admin only) is enforced inside <see cref="IQualificationService"/>,
    /// so a customer Administrator cannot change the shared national library (R15).
    /// </summary>
    private static void MapLibraryManagementEndpoints(IEndpointRouteBuilder group)
    {
        // Qualification-type library (SF-12).
        group.MapGet("/types/manage", async (IQualificationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetQualificationTypesForManagementAsync(cancellationToken)))
            .WithName("GetQualificationTypesForManagement").RequireAuthorization("RequireWrite");

        group.MapPost("/types", async (CreateQualificationTypeRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.CreateQualificationTypeAsync(request, cancellationToken);
                    return Results.Created($"/api/qualifications/types/{id}", new { id });
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
            .WithName("CreateQualificationType").RequireAuthorization("RequireWrite");

        group.MapPut("/types/{id:guid}", async (Guid id, UpdateQualificationTypeRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.UpdateQualificationTypeAsync(id, request, cancellationToken);
                    return Results.NoContent();
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
            .WithName("UpdateQualificationType").RequireAuthorization("RequireWrite");

        group.MapDelete("/types/{id:guid}", async (Guid id, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.DeleteQualificationTypeAsync(id, cancellationToken);
                    return Results.NoContent();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("DeleteQualificationType").RequireAuthorization("RequireWrite");

        // Trade→accreditation map (SF-11).
        group.MapGet("/requirements", async (IQualificationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetTradeRequirementsAsync(cancellationToken)))
            .WithName("GetTradeRequirements").RequireAuthorization("RequireWrite");

        group.MapPost("/requirements", async (CreateTradeRequirementRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.CreateTradeRequirementAsync(request, cancellationToken);
                    return Results.Created($"/api/qualifications/requirements/{id}", new { id });
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
            .WithName("CreateTradeRequirement").RequireAuthorization("RequireWrite");

        group.MapPut("/requirements/{id:guid}", async (Guid id, UpdateTradeRequirementRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.UpdateTradeRequirementAsync(id, request, cancellationToken);
                    return Results.NoContent();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("UpdateTradeRequirement").RequireAuthorization("RequireWrite");

        group.MapDelete("/requirements/{id:guid}", async (Guid id, IQualificationService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.DeleteTradeRequirementAsync(id, cancellationToken);
                    return Results.NoContent();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("DeleteTradeRequirement").RequireAuthorization("RequireWrite");
    }

    /// <summary>Body for confirming a card — the card id comes from the route.</summary>
    private sealed record ConfirmCardBody(string ConfirmedBy);

    /// <summary>Body for renewing a card — the source card id comes from the route.</summary>
    private sealed record RenewCardBody(string? CardNumber, DateOnly? IssuedOn, DateOnly? ExpiresOn);
}
