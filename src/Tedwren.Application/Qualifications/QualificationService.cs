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

    /// <summary>Creates the service over its repositories.</summary>
    public QualificationService(
        IQualificationTypeRepository types,
        IQualificationCardRepository cards,
        ITradeRequirementRepository requirements)
    {
        _types = types;
        _cards = cards;
        _requirements = requirements;
    }

    /// <summary>Returns the qualification-type library with each type's current holder count (SF-12).</summary>
    public async Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _types.GetAllAsync(cancellationToken);
        var counts = await _cards.GetHeldByCountsAsync(cancellationToken);
        return types
            .Select(t => new QualificationTypeDto(
                t.Id, t.Name, t.Category, t.Issuer, t.DefaultValidityMonths, t.IsCscsVerifiable,
                counts.TryGetValue(t.Id, out var held) ? held : 0))
            .ToList();
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
        var card = new QualificationCard
        {
            PersonId = request.PersonId,
            QualificationTypeId = request.QualificationTypeId,
            CardNumber = request.CardNumber,
            HolderName = request.HolderName,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            NeedsReview = request.NeedsReview,
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
    /// Returns the qualifications a person's trade requires but they do not currently hold (SF-11): the
    /// trade's required types minus the types the person holds on a current (non-superseded, in-date) card.
    /// </summary>
    public async Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default)
    {
        var required = await _requirements.GetByTradeAsync(trade, cancellationToken);
        if (required.Count == 0)
        {
            return new CompetencyShortfallDto(personId, trade, Array.Empty<string>());
        }

        var today = Today();
        var cards = await _cards.GetByPersonAsync(personId, cancellationToken);
        var heldTypeIds = cards
            .Where(c => !c.IsSuperseded && c.GetStatus(today) != CardStatus.Expired)
            .Select(c => c.QualificationTypeId)
            .ToHashSet();

        var missingTypeIds = required
            .Select(r => r.QualificationTypeId)
            .Where(id => !heldTypeIds.Contains(id))
            .Distinct()
            .ToList();

        var types = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id);
        var missingNames = missingTypeIds
            .Select(id => types.TryGetValue(id, out var t) ? t.Name : id.ToString())
            .ToList();

        return new CompetencyShortfallDto(personId, trade, missingNames);
    }

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
            card.IsSuperseded);
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
