using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for compliance packs (SUB-13–SUB-26). A pack's snapshot is fixed at send (R7);
/// access events are append-only (SUB-20). Sender reads are company-scoped (R15); recipient lookup is by the
/// opaque token only (R8/R9).
/// </summary>
public interface ICompliancePackRepository
{
    /// <summary>Persists a new pack (with its fixed snapshot).</summary>
    Task AddAsync(CompliancePack pack, CancellationToken cancellationToken = default);

    /// <summary>Returns a pack by id, scoped to the owning company, or null (R15).</summary>
    Task<CompliancePack?> GetAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default);

    /// <summary>Returns a pack by its opaque access token, or null (the recipient's only handle, R8/R9).</summary>
    Task<CompliancePack?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's packs, newest first.</summary>
    Task<IReadOnlyList<CompliancePack>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Persists status changes (revoke, supersede) to a pack.</summary>
    Task UpdateStatusAsync(CompliancePack pack, CancellationToken cancellationToken = default);

    /// <summary>Appends a recipient access event (SUB-20).</summary>
    Task AddAccessEventAsync(PackAccessEvent accessEvent, CancellationToken cancellationToken = default);

    /// <summary>Returns the open/download tallies for a pack (SUB-20).</summary>
    Task<(int Opened, int Downloaded)> GetAccessTallyAsync(Guid packId, CancellationToken cancellationToken = default);
}
