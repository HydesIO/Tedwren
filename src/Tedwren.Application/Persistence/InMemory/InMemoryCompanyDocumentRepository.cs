using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ICompanyDocumentRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryCompanyDocumentRepository : ICompanyDocumentRepository
{
    private readonly InMemoryOrganisationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryCompanyDocumentRepository(InMemoryOrganisationStore store) => _store = store;

    /// <summary>Returns a company's documents, most-recently-created first.</summary>
    public Task<IReadOnlyList<CompanyDocument>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CompanyDocument> documents = _store.CompanyDocuments.Values
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedUtc)
            .ToList();
        return Task.FromResult(documents);
    }

    /// <summary>Adds a document to the store.</summary>
    public Task AddAsync(CompanyDocument document, CancellationToken cancellationToken = default)
    {
        _store.CompanyDocuments[document.Id] = document;
        return Task.CompletedTask;
    }
}
