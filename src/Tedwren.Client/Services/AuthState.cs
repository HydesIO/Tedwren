using System.Net.Http.Json;
using Microsoft.JSInterop;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Contracts.Identity;

namespace Tedwren.Client.Services;

/// <summary>
/// Client-side authentication state: signs in / accepts invitations against <c>/api/auth</c>, persists the
/// bearer token to localStorage, and exposes the current operator. The token is held in the shared
/// <see cref="ITokenStore"/> so the request handler attaches it to every call.
/// </summary>
public sealed class AuthState
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;
    private readonly IJSRuntime _js;

    /// <summary>The signed-in operator, or null when signed out.</summary>
    public CurrentUserDto? CurrentUser { get; private set; }

    /// <summary>Whether a user is currently signed in.</summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_tokens.Token);

    /// <summary>Whether the signed-in user may change data (Auditor is read-only, SF-23).</summary>
    public bool CanWrite => CurrentUser is not null &&
        !string.Equals(CurrentUser.Role, "Auditor", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the signed-in user is a Tedwren platform administrator (gates the admin area). Computed
    /// server-side and carried on <see cref="CurrentUserDto.IsPlatformAdmin"/>; the client never derives it.
    /// </summary>
    public bool IsPlatformAdmin => CurrentUser?.IsPlatformAdmin ?? false;

    /// <summary>Creates the auth state over the API client, token store and JS interop.</summary>
    public AuthState(HttpClient http, ITokenStore tokens, IJSRuntime js)
    {
        _http = http;
        _tokens = tokens;
        _js = js;
    }

    private bool _initialized;

    /// <summary>Loads any persisted token and resolves the current operator. Idempotent; called at shell start.</summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var token = await _js.InvokeAsync<string>("tedwren.auth.get");
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        _tokens.Token = token;
        try
        {
            CurrentUser = await _http.GetFromJsonAsync<CurrentUserDto>("api/me");
        }
        catch (HttpRequestException)
        {
            await LogoutAsync();
        }
    }

    /// <summary>Signs in with email + password. Returns false on invalid credentials.</summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        return await ApplyAsync(response);
    }

    /// <summary>Accepts an invitation, setting the password and signing in. Returns false on an invalid/expired link.</summary>
    public async Task<bool> AcceptInviteAsync(string token, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/accept-invite", new AcceptInviteRequest(token, password));
        return await ApplyAsync(response);
    }

    /// <summary>
    /// Requests a password-reset email for the address. Completes without revealing whether the account
    /// exists — the caller always shows the same "check your inbox" confirmation (no account enumeration).
    /// </summary>
    public async Task RequestPasswordResetAsync(string email)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/forgot-password", new ForgotPasswordRequest(email));
        _ = response; // Response is intentionally ignored; the outcome must not depend on account existence.
    }

    /// <summary>Sets a new password from a reset link and signs in. Returns false on an invalid/expired link.</summary>
    public async Task<bool> ResetPasswordAsync(string token, string password)
    {
        // A reset consumes the same one-time token the invite-acceptance endpoint validates (D1).
        using var response = await _http.PostAsJsonAsync("api/auth/accept-invite", new AcceptInviteRequest(token, password));
        return await ApplyAsync(response);
    }

    /// <summary>Clears the token and current user.</summary>
    public async Task LogoutAsync()
    {
        _tokens.Token = null;
        CurrentUser = null;
        await _js.InvokeVoidAsync("tedwren.auth.clear");
    }

    /// <summary>Stores the token + identity from a successful auth response.</summary>
    private async Task<bool> ApplyAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        if (result is null)
        {
            return false;
        }

        _tokens.Token = result.Token;
        await _js.InvokeVoidAsync("tedwren.auth.set", result.Token);
        CurrentUser = new CurrentUserDto(result.Name, result.Role, result.CompanyId);

        // The auth result cannot carry the platform-admin flag (it is computed from claims, not returned by
        // the token endpoint), so refresh the identity from /api/me. This keeps the admin-area gate
        // authoritative and server-derived; a failure here just leaves the (non-admin) partial identity.
        try
        {
            CurrentUser = await _http.GetFromJsonAsync<CurrentUserDto>("api/me") ?? CurrentUser;
        }
        catch (HttpRequestException)
        {
            // Keep the partial identity from the auth result; the shell will re-resolve on next load.
        }
        return true;
    }
}
