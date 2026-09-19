using Tedwren.Abstractions.Contracts.Permits;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The permits-to-work service: raise a permit (draft or issued) and list a company's permits. Scoped to the
/// issuing company (R15).
/// </summary>
public interface IPermitService
{
    /// <summary>Raises a permit and returns its new identifier.</summary>
    Task<Guid> CreateAsync(CreatePermitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's permits, newest first.</summary>
    Task<IReadOnlyList<PermitDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a draft/issued permit (PRD §8.2), scoped to the caller's company (R15). Returns false when no
    /// such permit exists for the caller; throws when the permit cannot be approved from its current state.
    /// </summary>
    Task<bool> ApproveAsync(Guid permitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an issued/approved permit with an optional reason (PRD §8.2), scoped to the caller's company (R15).
    /// Returns false when no such permit exists for the caller; throws when it cannot be closed from its state.
    /// </summary>
    Task<bool> CloseAsync(Guid permitId, string? reason, CancellationToken cancellationToken = default);
}
