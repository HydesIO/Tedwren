using Microsoft.JSInterop;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="ISecureStore"/> for the emulator: persists the refresh/session secrets in the page's
/// <c>localStorage</c>. This is a TEST surface only — <c>localStorage</c> is not a secure enclave, so it is never
/// used on a real device (the MAUI head keeps the Keychain/Keystore implementation). Every call is guarded so a
/// blocked/unavailable store (private browsing, cleared site data) degrades to a miss rather than throwing.
/// </summary>
public sealed class WebSecureStore : ISecureStore
{
    private const string Prefix = "tw.secure.";
    private readonly IJSRuntime _js;

    /// <summary>Creates the store over the JS runtime used to reach <c>localStorage</c>.</summary>
    public WebSecureStore(IJSRuntime js) => _js = js;

    /// <summary>Returns a stored secret by key, or null when absent/unreadable.</summary>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", Prefix + key);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Stores (or replaces) a secret; a write failure is swallowed (best-effort in a test surface).</summary>
    public async Task SetAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", Prefix + key, value);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Best-effort: a storage write failure must not break sign-in.
        }
    }

    /// <summary>Removes a secret (used on sign-out). Synchronous via the in-process JS runtime, matching the contract.</summary>
    public void Remove(string key)
    {
        try
        {
            if (_js is IJSInProcessRuntime inProcess)
            {
                inProcess.InvokeVoid("localStorage.removeItem", Prefix + key);
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Best-effort.
        }
    }
}
