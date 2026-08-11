using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="Permit"/>. Reads are scoped by company (R15).</summary>
public interface IPermitRepository
{
    /// <summary>Persists a new permit.</summary>
    Task AddAsync(Permit permit, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's permits, newest first.</summary>
    Task<IReadOnlyList<Permit>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
