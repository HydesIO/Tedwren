using Tedwren.Abstractions.Contracts.Qualifications;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The qualification-card and competency service contract, shared by the client and the API so the UI
/// and business logic are identical whether data is served from mock or from the database. Encodes the
/// card lifecycle (capture SF-5, confirm SF-6, computed status SF-8, renewal SF-10), the default library
/// (SF-12) and trade competency shortfall (SF-11).
/// </summary>
public interface IQualificationService
{
    /// <summary>Returns the qualification-type library with each type's current holder count (SF-12).</summary>
    Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the cards held by a person, each with its server-computed status (SF-7/SF-8).</summary>
    Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Captures a new card (SF-5). The card is never confirmed automatically. Returns its id.</summary>
    Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Confirms a card by a named person, recording who and when (SF-6). Returns false if not found.</summary>
    Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renews a card: creates a superseding record and retains the old one (SF-10). Null if not found.</summary>
    Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the qualifications a person's trade requires but they do not currently hold (SF-11).</summary>
    Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default);
}
