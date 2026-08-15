using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for sales leads and their activity log (commercial database).</summary>
public interface ILeadRepository
{
    /// <summary>Persists a new lead.</summary>
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>Returns a lead by id, or null.</summary>
    Task<Lead?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns every lead, newest-updated first.</summary>
    Task<IReadOnlyList<Lead>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the leads attributed to an affiliate, newest-updated first.</summary>
    Task<IReadOnlyList<Lead>> ListByAffiliateAsync(Guid affiliateId, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent open (non-terminal) lead matching a company + email, or null (dedupe for capture).</summary>
    Task<Lead?> FindOpenByCompanyAndEmailAsync(string companyName, string? email, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing lead.</summary>
    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>Appends a note to a lead's activity log.</summary>
    Task AddNoteAsync(LeadNote note, CancellationToken cancellationToken = default);

    /// <summary>Returns a lead's activity log, newest first.</summary>
    Task<IReadOnlyList<LeadNote>> ListNotesAsync(Guid leadId, CancellationToken cancellationToken = default);
}
