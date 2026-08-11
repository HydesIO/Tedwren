using Tedwren.Application.Persistence;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the private image endpoint (<c>/api/images/{id}</c>). Card photos are served only to an authenticated
/// console user (the fallback policy applies) and never from a permanent public address (R9).
/// </summary>
public static class ImageEndpoints
{
    /// <summary>Registers the <c>/api/images</c> endpoint.</summary>
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/images/{id}", async (string id, IImageStore store, CancellationToken cancellationToken) =>
            {
                var image = await store.GetAsync(id, cancellationToken);
                return image is null ? Results.NotFound() : Results.File(image.Bytes, image.ContentType);
            })
            .WithName("GetImage")
            .WithTags("Images");

        return app;
    }
}
