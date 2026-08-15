namespace Tedwren.Abstractions.Contracts.Leads;

/// <summary>A lead as the admin console lists them on a card. Enum values are surfaced as strings for the UI.</summary>
public sealed record LeadDto(
    Guid Id,
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string? Location,
    string Model,
    int? NumberOfSites,
    decimal? EstimatedRevenue,
    string Status,
    string? AccountManager,
    string? CompanyNumber,
    Guid? AffiliateId,
    Guid? ConvertedAccountId,
    string? Source,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

/// <summary>A single entry in a lead's activity log.</summary>
public sealed record LeadNoteDto(Guid Id, string? Author, string Body, string? StatusChange, DateTimeOffset CreatedUtc);

/// <summary>A lead plus its full activity log, for the detail page.</summary>
public sealed record LeadDetailDto(LeadDto Lead, IReadOnlyList<LeadNoteDto> Notes);

/// <summary>Admin request to create a lead. Model/status are the enum names ("MainContractor", "New", …).</summary>
public sealed record CreateLeadRequest(
    string CompanyName,
    string? ContactName = null,
    string? ContactEmail = null,
    string? Location = null,
    string? Model = null,
    int? NumberOfSites = null,
    decimal? EstimatedRevenue = null,
    string? AccountManager = null,
    string? CompanyNumber = null,
    string? Source = null);

/// <summary>Admin request to update a lead's editable fields (not its status — that has its own endpoint).</summary>
public sealed record UpdateLeadRequest(
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string? Location,
    string? Model,
    int? NumberOfSites,
    decimal? EstimatedRevenue,
    string? AccountManager,
    string? CompanyNumber);

/// <summary>Admin request to move a lead to a new pipeline stage; an automatic activity note records the change.</summary>
public sealed record UpdateLeadStatusRequest(string Status);

/// <summary>Admin request to add an activity-log note to a lead.</summary>
public sealed record AddLeadNoteRequest(string Body);

/// <summary>
/// Admin request to convert a lead into an account. The account id may be supplied (manual assignment) or left
/// null (recording the conversion decision without linking a specific account yet). Sets the lead to Converted.
/// </summary>
public sealed record ConvertLeadRequest(Guid? AccountId = null);

/// <summary>
/// Anonymous inbound lead from the marketing site (a demo request or contact message, Web Plan §7). Limited to
/// what a public form can safely provide; the lead is created as New with the source recorded.
/// </summary>
public sealed record CaptureLeadRequest(
    string CompanyName,
    string? ContactName = null,
    string? ContactEmail = null,
    string? Model = null,
    int? NumberOfSites = null,
    string? Source = null,
    string? Note = null);
