using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Cache-then-network reader for the manager surface (M7): when online it fetches fresh data and caches it; when
/// offline (or a call fails) it returns the last cached value, so manager screens stay usable in the field. Read
/// surfaces only — an entry decision/override and form review actions are never cached and go direct through the
/// clients. The muster carries its own <see cref="MusterDto.GeneratedUtc"/> so a cached copy shows its age (MC-14).
/// </summary>
public sealed class ManagerDataService
{
    private const string DashboardKey = "manager.dashboard";
    private const string ExpiryKey = "manager.expiry";
    private const string ActivityKey = "manager.activity";
    private const string SitesKey = "manager.sites";
    private const string OperativesKey = "manager.operatives";
    private const string TemplatesKey = "manager.forms.templates";
    private const string AssignmentsKey = "manager.forms.assignments";
    private const string SubmissionsKey = "manager.forms.submissions";
    private const string EvidenceKey = "manager.evidence";

    private readonly ManagerApiClient _overview;
    private readonly ManagerSiteEntryApiClient _siteEntry;
    private readonly ManagerWorkforceApiClient _workforce;
    private readonly ManagerFormsApiClient _forms;
    private readonly ManagerEvidenceApiClient _evidence;
    private readonly IReadCache _cache;
    private readonly IConnectivityService _connectivity;

    /// <summary>Creates the reader over the manager clients, the read cache and the connectivity service.</summary>
    public ManagerDataService(
        ManagerApiClient overview, ManagerSiteEntryApiClient siteEntry, ManagerWorkforceApiClient workforce,
        ManagerFormsApiClient forms, ManagerEvidenceApiClient evidence, IReadCache cache, IConnectivityService connectivity)
    {
        _overview = overview;
        _siteEntry = siteEntry;
        _workforce = workforce;
        _forms = forms;
        _evidence = evidence;
        _cache = cache;
        _connectivity = connectivity;
    }

    /// <summary>The organisation dashboard summary (fresh when online, else cached).</summary>
    public Task<DashboardSummaryDto?> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(DashboardKey, () => _overview.GetDashboardAsync(cancellationToken), cancellationToken);

    /// <summary>Upcoming expiries within 30 days (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<UpcomingExpiryDto>?> GetUpcomingExpiriesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<UpcomingExpiryDto>>(ExpiryKey, async () => await _overview.GetUpcomingExpiriesAsync(30, cancellationToken), cancellationToken);

    /// <summary>Recent audit activity (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<AuditEntryDto>?> GetRecentActivityAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<AuditEntryDto>>(ActivityKey, async () => await _overview.GetRecentActivityAsync(7, cancellationToken), cancellationToken);

    /// <summary>The company's sites, for the muster/decide picker (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<SiteSummary>?> GetSitesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<SiteSummary>>(SitesKey, async () => await _siteEntry.GetSitesAsync(cancellationToken), cancellationToken);

    /// <summary>The live muster for a site (fresh when online, else the last cached copy — with its own data age, MC-14).</summary>
    public Task<MusterDto?> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        ReadAsync($"manager.muster.{siteId}", () => _siteEntry.GetMusterAsync(siteId, cancellationToken), cancellationToken);

    /// <summary>The operative register (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<OperativeListItemDto>?> GetOperativesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<OperativeListItemDto>>(OperativesKey, async () => await _workforce.GetOperativesAsync(cancellationToken), cancellationToken);

    /// <summary>The form-template library (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<FormTemplateSummaryDto>?> GetFormTemplatesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<FormTemplateSummaryDto>>(TemplatesKey, async () => await _forms.GetTemplatesAsync(cancellationToken), cancellationToken);

    /// <summary>The current form assignments (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<FormAssignmentDto>?> GetFormAssignmentsAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<FormAssignmentDto>>(AssignmentsKey, async () => await _forms.GetAssignmentsAsync(cancellationToken), cancellationToken);

    /// <summary>The form submissions for review (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<FormSubmissionSummaryDto>?> GetFormSubmissionsAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<FormSubmissionSummaryDto>>(SubmissionsKey, async () => await _forms.GetSubmissionsAsync(cancellationToken), cancellationToken);

    /// <summary>The field-evidence captures for review (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<EvidenceCaptureDto>?> GetEvidenceCapturesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<EvidenceCaptureDto>>(EvidenceKey, async () => await _evidence.GetCapturesAsync(null, cancellationToken), cancellationToken);

    private async Task<T?> ReadAsync<T>(string key, Func<Task<T?>> fetch, CancellationToken cancellationToken)
    {
        if (_connectivity.IsConnected)
        {
            try
            {
                var fresh = await fetch();
                if (fresh is not null)
                {
                    await _cache.SetAsync(key, fresh, cancellationToken);
                    return fresh;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException)
            {
                // Fall through to the cached copy — a transient/offline failure must not blank the screen.
            }
        }

        return await _cache.GetAsync<T>(key, cancellationToken);
    }
}
