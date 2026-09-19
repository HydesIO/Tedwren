using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Safety;

/// <summary>
/// The store-agnostic hazard / near-miss reporting service (PRD §8.2). A report gets an immediate reference; a
/// responsible person triages it (assign → close with a note); the register yields leading-indicator counts.
/// Everything is scoped to the reporting company (R15). Any photo is stored through <see cref="IImageStore"/> (no
/// permanent public URL, R9) after size/type validation.
/// </summary>
public sealed class HazardReportService : IHazardReportService
{
    private readonly IHazardReportRepository _reports;
    private readonly IImageStore _images;

    /// <summary>Creates the service over the hazard repository and the image store.</summary>
    public HazardReportService(IHazardReportRepository reports, IImageStore images)
    {
        _reports = reports;
        _images = images;
    }

    /// <summary>Records a hazard report and returns it with its reference.</summary>
    public async Task<HazardReportDto> ReportAsync(Guid companyId, string reportedBy, ReportHazardRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("A description is required.", nameof(request));
        }

        string? photoReference = null;
        if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
        {
            UploadValidation.Validate(request.PhotoBase64, request.PhotoContentType, UploadKind.Image);
            var bytes = Convert.FromBase64String(StripDataUrl(request.PhotoBase64));
            photoReference = await _images.SaveAsync(bytes, request.PhotoContentType ?? "image/jpeg", cancellationToken);
        }

        var report = new HazardReport
        {
            CompanyId = companyId,
            Reference = BuildReference(),
            Kind = SafetyEnum.Parse(request.Kind, HazardKind.NearMiss),
            Description = request.Description.Trim(),
            Location = Clean(request.Location),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhotoReference = photoReference,
            Severity = SafetyEnum.Parse(request.Severity, SafetySeverity.Medium),
            Category = Clean(request.Category),
            Status = HazardStatus.Open,
            ReportedBy = string.IsNullOrWhiteSpace(reportedBy) ? "System" : reportedBy.Trim(),
        };
        await _reports.AddAsync(report, cancellationToken);
        return ToDto(report);
    }

    /// <summary>Returns a company's hazard reports, newest first.</summary>
    public async Task<IReadOnlyList<HazardReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        (await _reports.GetByCompanyAsync(companyId, cancellationToken)).Select(ToDto).ToList();

    /// <summary>Returns a single hazard report, or null when missing/cross-tenant (R15).</summary>
    public async Task<HazardReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetAsync(id, cancellationToken);
        return report is null || report.CompanyId != companyId ? null : ToDto(report);
    }

    /// <summary>Assigns a report to a responsible person (sets it to Assigned), scoped to the company (R15).</summary>
    public async Task<bool> AssignAsync(Guid companyId, Guid id, string assignedTo, string? category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            throw new ArgumentException("An assignee is required.", nameof(assignedTo));
        }

        var report = await _reports.GetAsync(id, cancellationToken);
        if (report is null || report.CompanyId != companyId)
        {
            return false;
        }

        report.AssignedTo = assignedTo.Trim();
        if (!string.IsNullOrWhiteSpace(category))
        {
            report.Category = category.Trim();
        }

        if (report.Status == HazardStatus.Open)
        {
            report.Status = HazardStatus.Assigned;
        }

        await _reports.UpdateAsync(report, cancellationToken);
        return true;
    }

    /// <summary>Closes a report out with a required note, scoped to the company (R15).</summary>
    public async Task<bool> CloseAsync(Guid companyId, Guid id, string note, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A closure note is required.", nameof(note));
        }

        var report = await _reports.GetAsync(id, cancellationToken);
        if (report is null || report.CompanyId != companyId)
        {
            return false;
        }

        report.Status = HazardStatus.Closed;
        report.ClosureNote = note.Trim();
        report.ClosedUtc = DateTimeOffset.UtcNow;
        await _reports.UpdateAsync(report, cancellationToken);
        return true;
    }

    /// <summary>Returns leading-indicator counts (by status and kind) for the company's hazard register.</summary>
    public async Task<HazardStatsDto> GetStatsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var reports = await _reports.GetByCompanyAsync(companyId, cancellationToken);
        return new HazardStatsDto(
            reports.Count,
            reports.Count(r => r.Status == HazardStatus.Open),
            reports.Count(r => r.Status == HazardStatus.Assigned),
            reports.Count(r => r.Status == HazardStatus.Closed),
            reports.Count(r => r.Kind == HazardKind.NearMiss),
            reports.Count(r => r.Kind == HazardKind.Hazard),
            reports.Count(r => r.Kind == HazardKind.UnsafeAct),
            reports.Count(r => r.Kind == HazardKind.UnsafeCondition),
            reports.Count(r => r.Severity == SafetySeverity.High));
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Builds a human-readable report reference (date + short random suffix).</summary>
    private static string BuildReference() =>
        $"HAZ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>Strips a "data:...;base64," prefix from a base64 payload, if present.</summary>
    private static string StripDataUrl(string base64)
    {
        var comma = base64.IndexOf(',');
        return base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0 ? base64[(comma + 1)..] : base64;
    }

    /// <summary>Maps a hazard report entity to its DTO (enum values as strings).</summary>
    private static HazardReportDto ToDto(HazardReport r) => new(
        r.Id, r.Reference, r.Kind.ToString(), r.Description, r.Location, r.Latitude, r.Longitude,
        r.PhotoReference is not null, r.Severity.ToString(), r.Category, r.Status.ToString(), r.AssignedTo,
        r.ReportedBy, r.ReportedUtc, r.ClosedUtc, r.ClosureNote);
}
