using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using AbstractionsVerificationState = Tedwren.Abstractions.Common.CardVerificationState;
using DomainVerificationState = Tedwren.Domain.Enums.CardVerificationState;

namespace Tedwren.Application.Qualifications;

/// <summary>
/// The single implementation of the qualification-card and competency rules. Data-store agnostic: the
/// same logic runs over the in-memory repositories (API mock mode) and the Dapper repositories (database
/// mode). Card status is always computed from the expiry date here (SF-8), never taken from storage.
/// </summary>
public sealed class QualificationService : IQualificationService
{
    private readonly IQualificationTypeRepository _types;
    private readonly IQualificationCardRepository _cards;
    private readonly ITradeRequirementRepository _requirements;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over its repositories. <paramref name="currentUser"/> is optional (supplied by DI) so the
    /// platform-admin master-data writes are scoped to the signed-in identity (Q21, R15); when it is absent (direct
    /// construction in unit tests) the caller is treated as an unscoped platform administrator.
    /// </summary>
    public QualificationService(
        IQualificationTypeRepository types,
        IQualificationCardRepository cards,
        ITradeRequirementRepository requirements,
        ICurrentUserService? currentUser = null)
    {
        _types = types;
        _cards = cards;
        _requirements = requirements;
        _currentUser = currentUser;
    }

