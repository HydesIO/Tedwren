namespace Tedwren.Client.Services;

/// <summary>
/// The current customer (company) the console is acting for. Until real multi-tenant sign-in exists the app
/// is single-tenant, but the active company is no longer a hard-coded constant: onboarding a brand-new
/// organisation sets it here so every scoped read lines up with the freshly-created company (R15). The value
/// is persisted per browser (localStorage) so it survives reloads, falling back to the well-known seed
/// company when nothing has been chosen yet.
/// </summary>
public interface ITenantState
{
    /// <summary>The active company id. Reads the fallback seed id until <see cref="InitializeAsync"/> has run.</summary>
    Guid CurrentCompanyId { get; }

    /// <summary>Loads the persisted active company (once). Safe to call repeatedly and before first render.</summary>
    Task InitializeAsync();

    /// <summary>Sets and persists the active company (e.g. after onboarding creates one), then keeps it cached.</summary>
    Task SetCurrentCompanyAsync(Guid companyId);
}
