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
}
