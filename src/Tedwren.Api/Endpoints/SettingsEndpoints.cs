using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the per-company general-settings endpoints (<c>/api/settings</c>) for System Configuration:
/// read the current settings and save them.
/// </summary>
public static class SettingsEndpoints
{
    /// <summary>Registers the <c>/api/settings</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/{companyId:guid}", async (Guid companyId, ISettingsService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForCompanyAsync(companyId, cancellationToken)))
            .WithName("GetCompanySettings");

        group.MapPut("/{companyId:guid}", async (Guid companyId, GeneralSettingsDto settings, ISettingsService service, CancellationToken cancellationToken) =>
            {
                await service.SaveForCompanyAsync(companyId, settings, cancellationToken);
                return Results.NoContent();
            })
            .WithName("SaveCompanySettings");

        return app;
    }
}
