using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Composes the operative (mobile) forms assignment surface (M6). Filters the company's form assignments to the ones
/// that apply to this operative — Organisation-scope (everyone), Operator-scope (assigned to them), and Site-scope
/// for the sites they actually attend (from the attendance log) — and resolves each to its family's latest published
/// template version (the immutable id the device caches + fills). Reuses the console repositories directly rather
/// than the client-implemented <c>IFormAssignmentService</c> (SRP). Ids come from the operative token (R15).
/// </summary>
public sealed class MobileFormService : IMobileFormService
{
    /// <summary>How far back to look at attendance when deciding which sites' forms an operative sees.</summary>
    private const int AttendanceWindowDays = 180;

    private readonly IFormAssignmentRepository _assignments;
    private readonly IFormTemplateRepository _templates;
    private readonly IAttendanceRepository _attendance;
    private readonly ISiteRepository _sites;

    /// <summary>Creates the service over the assignment, template, attendance and site repositories.</summary>
    public MobileFormService(
        IFormAssignmentRepository assignments,
        IFormTemplateRepository templates,
        IAttendanceRepository attendance,
        ISiteRepository sites)
    {
        _assignments = assignments;
        _templates = templates;
        _attendance = attendance;
        _sites = sites;
    }

    /// <summary>The forms assigned to this operative, each resolved to its latest published version.</summary>
    public async Task<IReadOnlyList<MobileFormAssignmentDto>> GetAssignmentsAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var assignments = await _assignments.GetByCompanyAsync(companyId, cancellationToken);

        // Sites the operative works at: the distinct sites in their recent attendance log (Site-scope eligibility).
        var now = DateTimeOffset.UtcNow;
        var attendance = await _attendance.GetByPersonInRangeAsync(personId, now.AddDays(-AttendanceWindowDays), now, cancellationToken);
        var attendedSites = attendance.Select(a => a.SiteId).ToHashSet();

        var result = new List<MobileFormAssignmentDto>();
        foreach (var assignment in assignments)
        {
            var appliesToMe = assignment.Scope switch
            {
                FormScope.Organisation => true,
                FormScope.Operator => assignment.PersonId == personId,
                FormScope.Site => assignment.SiteId is { } siteId && attendedSites.Contains(siteId),
                _ => false, // Induction-scope forms are completed in the induction flow, not the operative inbox.
            };
            if (!appliesToMe)
            {
                continue;
            }

            // Resolve the family's latest published version — the immutable template the device caches and fills.
            var versions = await _templates.GetByFamilyAsync(companyId, assignment.FormTemplateFamilyId, cancellationToken);
            var published = versions
                .Where(t => t.Status == FormTemplateStatus.Published)
                .OrderByDescending(t => t.Version)
                .FirstOrDefault();
            if (published is null)
            {
                continue; // nothing published to complete yet
            }

            string? siteName = assignment.SiteId is { } sid
                ? (await _sites.GetByIdAsync(sid, cancellationToken))?.Name
                : null;

            result.Add(new MobileFormAssignmentDto(
                assignment.Id,
                assignment.FormTemplateFamilyId,
                published.Id,
                published.Name,
                assignment.Scope.ToString(),
                assignment.SiteId,
                siteName,
                assignment.Schedule.ToString()));
        }

        return result;
    }
}
