using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the compliance-pack HTTP endpoints (SUB-13–SUB-26, R7–R9). Sender routes (readiness preview, build,
/// list, revoke) are company-scoped; recipient routes (view, download) are reached only through the opaque
/// token + passcode, so there are no permanent public asset URLs (R8/R9). A refused recipient access returns
/// 403 with a reason; a wrong sender scope returns 404.
/// </summary>
public static class CompliancePackEndpoints
{
    /// <summary>Registers the <c>/api/packs</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapCompliancePackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/packs").WithTags("CompliancePacks");

        group.MapPost("/readiness", async (ReadinessPreviewRequest request, ICompliancePackService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.PreviewReadinessAsync(request.CompanyId, request.OperativeIds, cancellationToken)))
            .WithName("PreviewPackReadiness");

        group.MapPost("/", async (BuildPackRequest request, ICompliancePackService service, CancellationToken cancellationToken) =>
            {
                var result = await service.BuildAsync(request, cancellationToken);
                // SUB-14: a build blocked by unacknowledged readiness issues is a 409 carrying the issues.
                return result.Sent ? Results.Ok(result) : Results.Conflict(result);
            })
            .WithName("BuildPack");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, ICompliancePackService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListForCompanyAsync(companyId, cancellationToken)))
            .WithName("ListCompanyPacks");

        group.MapPost("/company/{companyId:guid}/{packId:guid}/revoke", async (Guid companyId, Guid packId, ICompliancePackService service, CancellationToken cancellationToken) =>
                await service.RevokeAsync(companyId, packId, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .WithName("RevokePack");

        // Recipient access — token + passcode only, no account (R8).
        group.MapGet("/view", async (string token, string passcode, ICompliancePackService service, CancellationToken cancellationToken) =>
            {
                var result = await service.ViewAsync(token, passcode, cancellationToken);
                return result.Granted ? Results.Ok(result.Pack) : Results.Json(new { reason = result.Reason }, statusCode: StatusCodes.Status403Forbidden);
            })
            .WithName("ViewPack");

        group.MapGet("/download.csv", async (string token, string passcode, ICompliancePackService service, CancellationToken cancellationToken) =>
                await service.DownloadCsvAsync(token, passcode, cancellationToken) is { } bytes
                    ? Results.File(bytes, "text/csv", "compliance-pack.csv")
                    : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("DownloadPackCsv");

        group.MapGet("/download.zip", async (string token, string passcode, ICompliancePackService service, CancellationToken cancellationToken) =>
                await service.DownloadZipAsync(token, passcode, cancellationToken) is { } bytes
                    ? Results.File(bytes, "application/zip", "compliance-pack.zip")
                    : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("DownloadPackZip");

        group.MapGet("/download.pdf", async (string token, string passcode, ICompliancePackService service, CancellationToken cancellationToken) =>
                await service.DownloadPdfAsync(token, passcode, cancellationToken) is { } bytes
                    ? Results.File(bytes, "application/pdf", "compliance-pack.pdf")
                    : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("DownloadPackPdf");

        return app;
    }

    /// <summary>Body for the readiness-preview endpoint.</summary>
    public sealed record ReadinessPreviewRequest(Guid CompanyId, IReadOnlyList<Guid> OperativeIds);
}
