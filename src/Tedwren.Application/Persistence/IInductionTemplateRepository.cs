using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for induction templates (MC-3). Reads are company-scoped (R15).</summary>
public interface IInductionTemplateRepository
{
    /// <summary>Returns a company's induction templates.</summary>
    Task<IReadOnlyList<InductionTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a template by id, or null.</summary>
    Task<InductionTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new template.</summary>
    Task AddAsync(InductionTemplate template, CancellationToken cancellationToken = default);
}
