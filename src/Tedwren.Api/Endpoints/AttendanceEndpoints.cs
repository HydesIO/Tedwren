using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the attendance HTTP endpoints (SF-13–SF-19). Sign-in / sign-out always return 200 with an outcome
/// body — every attempt is recorded, including refusals (SF-16). Designed for the worker's phone browser
/// (no app, SF-13/SF-25) and the site manager's console alike.
/// </summary>
public static class AttendanceEndpoints
{
    /// <summary>Registers the <c>/api/attendance</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance").WithTags("Attendance");

        group.MapPost("/sign-in", async (SignInRequest request, IAttendanceService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.SignInAsync(request, cancellationToken)))
            .WithName("SignIn");

        group.MapPost("/sign-out", async (SignOutRequest request, IAttendanceService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.SignOutAsync(request, cancellationToken)))
            .WithName("SignOut");

        group.MapGet("/on-site/{siteId:guid}", async (Guid siteId, IAttendanceService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetOnSiteAsync(siteId, cancellationToken)))
            .WithName("GetOnSite");

        group.MapGet("/sites/{siteId:guid}/records", async (Guid siteId, IAttendanceService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSiteRecordsAsync(siteId, 50, cancellationToken)))
            .WithName("GetAttendanceRecords");

        return app;
    }
}
