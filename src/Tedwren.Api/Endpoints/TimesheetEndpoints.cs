using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the timesheet HTTP endpoints (SUB-7–SUB-12, SUB-27, MC-24). A timesheet is a first-class object:
/// listed per company/week (MC-24), rolled up from attendance, submitted, approved at a chosen granularity
/// (SUB-9), returned for correction (never denied, R18), corrected by appending lines (R16), and exported as
/// CSV and Excel (SUB-10).
/// </summary>
public static class TimesheetEndpoints
{
    /// <summary>Registers the <c>/api/timesheets</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapTimesheetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timesheets").WithTags("Timesheets");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, DateOnly week, ITimesheetService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForCompanyWeekAsync(companyId, week, cancellationToken)))
            .WithName("GetCompanyTimesheets");

        group.MapGet("/{id:guid}", async (Guid id, ITimesheetService service, CancellationToken cancellationToken) =>
                await service.GetTimesheetAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetTimesheet");

        group.MapGet("/operative", async (Guid companyId, Guid personId, DateOnly week, ITimesheetService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetOrCreateForWeekAsync(companyId, personId, week, cancellationToken)))
            .WithName("GetOrCreateOperativeTimesheet");

        group.MapGet("/operative/hours", async (Guid companyId, Guid personId, DateOnly week, ITimesheetService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetOperativeHoursAsync(companyId, personId, week, cancellationToken)))
            .WithName("GetOperativeHours");

        group.MapPost("/{id:guid}/submit", async (Guid id, ITimesheetService service, CancellationToken cancellationToken) =>
                await service.SubmitAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("SubmitTimesheet");

        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveTimesheetRequest request, ITimesheetService service, CancellationToken cancellationToken) =>
                await service.ApproveAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ApproveTimesheet");

        group.MapPost("/{id:guid}/return", async (Guid id, ReturnTimesheetRequest request, ITimesheetService service, CancellationToken cancellationToken) =>
                await service.ReturnAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ReturnTimesheet");

        group.MapPost("/{id:guid}/correct", async (Guid id, CorrectLineRequest request, ITimesheetService service, CancellationToken cancellationToken) =>
                await service.CorrectLineAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("CorrectTimesheetLine");

        group.MapGet("/{id:guid}/export.csv", async (Guid id, ITimesheetService service, CancellationToken cancellationToken) =>
            {
                var csv = await service.ExportTimesheetCsvAsync(id, cancellationToken);
                return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"timesheet-{id}.csv");
            })
            .WithName("ExportTimesheetCsv");

        group.MapGet("/company/{companyId:guid}/export.csv", async (Guid companyId, DateOnly week, ITimesheetService service, CancellationToken cancellationToken) =>
            {
                var csv = await service.ExportCompanyWeekCsvAsync(companyId, week, cancellationToken);
                return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"timesheets-{week:yyyy-MM-dd}.csv");
            })
            .WithName("ExportCompanyTimesheetsCsv");

        group.MapGet("/company/{companyId:guid}/export.xlsx", async (Guid companyId, DateOnly week, ITimesheetService service, CancellationToken cancellationToken) =>
            {
                var xlsx = await service.ExportCompanyWeekXlsxAsync(companyId, week, cancellationToken);
                return Results.File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"timesheets-{week:yyyy-MM-dd}.xlsx");
            })
            .WithName("ExportCompanyTimesheetsXlsx");

        return app;
    }
}
