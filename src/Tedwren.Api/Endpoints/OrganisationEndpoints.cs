using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the organisation (companies + operatives) HTTP endpoints. Each delegates straight to
/// <see cref="IOrganisationService"/>, so the same behaviour is served whether the underlying data is
/// mock or database. Designed to be consumed equally by the web client and a future mobile app.
/// </summary>
public static class OrganisationEndpoints
{
    /// <summary>Registers the <c>/api/organisation</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organisation").WithTags("Organisation");

        group.MapGet("/companies", async (IOrganisationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCompaniesAsync(cancellationToken)))
            .WithName("GetCompanies");

        group.MapGet("/companies/{slug}", async (string slug, IOrganisationService service, CancellationToken cancellationToken) =>
            {
                var company = await service.GetCompanyAsync(slug, cancellationToken);
                return company is null ? Results.NotFound() : Results.Ok(company);
            })
            .WithName("GetCompany");

        group.MapPost("/companies", async (CreateCompanyRequest request, IOrganisationService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CreateCompanyAsync(request, cancellationToken);
                return Results.Created($"/api/organisation/companies/{id}", new { id });
            })
            .WithName("CreateCompany");

        group.MapPost("/operatives", async (AddOperativeRequest request, IOrganisationService service, CancellationToken cancellationToken) =>
            {
                var result = await service.AddOperativeAsync(request, cancellationToken);
                return result.Succeeded ? Results.Ok(result) : Results.Conflict(result);
            })
            .WithName("AddOperative");

        group.MapPost("/companies/{companyId:guid}/operatives/{engagementId:guid}/archive",
                async (Guid companyId, Guid engagementId, IOrganisationService service, CancellationToken cancellationToken) =>
                    await service.ArchiveEngagementAsync(companyId, engagementId, cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .WithName("ArchiveEngagement");

        group.MapPost("/companies/{companyId:guid}/operatives/{engagementId:guid}/reactivate",
                async (Guid companyId, Guid engagementId, IOrganisationService service, CancellationToken cancellationToken) =>
                    await service.ReactivateEngagementAsync(companyId, engagementId, cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .WithName("ReactivateEngagement");

        return app;
    }
}
