using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Forms;

/// <summary>
/// The single implementation of the Forms Library assignment rules (PRD-Phase 2, requirement 5). Assigns a form
/// family to the organisation, a site, an operator or an induction, snapshotting the form name and validating
/// the target. Tenant-scoped — every read and write is confined to the caller's company (R15).
/// </summary>
public sealed class FormAssignmentService : IFormAssignmentService
{
    private readonly IFormAssignmentRepository _assignments;
    private readonly IFormTemplateRepository _templates;
    private readonly ICurrentUserService? _currentUser;
    private readonly ISiteRepository? _sites;

    /// <summary>Creates the service over its repositories and the optional current-user (tenant) and site repository (names).</summary>
    public FormAssignmentService(
        IFormAssignmentRepository assignments,
        IFormTemplateRepository templates,
        ICurrentUserService? currentUser = null,
        ISiteRepository? sites = null)
    {
        _assignments = assignments;
        _templates = templates;
        _currentUser = currentUser;
        _sites = sites;
    }

    /// <summary>Resolves the signed-in caller's tenant company (R15). Null when unauthenticated / in unit tests.</summary>
    private async Task<Guid?> ResolveTenantAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId;
    }

    /// <summary>Lists the caller's assignments, resolving site names for site-scoped ones (R15).</summary>
    public async Task<IReadOnlyList<FormAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return new List<FormAssignmentDto>();
        }

        var assignments = await _assignments.GetByCompanyAsync(tenant.Value, cancellationToken);
        var result = new List<FormAssignmentDto>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var siteName = assignment.SiteId is { } siteId && _sites is not null
                ? (await _sites.GetByIdAsync(siteId, cancellationToken))?.Name
                : null;
            result.Add(ToDto(assignment, siteName));
        }

        return result;
    }

    /// <summary>Creates an assignment for the caller's company (R15), snapshotting the form's current name.</summary>
    public async Task<Guid> CreateAsync(CreateFormAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);

        // The family must belong to the caller and have at least one version.
        var versions = tenant is null
            ? await _templates.GetByFamilyAsync(Guid.Empty, request.FormTemplateFamilyId, cancellationToken)
            : await _templates.GetByFamilyAsync(tenant.Value, request.FormTemplateFamilyId, cancellationToken);
        var latest = versions.OrderByDescending(v => v.Version).FirstOrDefault();
        if (latest is null)
        {
            throw new ArgumentException("The form could not be found.", nameof(request));
        }

        var scope = Enum.TryParse<FormScope>(request.Scope, ignoreCase: true, out var s) ? s : FormScope.Organisation;
        if (scope == FormScope.Site && request.SiteId is null)
        {
            throw new ArgumentException("A site is required for a site assignment.", nameof(request));
        }

        if (scope == FormScope.Operator && request.PersonId is null)
        {
            throw new ArgumentException("An operator is required for an operator assignment.", nameof(request));
        }

        var assignment = new FormAssignment
        {
            CompanyId = latest.CompanyId,
            FormTemplateFamilyId = request.FormTemplateFamilyId,
            FormName = latest.Name,
            Scope = scope,
            SiteId = scope == FormScope.Site ? request.SiteId : null,
            PersonId = scope == FormScope.Operator ? request.PersonId : null,
            InductionTemplateId = scope == FormScope.Induction ? request.InductionTemplateId : null,
            Schedule = Enum.TryParse<FormSchedule>(request.Schedule, ignoreCase: true, out var sched) ? sched : FormSchedule.AdHoc,
            FailureAlertEmail = string.IsNullOrWhiteSpace(request.FailureAlertEmail) ? null : request.FailureAlertEmail.Trim(),
        };

        await _assignments.AddAsync(assignment, cancellationToken);
        return assignment.Id;
    }

    /// <summary>Removes an assignment, scoped to the caller (R15).</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        var assignment = await _assignments.GetByIdAsync(id, cancellationToken);
        if (assignment is null || (tenant is not null && assignment.CompanyId != tenant))
        {
            return false;
        }

        await _assignments.DeleteAsync(id, cancellationToken);
        return true;
    }

    /// <summary>Maps an assignment to its DTO.</summary>
    private static FormAssignmentDto ToDto(FormAssignment a, string? siteName) => new(
        a.Id, a.FormTemplateFamilyId, a.FormName, a.Scope.ToString(), a.SiteId, siteName,
        a.PersonId, a.InductionTemplateId, a.Schedule.ToString(), a.FailureAlertEmail, a.CreatedUtc);
}
