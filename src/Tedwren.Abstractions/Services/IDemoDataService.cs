using Tedwren.Abstractions.Contracts.DemoData;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Builds, inspects and tears down the Tedwren demonstration dataset — two fixed demo companies (a main
/// contractor and a subcontractor) with their administrators, sites, operatives, compliance history and a full
/// commercial payment history for reporting. Intended for the Product Admin portal only; the API restricts the
/// whole surface to platform admins. Every record uses deterministic, namespaced identifiers so a delete removes
/// exactly what was seeded and a recreate is idempotent. It never reads or writes anything outside the two demo
/// companies.
/// </summary>
public interface IDemoDataService
{
    /// <summary>Reports whether the demo dataset is present, with per-area counts.</summary>
    Task<DemoDataStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the live progress of a running seed/delete operation (idle snapshot when none is running).</summary>
    DemoDataProgress GetProgress();

    /// <summary>
    /// Creates the full demo dataset across the product and commercial databases. Deletes any existing demo
    /// dataset first so this doubles as "recreate". Reports progress via <see cref="GetProgress"/>.
    /// </summary>
    Task<DemoDataStatus> SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes the entire demo dataset from both databases. Safe to call when none exists.</summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
