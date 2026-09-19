using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Safety;

/// <summary>
/// Maps a <see cref="HazardReport"/> to its DTO (enum values as strings), exposing the photo only as a flag (R9).
/// Shared by the console <see cref="HazardReportService"/> and the mobile <c>MobileHazardService</c> so the
/// projection can't drift between the two write paths.
/// </summary>
internal static class HazardReportMapper
{
    /// <summary>Projects a hazard report entity to its wire DTO.</summary>
    public static HazardReportDto ToDto(HazardReport r) => new(
        r.Id, r.Reference, r.Kind.ToString(), r.Description, r.Location, r.Latitude, r.Longitude,
        r.PhotoReference is not null, r.Severity.ToString(), r.Category, r.Status.ToString(), r.AssignedTo,
        r.ReportedBy, r.ReportedUtc, r.ClosedUtc, r.ClosureNote);
}
