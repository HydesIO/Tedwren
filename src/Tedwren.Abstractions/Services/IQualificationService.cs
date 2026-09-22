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

    /// <summary>Returns the qualifications a person's trade requires but they do not currently hold (SF-11); includes the company's own map rows when a company id is given (Q21).</summary>
    Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>Evaluates Gate 3 for an operative (spec §2, SF-11): whether every legally-mandatory accreditation the trade requires is held on a current, in-date card.</summary>
    Task<Gate3StatusDto> EvaluateGate3Async(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the qualification-type library the caller may manage (SF-12/Q21): the shared platform rows plus the caller's own org-custom types, with holder counts.</summary>
    Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesForManagementAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a qualification type (SF-12): a platform administrator may add a shared (global) type, a tenant an org-custom one (Q21, R15). Returns its id.</summary>
    Task<Guid> CreateQualificationTypeAsync(CreateQualificationTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a qualification type the caller owns (a shared row is platform-admin only, R15).</summary>
    Task UpdateQualificationTypeAsync(Guid id, UpdateQualificationTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a qualification type the caller owns, provided nothing references it (guarded — throws when cards or map rows still use it).</summary>
    Task DeleteQualificationTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the trade→accreditation map the caller may manage (SF-11/Q21): the shared platform rows plus the caller's own org-custom rows, each with its accreditation name.</summary>
    Task<IReadOnlyList<TradeQualificationRequirementDto>> GetTradeRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a trade→accreditation map row (SF-11): a platform administrator may add a shared (global) row, a tenant an org-custom one (Q21, R15). Returns its id.</summary>
    Task<Guid> CreateTradeRequirementAsync(CreateTradeRequirementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a trade→accreditation map row's flags the caller owns (a shared row is platform-admin only, R15).</summary>
    Task UpdateTradeRequirementAsync(Guid id, UpdateTradeRequirementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a trade→accreditation map row the caller owns (a shared row is platform-admin only, R15).</summary>
    Task DeleteTradeRequirementAsync(Guid id, CancellationToken cancellationToken = default);
}