    /// <summary>Returns the qualification-type library with each type's current holder count (SF-12).</summary>
    public async Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _types.GetAllAsync(cancellationToken);
        var counts = await _cards.GetHeldByCountsAsync(cancellationToken);
        return types.Select(t => ToDto(t, counts)).ToList();
    }

    /// <summary>Returns the cards held by a person, each mapped with its server-computed status (SF-7/SF-8).</summary>
    public async Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetByPersonAsync(personId, cancellationToken);
        if (cards.Count == 0)
        {
            return Array.Empty<QualificationCardDto>();
        }

        var types = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id);
        var today = Today();
        return cards
            .Select(card =>
            {
                types.TryGetValue(card.QualificationTypeId, out var type);
                return ToDto(card, type, today);
            })
            .ToList();
    }

    /// <summary>Captures a new card (SF-5) in the read-but-unchecked state; never confirmed automatically.</summary>
    public async Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default)
    {
        // Idempotency for the operative app's at-least-once outbox (R4/R16): a repeat of the same client-generated
        // capture returns the card already stored instead of creating a duplicate. Console captures pass no client id.
        if (request.CaptureClientId is { } clientId)
        {
            var existing = await _cards.GetByPersonAsync(request.PersonId, cancellationToken);
            var already = existing.FirstOrDefault(c => c.CaptureClientId == clientId);
            if (already is not null)
            {
                return already.Id;
            }
        }

        var card = new QualificationCard
        {
            PersonId = request.PersonId,
            QualificationTypeId = request.QualificationTypeId,
            CardNumber = request.CardNumber,
            HolderName = request.HolderName,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            NeedsReview = request.NeedsReview,
            ImageReference = request.ImageReference,
            CaptureClientId = request.CaptureClientId,
            VerificationState = DomainVerificationState.ReadUnchecked,
        };

        await _cards.AddAsync(card, cancellationToken);
        return card.Id;
    }

    /// <summary>Confirms a card by a named person, recording who and when and clearing review (SF-6).</summary>
    public async Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null)
        {
            return false;
        }

        card.VerificationState = DomainVerificationState.CustomerChecked;
        card.ConfirmedBy = request.ConfirmedBy;
        card.ConfirmedUtc = DateTimeOffset.UtcNow;
        card.NeedsReview = false;
        await _cards.UpdateAsync(card, cancellationToken);
        return true;
    }

    /// <summary>
    /// Renews a card: creates a new card for the same person and type that supersedes the old one, and
    /// marks the old card superseded. The old record is retained, never overwritten (SF-10).
    /// </summary>
    public async Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default)
    {
        var old = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (old is null)
        {
            return null;
        }

        var renewed = new QualificationCard
        {
            PersonId = old.PersonId,
            QualificationTypeId = old.QualificationTypeId,
            CardNumber = request.CardNumber ?? old.CardNumber,
            HolderName = old.HolderName,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            SupersedesCardId = old.Id,
            VerificationState = DomainVerificationState.ReadUnchecked,
        };
        await _cards.AddAsync(renewed, cancellationToken);

        old.SupersededByCardId = renewed.Id;
        await _cards.UpdateAsync(old, cancellationToken);
        return renewed.Id;
    }

    /// <summary>
    /// Returns the qualifications a person's trade requires but they do not currently hold (SF-11) — every
    /// unsatisfied requirement, mandatory or advisory. Computed from the shared <see cref="Gate3Evaluator"/>.
    /// </summary>
    public async Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var gate = await EvaluateGate3Async(personId, trade, companyId, cancellationToken);
        var missing = gate.Requirements.Where(r => !r.Satisfied).Select(r => r.Accreditation).ToList();
        return new CompetencyShortfallDto(personId, trade, missing);
    }

    /// <summary>
    /// Evaluates Gate 3 for an operative (Subcontractor Onboarding spec §2, SF-11): whether every legally-mandatory
    /// accreditation the trade requires is held on a current, in-date card. Reads global requirements plus the
    /// company's own (Q21); delegates the decision to the pure <see cref="Gate3Evaluator"/>.
    /// </summary>
    public async Task<Gate3StatusDto> EvaluateGate3Async(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var requirements = await _requirements.GetByTradeAsync(trade, companyId, cancellationToken);
        var cards = await _cards.GetByPersonAsync(personId, cancellationToken);
        var types = await _types.GetAllAsync(cancellationToken);
        return Gate3Evaluator.Evaluate(requirements, cards, types, Today());
    }

    // ---- Master-data management (SF-12 type library + SF-11 trade→accreditation map; Q21, R15) ----
    // Ownership mirrors MasterDataService: the platform administrator owns the shared (global) rows every tenant
    // inherits, while a main contractor sees those plus its own org-scoped custom entries and may add more, but may
    // never edit a shared row. Enforced here (not at the route) so the client cannot bypass it.

    /// <summary>Returns the qualification types the caller may manage: the shared platform rows plus the caller's own org-custom types (Q21).</summary>
    public async Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesForManagementAsync(CancellationToken cancellationToken = default)
    {
        var company = await CallerCompanyAsync(cancellationToken);
        var counts = await _cards.GetHeldByCountsAsync(cancellationToken);
        var types = await _types.GetAllAsync(cancellationToken);
        return types
            .Where(t => t.CompanyId is null || t.CompanyId == company)
            .Select(t => ToDto(t, counts))
            .ToList();
    }

    /// <summary>Adds a qualification type: a platform admin owns a shared (global) type; a tenant an org-custom one (Q21, R15).</summary>
    public async Task<Guid> CreateQualificationTypeAsync(CreateQualificationTypeRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A name is required.", nameof(request));
        }

        var type = new QualificationType
        {
            Name = name,
            Category = Clean(request.Category),
            Issuer = Clean(request.Issuer),
            DefaultValidityMonths = request.DefaultValidityMonths,
            IsCscsVerifiable = request.IsCscsVerifiable,
            CompanyId = await OwnerCompanyForCreateAsync(request.Global, cancellationToken),
        };
        await _types.AddAsync(type, cancellationToken);
        return type.Id;
    }

    /// <summary>Updates a qualification type the caller owns (a shared row is platform-admin only, R15).</summary>
    public async Task UpdateQualificationTypeAsync(Guid id, UpdateQualificationTypeRequest request, CancellationToken cancellationToken = default)
    {
        var type = await _types.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The qualification type was not found.");
        await AuthorizeMutationAsync(type.CompanyId, cancellationToken);

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A name is required.", nameof(request));
        }

        type.Name = name;
        type.Category = Clean(request.Category);
        type.Issuer = Clean(request.Issuer);
        type.DefaultValidityMonths = request.DefaultValidityMonths;
        type.IsCscsVerifiable = request.IsCscsVerifiable;
        await _types.UpdateAsync(type, cancellationToken);
    }

    /// <summary>Deletes a qualification type the caller owns, provided nothing references it (guarded; SF-12).</summary>
    public async Task DeleteQualificationTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var type = await _types.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The qualification type was not found.");
        await AuthorizeMutationAsync(type.CompanyId, cancellationToken);

        var counts = await _cards.GetHeldByCountsAsync(cancellationToken);
        if (counts.TryGetValue(id, out var held) && held > 0)
        {
            throw new InvalidOperationException("This accreditation is held by operatives and cannot be deleted.");
        }

        var requirements = await _requirements.GetAllAsync(cancellationToken);
        if (requirements.Any(r => r.QualificationTypeId == id))
        {
            throw new InvalidOperationException("This accreditation is mapped to a trade and cannot be deleted — remove the mapping first.");
        }

        await _types.DeleteAsync(id, cancellationToken);
    }

    /// <summary>Returns the trade→accreditation map the caller may manage, each row resolved to its accreditation name (SF-11/Q21).</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirementDto>> GetTradeRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var company = await CallerCompanyAsync(cancellationToken);
        var requirements = await _requirements.GetForManagementAsync(company, cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        return requirements
            .Select(r => new TradeQualificationRequirementDto(
                r.Id, r.Trade, r.QualificationTypeId,
                typeNames.TryGetValue(r.QualificationTypeId, out var n) ? n : "Unknown accreditation",
                r.LegalMandatory, r.ClientRequired, r.CompanyId, r.CompanyId is null))
            .ToList();
    }

    /// <summary>Adds a trade→accreditation map row: a platform admin owns a shared (global) row; a tenant an org-custom one (Q21, R15).</summary>
    public async Task<Guid> CreateTradeRequirementAsync(CreateTradeRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var trade = (request.Trade ?? string.Empty).Trim();
        if (trade.Length == 0)
        {
            throw new ArgumentException("A trade is required.", nameof(request));
        }

        if (await _types.GetByIdAsync(request.QualificationTypeId, cancellationToken) is null)
        {
            throw new ArgumentException("The accreditation type was not found.", nameof(request));
        }

        var requirement = new TradeQualificationRequirement
        {
            Trade = trade,
            QualificationTypeId = request.QualificationTypeId,
            LegalMandatory = request.LegalMandatory,
            ClientRequired = request.ClientRequired,
            CompanyId = await OwnerCompanyForCreateAsync(request.Global, cancellationToken),
        };
        await _requirements.AddAsync(requirement, cancellationToken);
        return requirement.Id;
    }

    /// <summary>Updates a map row's flags the caller owns (a shared row is platform-admin only, R15).</summary>
    public async Task UpdateTradeRequirementAsync(Guid id, UpdateTradeRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The trade requirement was not found.");
        await AuthorizeMutationAsync(requirement.CompanyId, cancellationToken);

        requirement.LegalMandatory = request.LegalMandatory;
        requirement.ClientRequired = request.ClientRequired;
        await _requirements.UpdateAsync(requirement, cancellationToken);
    }

    /// <summary>Deletes a map row the caller owns (a shared row is platform-admin only, R15).</summary>
    public async Task DeleteTradeRequirementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The trade requirement was not found.");
        await AuthorizeMutationAsync(requirement.CompanyId, cancellationToken);
        await _requirements.DeleteAsync(id, cancellationToken);
    }

    /// <summary>The owning company for a new row: null (shared) for a platform admin who asked for global, else the caller's own company (Q21, R15).</summary>
    private async Task<Guid?> OwnerCompanyForCreateAsync(bool global, CancellationToken cancellationToken)
    {
        var (isPlatformAdmin, companyId) = await CallerAsync(cancellationToken);
        if (global)
        {
            if (!isPlatformAdmin)
            {
                throw new InvalidOperationException("Only a platform administrator can add to the shared library.");
            }

            return null;
        }

        return companyId ?? throw new InvalidOperationException("A signed-in company is required to add a custom entry.");
    }

    /// <summary>Checks the caller may mutate a row with the given owner: a shared (null) row is platform-admin only; an org row only its owner (R15).</summary>
    private async Task AuthorizeMutationAsync(Guid? ownerCompanyId, CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return;   // unscoped (unit tests) — no tenant restriction
        }

        var (isPlatformAdmin, companyId) = await CallerAsync(cancellationToken);
        if (isPlatformAdmin)
        {
            return;
        }

        if (ownerCompanyId is null)
        {
            throw new InvalidOperationException("Only a platform administrator can change the shared library.");
        }

        if (ownerCompanyId != companyId)
        {
            throw new InvalidOperationException("This entry belongs to another company.");
        }
    }

    /// <summary>The signed-in caller's rights, or an unscoped platform admin for direct construction (tests).</summary>
    private async Task<(bool IsPlatformAdmin, Guid? CompanyId)> CallerAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return (true, null);
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return (user.IsPlatformAdmin, user.CompanyId);
    }

    /// <summary>The caller's company for read-scoping (null for platform admin / unscoped tests → global rows only).</summary>
    private async Task<Guid?> CallerCompanyAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        return (await _currentUser.GetCurrentAsync(cancellationToken)).CompanyId;
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Maps a qualification type + holder counts to its DTO (IsGlobal derived from a null company).</summary>
    private static QualificationTypeDto ToDto(QualificationType t, IReadOnlyDictionary<Guid, int> counts) =>
        new(t.Id, t.Name, t.Category, t.Issuer, t.DefaultValidityMonths, t.IsCscsVerifiable,
            counts.TryGetValue(t.Id, out var held) ? held : 0, t.CompanyId, t.CompanyId is null);

    /// <summary>Today's date used for status computation (SF-8). UK-local per R11; card expiry is date-only.</summary>
    private static DateOnly Today() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>Maps a card + its type to the DTO, computing the status and labels (SF-7/SF-8).</summary>
    private static QualificationCardDto ToDto(QualificationCard card, QualificationType? type, DateOnly today)
    {
        var status = card.GetStatus(today);
        return new QualificationCardDto(
            card.Id,
            card.PersonId,
            card.QualificationTypeId,
            type?.Name ?? "Unknown qualification",
            type?.Issuer,
            card.CardNumber,
            card.HolderName,
            card.IssuedOn,
            card.ExpiresOn,
            ToState(status),
            StatusLabel(status),
            ToAbstraction(card.VerificationState),
            VerificationLabel(card.VerificationState),
            card.NeedsReview,
            card.ConfirmedBy,
            card.ConfirmedUtc,
            card.IsSuperseded,
            card.ImageReference);
    }

    /// <summary>Maps the computed card status to the neutral compliance state used on the pill.</summary>
    private static ComplianceState ToState(CardStatus status) => status switch
    {
        CardStatus.Valid => ComplianceState.Compliant,
        CardStatus.ExpiringSoon => ComplianceState.AtRisk,
        CardStatus.Expired => ComplianceState.NonCompliant,
        _ => ComplianceState.Pending,
    };

    /// <summary>Human-readable label for a computed card status.</summary>
    private static string StatusLabel(CardStatus status) => status switch
    {
        CardStatus.Valid => "Valid",
        CardStatus.ExpiringSoon => "Expiring soon",
        CardStatus.Expired => "Expired",
        _ => "Pending",
    };

    /// <summary>Maps the domain verification state to the neutral contract enum.</summary>
    private static AbstractionsVerificationState ToAbstraction(DomainVerificationState state) => state switch
    {
        DomainVerificationState.CustomerChecked => AbstractionsVerificationState.CustomerChecked,
        DomainVerificationState.CscsVerified => AbstractionsVerificationState.CscsVerified,
        _ => AbstractionsVerificationState.ReadUnchecked,
    };

    /// <summary>Human-readable label for a verification state, keeping the three states distinct (SF-7).</summary>
    private static string VerificationLabel(DomainVerificationState state) => state switch
    {
        DomainVerificationState.CustomerChecked => "Checked",
        DomainVerificationState.CscsVerified => "CSCS verified",
        _ => "Read — not checked",
    };
}
