using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="CompanyDocument"/> (SUB-4). Implemented in-memory and with Dapper.</summary>
public interface ICompanyDocumentRepository
{
    /// <summary>Returns a company's documents, most-recently-created first.</summary>
    Task<IReadOnlyList<CompanyDocument>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single document by id, or null if none exists (MC-27 versioning).</summary>
    Task<CompanyDocument?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new company document.</summary>
    Task AddAsync(CompanyDocument document, CancellationToken cancellationToken = default);

    /// <summary>Updates a document's mutable/versioning fields (MC-27: marking it superseded).</summary>
    Task UpdateAsync(CompanyDocument document, CancellationToken cancellationToken = default);
}
