using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="ILeadRepository"/>.</summary>
public sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly ConcurrentDictionary<Guid, Lead> _leads = new();
    private readonly ConcurrentDictionary<Guid, LeadNote> _notes = new();

    /// <summary>Persists a new lead.</summary>
    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _leads[lead.Id] = lead;
        return Task.CompletedTask;
    }

    /// <summary>Returns a lead by id, or null.</summary>
    public Task<Lead?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_leads.GetValueOrDefault(id));

    /// <summary>Returns every lead, newest-updated first.</summary>
    public Task<IReadOnlyList<Lead>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Lead>>(_leads.Values.OrderByDescending(l => l.UpdatedUtc).ToList());

    /// <summary>Returns the leads attributed to an affiliate, newest-updated first.</summary>
    public Task<IReadOnlyList<Lead>> ListByAffiliateAsync(Guid affiliateId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Lead>>(
            _leads.Values.Where(l => l.AffiliateId == affiliateId).OrderByDescending(l => l.UpdatedUtc).ToList());

    /// <summary>Returns the most recent open lead matching a company + email, or null.</summary>
    public Task<Lead?> FindOpenByCompanyAndEmailAsync(string companyName, string? email, CancellationToken cancellationToken = default)
    {
        var match = _leads.Values
            .Where(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost)
            .Where(l => string.Equals(l.CompanyName, companyName, StringComparison.OrdinalIgnoreCase))
            .Where(l => string.Equals(l.ContactEmail ?? string.Empty, email ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.UpdatedUtc)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    /// <summary>Persists changes to an existing lead.</summary>
    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _leads[lead.Id] = lead;
        return Task.CompletedTask;
    }

    /// <summary>Appends a note to a lead's activity log.</summary>
    public Task AddNoteAsync(LeadNote note, CancellationToken cancellationToken = default)
    {
        _notes[note.Id] = note;
        return Task.CompletedTask;
    }

    /// <summary>Returns a lead's activity log, newest first.</summary>
    public Task<IReadOnlyList<LeadNote>> ListNotesAsync(Guid leadId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeadNote>>(
            _notes.Values.Where(n => n.LeadId == leadId).OrderByDescending(n => n.CreatedUtc).ToList());
}
