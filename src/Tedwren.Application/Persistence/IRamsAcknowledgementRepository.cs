using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="RamsAcknowledgement"/> — the Gate-5 record that an operative signed a live RAMS version. Append-only, scoped by company (R15).</summary>
public interface IRamsAcknowledgementRepository
{
    /// <summary>Persists a new RAMS acknowledgement (append-only, R4/R16).</summary>
    Task AddAsync(RamsAcknowledgement acknowledgement, CancellationToken cancellationToken = default);

    /// <summary>Returns the operative's most recent acknowledgement for a family, or null when they have never signed it.</summary>
    Task<RamsAcknowledgement?> GetLatestForPersonAsync(Guid companyId, Guid personId, Guid familyId, CancellationToken cancellationToken = default);
}
