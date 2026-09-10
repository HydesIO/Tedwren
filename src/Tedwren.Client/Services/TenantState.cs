using Microsoft.JSInterop;
using Tedwren.Abstractions.Common;

namespace Tedwren.Client.Services;

/// <summary>
/// localStorage-backed <see cref="ITenantState"/>. Scoped so it is initialised once per app load. Reads and
/// writes the active company id through the <c>tedwren.tenant</c> JS helper; when nothing is stored it falls
/// back to <see cref="DefaultCompanyId"/> so existing single-tenant behaviour (and the API's seed data) still
/// line up until onboarding creates a company (R15).
/// </summary>
public sealed class TenantState : ITenantState
{
    /// <summary>The well-known seed company used until a company is created/chosen (matches the API seed).</summary>
    public static readonly Guid DefaultCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly IJSRuntime _js;
    private Guid _currentCompanyId = DefaultCompanyId;
    private OrgType? _currentOrgType;
    private bool _initialised;

    /// <summary>Creates the service over the JS runtime used to reach localStorage.</summary>
    public TenantState(IJSRuntime js) => _js = js;

    /// <inheritdoc />
    public Guid CurrentCompanyId => _currentCompanyId;

    /// <inheritdoc />
    public OrgType? CurrentOrgType => _currentOrgType;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_initialised)
        {
            return;
        }

        try
        {
            var stored = await _js.InvokeAsync<string>("tedwren.tenant.get");
            if (Guid.TryParse(stored, out var companyId) && companyId != Guid.Empty)
            {
                _currentCompanyId = companyId;
            }
        }
        catch
        {
            // JS not available (e.g. prerender) — keep the fallback company.
        }

        _initialised = true;
    }

    /// <inheritdoc />
    public async Task SetCurrentCompanyAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        _currentCompanyId = companyId;
        _initialised = true;
        try
        {
            await _js.InvokeVoidAsync("tedwren.tenant.set", companyId.ToString());
        }
        catch
        {
            // Persistence is best-effort; the value still applies for this session.
        }
    }

    /// <inheritdoc />
    public Task SetCurrentOrgTypeAsync(OrgType? orgType)
    {
        // Held in memory for the app's lifetime. The shell re-resolves it from the authoritative company
        // record on every load (before any page renders), so it does not need to survive a full reload.
        _currentOrgType = orgType;
        return Task.CompletedTask;
    }
}
