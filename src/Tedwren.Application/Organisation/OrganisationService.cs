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
    private readonly ICompanyDocumentRepository _documents;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly IQualificationCardRepository _cards;

    /// <summary>How soon before expiry a document is flagged as at risk.</summary>
    private const int DocumentExpiryWarningDays = 30;

    /// <summary>Creates the service over its repositories.</summary>
    public OrganisationService(
        ICompanyRepository companies,
        ICompanyDocumentRepository documents,
        IPersonRepository people,
        IEngagementRepository engagements,
        IQualificationCardRepository cards)
    {
        _companies = companies;
        _documents = documents;
        _people = people;
        _engagements = engagements;
        _cards = cards;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>Returns every company as a list-row summary, with its active operative count.</summary>
    public async Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var summaries = new List<CompanySummary>(companies.Count);
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            var (state, percent) = await ComputeCompanyComplianceAsync(engagements, cancellationToken);
            summaries.Add(new CompanySummary(
                company.Id, Slug.From(company.Name), company.Name, company.Type, company.Trade,
                engagements.Count, percent, state, ComplianceRollup.Label(state)));
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
        var operatives = new List<CompanyOperativeDto>(engagements.Count);
        var allCurrentCards = new List<QualificationCard>();
        foreach (var e in engagements)
        {
            var current = await GetCurrentCardsAsync(e.PersonId, cancellationToken);
            allCurrentCards.AddRange(current);
            var (opState, _) = ComplianceRollup.FromCards(current, Today);
            operatives.Add(new CompanyOperativeDto(
                e.Id, e.PersonId, Slug.From(e.Name), e.Name, e.Trade, opState, ComplianceRollup.Label(opState)));
        }

        var (companyState, companyPercent) = ComplianceRollup.FromCards(allCurrentCards, Today);

        var documents = await _documents.GetByCompanyAsync(company.Id, cancellationToken);
        var documentDtos = documents.Select(ToDocumentDto).ToList();

        return new CompanyDetailDto(
            company.Id,
            Slug.From(company.Name),
            company.Name,
            company.Type,
            company.Trade,
            companyState,
            ComplianceRollup.Label(companyState),
            CompliancePercent: companyPercent,
            company.RegistrationNumber,
            company.Address,
            company.ContactName,
            company.ContactEmail,
            company.ContactPhone,
            Documents: documentDtos,
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

    /// <summary>Adds a company-held document (insurance, accreditation or policy) and returns its new id (SUB-4).</summary>
    public async Task<Guid> AddCompanyDocumentAsync(CreateCompanyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A document name is required.", nameof(request));
        }

        var document = new CompanyDocument
        {
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Document" : request.Type.Trim(),
            ExpiresOn = request.ExpiresOn,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
        };

        await _documents.AddAsync(document, cancellationToken);
        return document.Id;
    }

    /// <summary>Maps a company document to its DTO, deriving a compliance state from its expiry (SUB-4).</summary>
    private static CompanyDocumentDto ToDocumentDto(CompanyDocument document)
    {
        var state = DocumentState(document.ExpiresOn);
        return new CompanyDocumentDto(document.Name, document.Type, state, ComplianceRollup.Label(state), document.ExpiresOn);
    }

    /// <summary>Derives a document's compliance state from its expiry: expired, at risk near expiry, else compliant.</summary>
    private static ComplianceState DocumentState(DateOnly? expiresOn)
    {
        if (expiresOn is not { } expiry)
        {
            return ComplianceState.Pending;
        }

        if (expiry < Today)
        {
            return ComplianceState.NonCompliant;
        }

        return expiry <= Today.AddDays(DocumentExpiryWarningDays) ? ComplianceState.AtRisk : ComplianceState.Compliant;
    }

    /// <summary>Updates an existing company's editable fields. Returns false when the company is not found.</summary>
    public async Task<bool> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companies.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return false;
        }

        company.Name = request.Name;
        company.Type = request.Type;
        company.Trade = request.Trade;
        company.RegistrationNumber = request.RegistrationNumber;
        company.Address = request.Address;
        company.ContactName = request.ContactName;
        company.ContactEmail = request.ContactEmail;
        company.ContactPhone = request.ContactPhone;
        await _companies.UpdateAsync(company, cancellationToken);
        return true;
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

    /// <summary>Returns a person's current (non-superseded) cards — the input to the compliance roll-up (SF-8/SF-10).</summary>
    private async Task<IReadOnlyList<QualificationCard>> GetCurrentCardsAsync(Guid personId, CancellationToken cancellationToken)
    {
        var cards = await _cards.GetByPersonAsync(personId, cancellationToken);
        return cards.Where(c => !c.IsSuperseded).ToList();
    }

    /// <summary>Aggregates a company's active operatives' current cards into one compliance state and percentage.</summary>
    private async Task<(ComplianceState State, double? Percent)> ComputeCompanyComplianceAsync(
        IReadOnlyList<Engagement> engagements, CancellationToken cancellationToken)
    {
        var allCurrent = new List<QualificationCard>();
        foreach (var e in engagements)
        {
            allCurrent.AddRange(await GetCurrentCardsAsync(e.PersonId, cancellationToken));
        }

        return ComplianceRollup.FromCards(allCurrent, Today);
    }
}
