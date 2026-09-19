using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) binary upload endpoint (<c>/api/mobile/uploads</c>, M5): a single multipart image
/// part, validated (type + 10 MB cap) and stored via <see cref="IImageStore"/>, returning an opaque reference the
/// caller then attaches to an evidence item or hazard report. The bytes are only ever served through the authorised
/// image route, never a permanent public URL (R9). Gated by <c>RequireOperative</c>; a generic binary sink shared by
/// evidence, hazards and (later) forms, so it is not module-gated — the entitlement-bearing action is the write that
/// consumes the reference.
/// </summary>
public static class MobileUploadEndpoints
{
    /// <summary>Registers the <c>/api/mobile/uploads</c> operative upload group.</summary>
    public static IEndpointRouteBuilder MapMobileUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/uploads").WithTags("Mobile").RequireAuthorization("RequireOperative");

        group.MapPost("", async (IFormFile? file, IImageStore images, CancellationToken cancellationToken) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "A file is required." });
                }

                try
                {
                    UploadValidation.ValidateFile(file.Length, file.ContentType, UploadKind.Image);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);
                var reference = await images.SaveAsync(buffer.ToArray(), file.ContentType, cancellationToken);
                return Results.Ok(new MobileUploadResultDto(reference));
            })
            .WithName("MobileUpload")
            .DisableAntiforgery(); // Bearer-authenticated native client; no browser antiforgery token.

        return app;
    }
}
