using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Abstractions.Services;

/// <summary>Composes the operative home dashboard overview (M3) for a given operative (company + person id).</summary>
public interface IOperativeDashboardService
{
    /// <summary>Returns the operative's dashboard overview, or null when they have no engagement in the company.</summary>
    Task<OperativeDashboardDto?> GetAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default);
}
