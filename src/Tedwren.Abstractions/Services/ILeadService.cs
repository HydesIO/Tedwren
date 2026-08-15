using Tedwren.Abstractions.Contracts.Leads;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The sales-lead pipeline (Web Plan §7): create and work prospective customers through stages, log activity,
/// and convert a lead into an account. Admin (platform-admin) operations, plus one anonymous capture entry
/// used by the marketing site's inbound forms. Extends beyond the PRD (which is "not a CRM", §3.2) — anchored
/// to the website plan/spec, not an SF/SUB/MC id.
/// </summary>
public interface ILeadService
{
    /// <summary>Returns every lead, newest-updated first.</summary>
    Task<IReadOnlyList<LeadDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a lead with its activity log, or null when it doesn't exist.</summary>
    Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a lead and returns it.</summary>
    Task<LeadDto> CreateAsync(CreateLeadRequest request, string? actor, CancellationToken cancellationToken = default);

    /// <summary>Updates a lead's editable fields. Null when the lead doesn't exist.</summary>
    Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Moves a lead to a new stage and records an automatic activity note. Null when the lead doesn't exist.</summary>
    Task<LeadDto?> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request, string? actor, CancellationToken cancellationToken = default);

    /// <summary>Adds an activity-log note. Null when the lead doesn't exist.</summary>
    Task<LeadNoteDto?> AddNoteAsync(Guid id, AddLeadNoteRequest request, string? actor, CancellationToken cancellationToken = default);

    /// <summary>Converts a lead into an account (sets Converted, links the account, logs it). Null when the lead doesn't exist.</summary>
    Task<LeadDto?> ConvertAsync(Guid id, ConvertLeadRequest request, string? actor, CancellationToken cancellationToken = default);

    /// <summary>Attributes a lead to an affiliate (or clears it when null) and logs it. Null when the lead doesn't exist.</summary>
    Task<LeadDto?> AssignAffiliateAsync(Guid id, AssignLeadAffiliateRequest request, string? actor, CancellationToken cancellationToken = default);

    /// <summary>Creates a lead from an anonymous marketing-site capture (demo/contact). Deduplicated per open lead by company + email.</summary>
    Task<LeadDto> CaptureAsync(CaptureLeadRequest request, CancellationToken cancellationToken = default);
}
