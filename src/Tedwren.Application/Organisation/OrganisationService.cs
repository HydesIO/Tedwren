using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Organisation;

/// <summary>
/// The single implementation of the organisation business rules. It is data-store agnostic: the same
/// logic runs over in-memory repositories (API mock mode) and Dapper repositories (database mode), so
/// the mock/database switch never changes behaviour. Compliance is reported as
/// <see cref="ComplianceState.Pending"/> until cards exist (Phase 9), never invented.
/// </summary>
public sealed class OrganisationService : IOrganisationService
{
    private readonly ICompanyRepository _companies;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;

    /// <summary>Creates the service over its repositories.</summary>
    public OrganisationService(
        ICompanyRepository companies,
        IPersonRepository people,
        IEngagementRepository engagements)
    {
        _companies = companies;
        _people = people;
        _engagements = engagements;
    }

    /// <summary>Returns every company as a list-row summary, with its active operative count.</summary>
    public async Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var summaries = new List<CompanySummary>(companies.Count);
        foreach (var company in companies)
        {
            var operatives = await _engagements.CountActiveByCompanyAsync(company.Id, cancellationToken);
            summaries.Add(ToSummary(company, operatives));
        }

        return summaries;
    }

    /// <summary>Returns the full company for a slug, or null when no company matches.</summary>
    public async Task<CompanyDetailDto?> GetCompanyAsync(string slug, CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var company = companies.FirstOrDefault(c => Slug.From(c.Name) == slug);
        if (company is null)
        {
            return null;
        }

        var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
        var operatives = engagements
            .Select(e => new CompanyOperativeDto(
                e.Id, Slug.From(e.Name), e.Name, e.Trade, ComplianceState.Pending, "Pending"))
            .ToList();

        return new CompanyDetailDto(
            company.Id,
            Slug.From(company.Name),
            company.Name,
            company.Type,
            company.Trade,
            ComplianceState.Pending,
            "Pending",
            CompliancePercent: null,
            company.RegistrationNumber,
            company.Address,
            company.ContactName,
            company.ContactEmail,
            company.ContactPhone,
            Documents: Array.Empty<CompanyDocumentDto>(),
            Operatives: operatives);
    }

    /// <summary>Creates a company and returns its new identifier.</summary>
    public async Task<Guid> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = new Company
        {
            Name = request.Name,
            Type = request.Type,
            Trade = request.Trade,
            RegistrationNumber = request.RegistrationNumber,
            Address = request.Address,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
        };

        await _companies.AddAsync(company, cancellationToken);
        return company.Id;
    }

    /// <summary>
    /// Adds an operative to a company: reuses (or creates) the person for the mobile number (SF-1), and
    /// enforces one engagement per person per company — an active duplicate is refused naming the
    /// existing record (SF-2), while an archived one is reactivated rather than duplicated (SF-3).
    /// </summary>
    public async Task<AddOperativeResult> AddOperativeAsync(AddOperativeRequest request, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryParse(request.MobileNumber, out var phone))
        {
            return new AddOperativeResult(false, null, null, $"'{request.MobileNumber}' is not a usable mobile number.");
        }

        var person = await _people.GetByPhoneAsync(phone!, cancellationToken);
        if (person is null)
        {
            person = new Person { PhoneNumber = phone! };
            await _people.AddAsync(person, cancellationToken);
        }

        var existing = await _engagements.GetByCompanyAndPersonAsync(request.CompanyId, person.Id, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == EngagementStatus.Active)
            {
                return new AddOperativeResult(false, existing.Id, person.Id, $"Already engaged in this company as \"{existing.Name}\".");
            }

            existing.Status = EngagementStatus.Active;
            existing.ArchivedUtc = null;
            await _engagements.UpdateAsync(existing, cancellationToken);
            return new AddOperativeResult(true, existing.Id, person.Id, null);
        }

        var engagement = new Engagement
        {
            CompanyId = request.CompanyId,
            PersonId = person.Id,
            Name = request.Name,
            Trade = request.Trade,
            InternalReference = request.InternalReference,
        };

        await _engagements.AddAsync(engagement, cancellationToken);
        return new AddOperativeResult(true, engagement.Id, person.Id, null);
    }

    /// <summary>Archives an engagement owned by the company (SF-3). Returns false if not found for that company.</summary>
    public async Task<bool> ArchiveEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        var engagement = await _engagements.GetAsync(companyId, engagementId, cancellationToken);
        if (engagement is null)
        {
            return false;
        }

        engagement.Status = EngagementStatus.Archived;
        engagement.ArchivedUtc = DateTimeOffset.UtcNow;
        await _engagements.UpdateAsync(engagement, cancellationToken);
        return true;
    }

    /// <summary>Reactivates an archived engagement owned by the company (SF-3). Returns false if not found.</summary>
    public async Task<bool> ReactivateEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        var engagement = await _engagements.GetAsync(companyId, engagementId, cancellationToken);
        if (engagement is null)
        {
            return false;
        }

        engagement.Status = EngagementStatus.Active;
        engagement.ArchivedUtc = null;
        await _engagements.UpdateAsync(engagement, cancellationToken);
        return true;
    }

    /// <summary>Maps a company entity and its operative count to a list summary (compliance pending pre-Phase 9).</summary>
    private static CompanySummary ToSummary(Company company, int operatives) =>
        new(company.Id, Slug.From(company.Name), company.Name, company.Type, company.Trade,
            operatives, CompliancePercent: null, ComplianceState.Pending, "Pending");
}
