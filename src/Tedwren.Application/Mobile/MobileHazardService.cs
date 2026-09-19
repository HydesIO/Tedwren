using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Application.Safety;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Records operatives' offline-captured hazard / near-miss reports (M5), reusing <see cref="IHazardReportRepository"/>
/// directly rather than the console <c>IHazardReportService</c> (SRP — the client implements that interface). It
/// mirrors the console <c>HazardReportService.ReportAsync</c> and shares the <see cref="HazardReference"/> +
/// <see cref="HazardReportMapper"/> so both write paths stay consistent. Append-only (R4); idempotent on the
/// device-generated client id (R4/R16); company + reporter come from the token (R15); the capture UTC is preserved
/// (R11). Any photo is pre-uploaded via <c>/api/mobile/uploads</c> and referenced here (R9).
/// </summary>
public sealed class MobileHazardService : IMobileHazardService
{
    private readonly IHazardReportRepository _reports;

    /// <summary>Creates the service over the hazard repository.</summary>
    public MobileHazardService(IHazardReportRepository reports) => _reports = reports;

    /// <summary>Records a hazard report, or returns the existing one when the client id was already synced.</summary>
    public async Task<HazardReportDto> ReportAsync(Guid companyId, string reportedBy, MobileReportHazardRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("A description is required.", nameof(request));
        }

        // Idempotency (R4/R16): a retried sync returns the existing report, never a duplicate.
        var existing = await _reports.GetAsync(request.ClientId, cancellationToken);
        if (existing is not null)
        {
            if (existing.CompanyId != companyId)
            {
                throw new InvalidOperationException("This id already belongs to another company.");
            }

            return HazardReportMapper.ToDto(existing);
        }

        var report = new HazardReport
        {
            Id = request.ClientId,
            CompanyId = companyId,
            Reference = HazardReference.New(),
            Kind = SafetyEnum.Parse(request.Kind, HazardKind.NearMiss),
            Description = request.Description.Trim(),
            Location = Clean(request.Location),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhotoReference = Clean(request.PhotoReference),
            Severity = SafetyEnum.Parse(request.Severity, SafetySeverity.Medium),
            Category = Clean(request.Category),
            Status = HazardStatus.Open,
            ReportedBy = string.IsNullOrWhiteSpace(reportedBy) ? "System" : reportedBy.Trim(),
            ReportedUtc = request.CapturedUtc,
        };
        await _reports.AddAsync(report, cancellationToken);
        return HazardReportMapper.ToDto(report);
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
