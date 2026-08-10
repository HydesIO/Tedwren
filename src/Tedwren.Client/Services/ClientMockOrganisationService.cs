using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Services;
using Tedwren.UiComponents.Models;
using Tedwren.UiComponents.SampleData;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IOrganisationService"/> for client <c>DataSource=Mock</c>. It wraps the existing
/// sample-data services and maps their records to the shared DTOs, so the Organisation screens look
/// exactly as they did before this phase. Writes are demo-only (the sample data is static), matching
/// the prior "nothing is persisted" behaviour.
/// </summary>
public sealed class ClientMockOrganisationService : IOrganisationService
{
    private readonly IListSampleDataService _list;
    private readonly IDetailSampleDataService _detail;

    /// <summary>Creates the service over the sample-data services.</summary>
    public ClientMockOrganisationService(IListSampleDataService list, IDetailSampleDataService detail)
    {
        _list = list;
        _detail = detail;
    }

    /// <summary>Maps the sample company list to summaries.</summary>
    public Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CompanySummary> companies = _list.GetCompanies()
            .Select(c => new CompanySummary(
                Guid.Empty, Slug.From(c.Name), c.Name, c.Type, c.Trade, c.Operatives,
                c.CompliancePercent, ToState(c.Status), c.StatusLabel))
            .ToList();
        return Task.FromResult(companies);
    }

    /// <summary>Maps a sample company detail to the DTO, or null when the slug is unknown.</summary>
    public Task<CompanyDetailDto?> GetCompanyAsync(string slug, CancellationToken cancellationToken = default)
    {
        var company = _detail.GetCompany(slug);
        if (company is null)
        {
            return Task.FromResult<CompanyDetailDto?>(null);
        }

        var documents = company.Documents
            .Select(d => new CompanyDocumentDto(d.Name, d.Type, ToState(d.Status), d.StatusLabel, d.ExpiresOn))
            .ToList();
        var operatives = company.Operatives
            .Select(o => new CompanyOperativeDto(Guid.Empty, Slug.From(o.Name), o.Name, o.Trade, ToState(o.Status), o.StatusLabel))
            .ToList();

        var dto = new CompanyDetailDto(
            Guid.Empty, slug, company.Name, company.Type, company.Trade, ToState(company.Status), company.StatusLabel,
            company.CompliancePercent, company.RegistrationNumber, company.Address, company.ContactName,
            company.ContactEmail, company.ContactPhone, documents, operatives);
        return Task.FromResult<CompanyDetailDto?>(dto);
    }

    /// <summary>Demo create — sample data is static, so nothing is persisted.</summary>
    public Task<Guid> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    /// <summary>Demo add — not persisted in mock mode.</summary>
    public Task<AddOperativeResult> AddOperativeAsync(AddOperativeRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AddOperativeResult(true, Guid.NewGuid(), Guid.NewGuid(), null));

    /// <summary>Demo archive — not persisted in mock mode.</summary>
    public Task<bool> ArchiveEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    /// <summary>Demo reactivate — not persisted in mock mode.</summary>
    public Task<bool> ReactivateEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    /// <summary>Maps the sample UI status kind to the neutral compliance state.</summary>
    private static ComplianceState ToState(StatusKind status) => status switch
    {
        StatusKind.Success => ComplianceState.Compliant,
        StatusKind.Warning => ComplianceState.AtRisk,
        StatusKind.Danger => ComplianceState.NonCompliant,
        _ => ComplianceState.Pending,
    };
}
