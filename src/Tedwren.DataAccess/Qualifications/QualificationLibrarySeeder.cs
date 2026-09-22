using Tedwren.Application.Persistence;
using Tedwren.Application.Qualifications;

namespace Tedwren.DataAccess.Qualifications;

/// <summary>
/// Seeds the default qualification-type library (SF-12) and default trade requirements (SF-11) into the
/// database after migrations, keeping <see cref="DefaultQualificationLibrary"/> as the single source of
/// truth (rather than duplicating the list in SQL). Idempotent: types are inserted only when their id is
/// absent, and each default requirement only when that (trade, type) mapping is not already present — so a
/// newly-added default (e.g. Gas Safe) lands on an existing database without disturbing the customer's own edits.
/// </summary>
public sealed class QualificationLibrarySeeder
{
    private readonly IQualificationTypeRepository _types;
    private readonly ITradeRequirementRepository _requirements;

    /// <summary>Creates the seeder over its repositories.</summary>
    public QualificationLibrarySeeder(IQualificationTypeRepository types, ITradeRequirementRepository requirements)
    {
        _types = types;
        _requirements = requirements;
    }

    /// <summary>Inserts any missing default types and, when none exist, the default trade requirements.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var type in DefaultQualificationLibrary.Types)
        {
            if (await _types.GetByIdAsync(type.Id, cancellationToken) is null)
            {
                await _types.AddAsync(type, cancellationToken);
            }
        }

        var existing = await _requirements.GetAllAsync(cancellationToken);
        var present = existing
            .Select(r => (r.Trade.ToLowerInvariant(), r.QualificationTypeId))
            .ToHashSet();
        foreach (var requirement in DefaultQualificationLibrary.TradeRequirements)
        {
            if (present.Add((requirement.Trade.ToLowerInvariant(), requirement.QualificationTypeId)))
            {
                await _requirements.AddAsync(requirement, cancellationToken);
            }
        }
    }
}
