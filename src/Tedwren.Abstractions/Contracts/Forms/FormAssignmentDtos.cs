namespace Tedwren.Abstractions.Contracts.Forms;

/// <summary>A form assignment for listing — where a form is assigned, its cadence and failed-check alert.</summary>
public sealed record FormAssignmentDto(
    Guid Id,
    Guid FormTemplateFamilyId,
    string FormName,
    string Scope,
    Guid? SiteId,
    string? SiteName,
    Guid? PersonId,
    Guid? InductionTemplateId,
    string Schedule,
    string? FailureAlertEmail,
    DateTimeOffset CreatedUtc);

/// <summary>Request to assign a form (by family) to a scope (requirement 5). <c>Scope</c>/<c>Schedule</c> are the enum names.</summary>
public sealed record CreateFormAssignmentRequest(
    Guid FormTemplateFamilyId,
    string Scope,
    Guid? SiteId,
    Guid? PersonId,
    Guid? InductionTemplateId,
    string Schedule,
    string? FailureAlertEmail);
