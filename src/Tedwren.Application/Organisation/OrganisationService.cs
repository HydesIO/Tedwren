using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Audit;
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
    private readonly IAuditService? _audit;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>How soon before expiry a document is flagged as at risk.</summary>
    private const int DocumentExpiryWarningDays = 30;

    /// <summary>
    /// Creates the service over its repositories. <paramref name="audit"/> and <paramref name="currentUser"/>
    /// are optional so a mutation records an audit-trail entry (SF-20) attributed to the signed-in user when
    /// they are supplied by DI; they default to null so unit tests that construct the service directly still run.
    /// </summary>
    public OrganisationService(
        ICompanyRepository companies,
        ICompanyDocumentRepository documents,
        IPersonRepository people,
        IEngagementRepository engagements,
        IQualificationCardRepository cards,
        IAuditService? audit = null,
        ICurrentUserService? currentUser = null)
    {
        _companies = companies;
        _documents = documents;
        _people = people;
        _engagements = engagements;
        _cards = cards;
        _audit = audit;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Records an audit-trail entry for a mutation (SF-20), attributed to the signed-in user (or "System").
    /// Best-effort: an audit-write failure never fails the operation it records. No-ops when no audit service
    /// is wired (direct unit-test construction).
    /// </summary>
    private async Task AuditAsync(Guid companyId, string action, string entity, string? reference, string category, CancellationToken cancellationToken)
    {
        if (_audit is null)
        {
            return;
        }

        try
        {
            var actor = _currentUser is not null ? (await _currentUser.GetCurrentAsync(cancellationToken)).Name : "System";
            await _audit.RecordAsync(new RecordAuditRequest(companyId, actor, action, entity, reference, category), cancellationToken);
        }
        catch
        {
            // Audit is a side effect of the operation, not a precondition — never let it break the mutation.
        }
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
                engagements.Count, percent, state, ComplianceRollup.Label(state), ToDtoOrgType(company.OrgType)));
        }

        return summaries;
    }

    /// <summary>Returns the full company for a slug, or null when no company matches.</summary>
    public async Task<CompanyDetailDto?> GetCompanyAsync(string slug, CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        // Resolve by the stable id when the route token is a Guid (collision-proof), else fall back to the
        // name slug. Two companies whose names slugify identically no longer collide — one becoming
        // unreachable or the wrong record opening — because the list now links by id (F15).
        var company = Guid.TryParse(slug, out var companyId)
            ? companies.FirstOrDefault(c => c.Id == companyId)
            : companies.FirstOrDefault(c => Slug.From(c.Name) == slug);
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

        // The library shows the current version of each document; superseded versions are retained but hidden
        // here and reachable via the version history (MC-27).
        var documents = await _documents.GetByCompanyAsync(company.Id, cancellationToken);
        var documentDtos = documents.Where(d => !d.IsSuperseded).Select(ToDocumentDto).ToList();

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
            Operatives: operatives,
            OrgType: ToDtoOrgType(company.OrgType));
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
            OrgType = ToDomainOrgType(request.OrgType),
        };

        await _companies.AddAsync(company, cancellationToken);
        await AuditAsync(company.Id, "Company created", company.Name, company.RegistrationNumber, "Organisation", cancellationToken);
        return company.Id;
    }

    /// <summary>Maps the domain product enum to the DTO/boundary enum (the client references only Abstractions).</summary>
    private static Abstractions.Common.OrgType? ToDtoOrgType(Domain.Enums.OrgType? orgType) => orgType switch
    {
        Domain.Enums.OrgType.Subcontractor => Abstractions.Common.OrgType.Subcontractor,
        Domain.Enums.OrgType.MainContractor => Abstractions.Common.OrgType.MainContractor,
        _ => null,
    };

    /// <summary>Maps the DTO/boundary product enum back to the domain enum for persistence.</summary>
    private static Domain.Enums.OrgType? ToDomainOrgType(Abstractions.Common.OrgType? orgType) => orgType switch
    {
        Abstractions.Common.OrgType.Subcontractor => Domain.Enums.OrgType.Subcontractor,
        Abstractions.Common.OrgType.MainContractor => Domain.Enums.OrgType.MainContractor,
        _ => null,
    };

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
        await AuditAsync(request.CompanyId, "Document added", document.Name, document.Reference, "Documents", cancellationToken);
        return document.Id;
    }

    /// <summary>Supersedes a document with a new version, retaining the prior one (MC-27). Scoped to the company (R15).</summary>
    public async Task<Guid?> SupersedeCompanyDocumentAsync(SupersedeCompanyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A document name is required.", nameof(request));
        }

        var existing = await _documents.GetAsync(request.DocumentId, cancellationToken);
        if (existing is null || existing.CompanyId != request.CompanyId)
        {
            return null;   // R15 — missing or cross-tenant
        }

        // Always supersede the current head of the chain, even if an older version was passed in.
        var head = await ResolveHeadAsync(existing, cancellationToken);

        var newVersion = new CompanyDocument
        {
            CompanyId = head.CompanyId,
            Name = request.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? head.Type : request.Type.Trim(),
            ExpiresOn = request.ExpiresOn,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Version = head.Version + 1,
            SupersedesDocumentId = head.Id,
        };
        await _documents.AddAsync(newVersion, cancellationToken);

        head.SupersededByDocumentId = newVersion.Id;
        await _documents.UpdateAsync(head, cancellationToken);

        await AuditAsync(head.CompanyId, "Document re-versioned", newVersion.Name, newVersion.Reference, "Documents", cancellationToken);
        return newVersion.Id;
    }

    /// <summary>Returns a document's full version chain (oldest first), scoped to the company (R15) (MC-27).</summary>
    public async Task<IReadOnlyList<CompanyDocumentDto>> GetCompanyDocumentVersionsAsync(Guid companyId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documents.GetAsync(documentId, cancellationToken);
        if (document is null || document.CompanyId != companyId)
        {
            return Array.Empty<CompanyDocumentDto>();   // R15
        }

        // Walk back to the root (oldest) then forward to the head, collecting the whole chain.
        var root = document;
        while (root.SupersedesDocumentId is { } prevId)
        {
            var prev = await _documents.GetAsync(prevId, cancellationToken);
            if (prev is null || prev.CompanyId != companyId)
            {
                break;
            }

            root = prev;
        }

        var chain = new List<CompanyDocument> { root };
        var current = root;
        while (current.SupersededByDocumentId is { } nextId)
        {
            var next = await _documents.GetAsync(nextId, cancellationToken);
            if (next is null || next.CompanyId != companyId)
            {
                break;
            }

            chain.Add(next);
            current = next;
        }

        return chain.OrderBy(d => d.Version).Select(ToDocumentDto).ToList();
    }

    /// <summary>Walks the supersede chain forward to its current head (MC-27), staying within the company (R15).</summary>
    private async Task<CompanyDocument> ResolveHeadAsync(CompanyDocument document, CancellationToken cancellationToken)
    {
        var head = document;
        while (head.SupersededByDocumentId is { } nextId)
        {
            var next = await _documents.GetAsync(nextId, cancellationToken);
            if (next is null || next.CompanyId != head.CompanyId)
            {
                break;
            }

            head = next;
        }

        return head;
    }

    /// <summary>Maps a company document to its DTO, deriving a compliance state from its expiry (SUB-4) and carrying its version (MC-27).</summary>
    private static CompanyDocumentDto ToDocumentDto(CompanyDocument document)
    {
        var state = DocumentState(document.ExpiresOn);
        return new CompanyDocumentDto(
            document.Id, document.Name, document.Type, state, ComplianceRollup.Label(state), document.ExpiresOn,
            document.Version, document.IsSuperseded);
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
        company.OrgType = ToDomainOrgType(request.OrgType);
        await _companies.UpdateAsync(company, cancellationToken);
        await AuditAsync(company.Id, "Company updated", company.Name, company.RegistrationNumber, "Organisation", cancellationToken);
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
            await AuditAsync(request.CompanyId, "Operative reactivated", existing.Name, existing.InternalReference, "Workforce", cancellationToken);
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
        await AuditAsync(request.CompanyId, "Operative added", engagement.Name, engagement.InternalReference, "Workforce", cancellationToken);
        return new AddOperativeResult(true, engagement.Id, person.Id, null);
    }

    /// <summary>
    /// Updates an operative's engagement name + trade (SF-2), tenant-scoped by company (R15). Refuses when the
    /// engagement is not found, or when a different active engagement in the same company already uses the
    /// target name — keeping operative names distinct within a company.
    /// </summary>
    public async Task<bool> UpdateEngagementAsync(Guid companyId, Guid engagementId, UpdateEngagementRequest request, CancellationToken cancellationToken = default)
    {
        var engagement = await _engagements.GetAsync(companyId, engagementId, cancellationToken);
        if (engagement is null)
        {
            return false;
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var active = await _engagements.GetActiveByCompanyAsync(companyId, cancellationToken);
        if (active.Any(e => e.Id != engagementId && string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        engagement.Name = name;
        engagement.Trade = string.IsNullOrWhiteSpace(request.Trade) ? null : request.Trade.Trim();
        await _engagements.UpdateAsync(engagement, cancellationToken);
        await AuditAsync(companyId, "Operative updated", engagement.Name, engagement.InternalReference, "Workforce", cancellationToken);
        return true;
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
        await AuditAsync(companyId, "Operative archived", engagement.Name, engagement.InternalReference, "Workforce", cancellationToken);
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
        await AuditAsync(companyId, "Operative reactivated", engagement.Name, engagement.InternalReference, "Workforce", cancellationToken);
        return true;
    }

    /// <summary>Updates a person's emergency contact (MC-2/UAT-010). Person-level, so it applies across every
    /// company that engages them. Returns false when the person is not found.</summary>
    public async Task<bool> UpdatePersonContactAsync(Guid personId, UpdatePersonContactRequest request, CancellationToken cancellationToken = default)
    {
        var person = await _people.GetByIdAsync(personId, cancellationToken);
        if (person is null)
        {
            return false;
        }

        person.EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim();
        person.EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim();
        await _people.UpdateAsync(person, cancellationToken);
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
