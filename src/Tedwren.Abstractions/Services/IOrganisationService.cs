using Tedwren.Abstractions.Contracts.Organisation;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The organisation (companies + people + engagements) service contract, shared by the client and the
/// API so the UI and business logic are identical whether data is served from mock or from the
/// database. Company reads/writes back the Organisation screens; the operative methods encode the
/// shared-foundation identity rules (SF-1/SF-2/SF-3).
/// </summary>
public interface IOrganisationService
{
    /// <summary>Returns every company as a list-row summary.</summary>
    Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the full company for a slug, or null when no company matches.</summary>
    Task<CompanyDetailDto?> GetCompanyAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Creates a company and returns its new identifier.</summary>
    Task<Guid> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing company's details. Returns false when the company is not found.</summary>
    Task<bool> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a company-held document (insurance, accreditation or policy) and returns its new id (SUB-4).</summary>
    Task<Guid> AddCompanyDocumentAsync(CreateCompanyDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an operative to a company: reuses the existing person for that mobile number or creates one
    /// (SF-1), and refuses a second engagement of the same person in the same company (SF-2).
    /// </summary>
    Task<AddOperativeResult> AddOperativeAsync(AddOperativeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an operative's per-company engagement details (name + trade, SF-2). Returns false when the
    /// engagement is not found for that company, or when another active engagement in the company already
    /// uses the target name (names stay distinct within a company).
    /// </summary>
    Task<bool> UpdateEngagementAsync(Guid companyId, Guid engagementId, UpdateEngagementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Archives an engagement owned by the given company (SF-3). Returns false if not found.</summary>
    Task<bool> ArchiveEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default);

    /// <summary>Reactivates an archived engagement owned by the given company (SF-3). Returns false if not found.</summary>
    Task<bool> ReactivateEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default);
}
