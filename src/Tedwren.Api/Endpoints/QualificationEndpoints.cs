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

        group.MapGet("/types", async (IQualificationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetQualificationTypesAsync(cancellationToken)))
            .WithName("GetQualificationTypes");

        group.MapGet("/people/{personId:guid}/cards",
                async (Guid personId, IQualificationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetCardsForPersonAsync(personId, cancellationToken)))
            .WithName("GetPersonCards");

        group.MapPost("/cards", async (CaptureCardRequest request, IQualificationService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CaptureCardAsync(request, cancellationToken);
                return Results.Created($"/api/qualifications/cards/{id}", new { id });
            })
            .WithName("CaptureCard");

        group.MapPost("/cards/{cardId:guid}/confirm",
                async (Guid cardId, ConfirmCardBody body, IQualificationService service, CancellationToken cancellationToken) =>
                    await service.ConfirmCardAsync(new ConfirmCardRequest(cardId, body.ConfirmedBy), cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .WithName("ConfirmCard");

        group.MapPost("/cards/{cardId:guid}/renew",
                async (Guid cardId, RenewCardBody body, IQualificationService service, CancellationToken cancellationToken) =>
                {
                    var id = await service.RenewCardAsync(
                        new RenewCardRequest(cardId, body.CardNumber, body.IssuedOn, body.ExpiresOn), cancellationToken);
                    return id is null ? Results.NotFound() : Results.Ok(new { id });
                })
            .WithName("RenewCard");

        group.MapGet("/people/{personId:guid}/shortfall",
                async (Guid personId, string trade, IQualificationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetShortfallAsync(personId, trade, cancellationToken)))
            .WithName("GetShortfall");

        return app;
    }

    /// <summary>Body for confirming a card — the card id comes from the route.</summary>
    private sealed record ConfirmCardBody(string ConfirmedBy);

    /// <summary>Body for renewing a card — the source card id comes from the route.</summary>
    private sealed record RenewCardBody(string? CardNumber, DateOnly? IssuedOn, DateOnly? ExpiresOn);
}
