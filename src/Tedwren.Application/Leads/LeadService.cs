using Tedwren.Abstractions.Contracts.Leads;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Leads;

/// <summary>
/// The sales-lead pipeline service (Web Plan §7). Holds the pipeline rules: creating and editing leads,
/// moving them through stages (each stage change logged as an automatic activity note), appending notes, and
/// converting a lead into an account. The repository only holds records. Pricing is not computed here —
/// estimated revenue is admin-entered, since which-meter/which-band is configuration (PRD §9).
/// </summary>
public sealed class LeadService : ILeadService
{
    private readonly ILeadRepository _repository;
    private readonly ICompanyRepository _companies;

    /// <summary>Injects the lead repository and the (product-database) company repository for account matching.</summary>
    public LeadService(ILeadRepository repository, ICompanyRepository companies)
    {
        _repository = repository;
        _companies = companies;
    }

    /// <summary>Returns every lead, newest-updated first.</summary>
    public async Task<IReadOnlyList<LeadDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var leads = await _repository.ListAsync(cancellationToken);
        return leads.Select(ToDto).ToList();
    }

    /// <summary>Returns a lead with its activity log, or null.</summary>
    public async Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var notes = await _repository.ListNotesAsync(id, cancellationToken);
        return new LeadDetailDto(ToDto(lead), notes.Select(ToNoteDto).ToList());
    }

    /// <summary>Creates a lead and returns it.</summary>
    public async Task<LeadDto> CreateAsync(CreateLeadRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        var lead = new Lead
        {
            CompanyName = RequireCompany(request.CompanyName),
            ContactName = Trim(request.ContactName),
            ContactEmail = ValidatedEmail(request.ContactEmail),
            Location = Trim(request.Location),
            Model = ParseModel(request.Model),
            NumberOfSites = request.NumberOfSites,
            EstimatedRevenue = request.EstimatedRevenue,
            AccountManager = Trim(request.AccountManager),
            CompanyNumber = Trim(request.CompanyNumber),
            Source = Trim(request.Source) ?? "manual",
            Status = LeadStatus.New,
        };
        await _repository.AddAsync(lead, cancellationToken);
        await _repository.AddNoteAsync(SystemNote(lead.Id, actor, "Lead created."), cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Updates a lead's editable fields.</summary>
    public async Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        lead.CompanyName = RequireCompany(request.CompanyName);
        lead.ContactName = Trim(request.ContactName);
        lead.ContactEmail = ValidatedEmail(request.ContactEmail);
        lead.Location = Trim(request.Location);
        lead.Model = ParseModel(request.Model);
        lead.NumberOfSites = request.NumberOfSites;
        lead.EstimatedRevenue = request.EstimatedRevenue;
        lead.AccountManager = Trim(request.AccountManager);
        lead.CompanyNumber = Trim(request.CompanyNumber);
        lead.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(lead, cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Moves a lead to a new stage and records an automatic activity note.</summary>
    public async Task<LeadDto?> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var newStatus = ParseStatus(request.Status);
        if (newStatus == lead.Status)
        {
            return ToDto(lead);
        }

        var label = $"{lead.Status} → {newStatus}";
        lead.Status = newStatus;
        lead.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(lead, cancellationToken);
        await _repository.AddNoteAsync(SystemNote(lead.Id, actor, $"Status changed: {label}", label), cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Adds an activity-log note.</summary>
    public async Task<LeadNoteDto?> AddNoteAsync(Guid id, AddLeadNoteRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var body = (request.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            throw new ArgumentException("A note body is required.", nameof(request));
        }

        var note = new LeadNote { LeadId = id, Author = Trim(actor), Body = body };
        await _repository.AddNoteAsync(note, cancellationToken);

        // Touch the lead so it sorts to the top of the recently-active list.
        lead.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(lead, cancellationToken);
        return ToNoteDto(note);
    }

    /// <summary>Converts a lead into an account (sets Converted, links the account, logs it).</summary>
    public async Task<LeadDto?> ConvertAsync(Guid id, ConvertLeadRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        // Manual assignment wins; otherwise try to match the account by the lead's company (registration) number.
        var accountId = request.AccountId;
        var matched = false;
        if (accountId is null && !string.IsNullOrWhiteSpace(lead.CompanyNumber))
        {
            var company = await _companies.GetByRegistrationNumberAsync(lead.CompanyNumber!, cancellationToken);
            if (company is not null)
            {
                accountId = company.Id;
                matched = true;
            }
        }

        lead.Status = LeadStatus.Converted;
        lead.ConvertedAccountId = accountId;
        lead.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(lead, cancellationToken);

        var detail = accountId is { } acc
            ? matched ? $" Auto-matched to account {acc} by company number." : $" Account {acc}."
            : string.Empty;
        await _repository.AddNoteAsync(SystemNote(lead.Id, actor, $"Converted to account.{detail}", "Converted"), cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Attributes a lead to an affiliate (or clears it) and logs it.</summary>
    public async Task<LeadDto?> AssignAffiliateAsync(Guid id, AssignLeadAffiliateRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.GetAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        lead.AffiliateId = request.AffiliateId;
        lead.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(lead, cancellationToken);

        var body = request.AffiliateId is { } aff ? $"Attributed to affiliate {aff}." : "Affiliate attribution cleared.";
        await _repository.AddNoteAsync(SystemNote(lead.Id, actor, body), cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Creates a lead from an anonymous marketing-site capture, deduplicating against an open lead.</summary>
    public async Task<LeadDto> CaptureAsync(CaptureLeadRequest request, CancellationToken cancellationToken = default)
    {
        var company = RequireCompany(request.CompanyName);
        var email = ValidatedEmail(request.ContactEmail);

        var existing = await _repository.FindOpenByCompanyAndEmailAsync(company, email, cancellationToken);
        if (existing is not null)
        {
            // Don't create a duplicate — log the fresh touch against the open lead instead.
            var touch = string.IsNullOrWhiteSpace(request.Note)
                ? $"New inbound activity ({Trim(request.Source) ?? "web"})."
                : request.Note!.Trim();
            await _repository.AddNoteAsync(SystemNote(existing.Id, null, touch), cancellationToken);
            existing.UpdatedUtc = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(existing, cancellationToken);
            return ToDto(existing);
        }

        var lead = new Lead
        {
            CompanyName = company,
            ContactName = Trim(request.ContactName),
            ContactEmail = email,
            Model = ParseModel(request.Model),
            NumberOfSites = request.NumberOfSites,
            Source = Trim(request.Source) ?? "web",
            Status = LeadStatus.New,
        };
        await _repository.AddAsync(lead, cancellationToken);

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? $"Lead captured from {lead.Source}."
            : request.Note!.Trim();
        await _repository.AddNoteAsync(SystemNote(lead.Id, null, note), cancellationToken);
        return ToDto(lead);
    }

    /// <summary>Builds an activity-log note, defaulting a missing author to the system.</summary>
    private static LeadNote SystemNote(Guid leadId, string? actor, string body, string? statusChange = null) =>
        new() { LeadId = leadId, Author = Trim(actor) ?? "System", Body = body, StatusChange = statusChange };

    private static string RequireCompany(string? company)
    {
        var value = (company ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException("A company name is required.");
        }

        return value;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Trims an optional email and rejects it when present but not a valid address.</summary>
    private static string? ValidatedEmail(string? value)
    {
        var email = Trim(value);
        if (email is not null && !EmailValidation.IsValid(email))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        return email;
    }

    private static LeadModel ParseModel(string? value) =>
        Enum.TryParse<LeadModel>(value, ignoreCase: true, out var model) ? model : LeadModel.Unknown;

    private static LeadStatus ParseStatus(string? value) =>
        Enum.TryParse<LeadStatus>(value, ignoreCase: true, out var status)
            ? status
            : throw new ArgumentException($"Unknown lead status '{value}'.");

    /// <summary>Maps a lead entity to its DTO (enums as their names).</summary>
    private static LeadDto ToDto(Lead l) => new(
        l.Id, l.CompanyName, l.ContactName, l.ContactEmail, l.Location, l.Model.ToString(),
        l.NumberOfSites, l.EstimatedRevenue, l.Status.ToString(), l.AccountManager, l.CompanyNumber,
        l.AffiliateId, l.ConvertedAccountId, l.Source, l.CreatedUtc, l.UpdatedUtc);

    /// <summary>Maps a note entity to its DTO.</summary>
    private static LeadNoteDto ToNoteDto(LeadNote n) => new(n.Id, n.Author, n.Body, n.StatusChange, n.CreatedUtc);
}
