using System.Text.Json;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Session;

/// <summary>The outcome of a manager (console) sign-in attempt.</summary>
public enum ManagerLoginStatus
{
    /// <summary>Signed in; the session is live and persisted.</summary>
    Success,

    /// <summary>The email or password was rejected (HTTP 401).</summary>
    InvalidCredentials,

    /// <summary>The sign-in could not be completed (offline or a server error).</summary>
    Error,
}

/// <summary>A manager sign-in attempt result, carrying the session on success.</summary>
public sealed record ManagerLoginResult(ManagerLoginStatus Status, MobileSession? Session);

/// <summary>
/// Notified when a manager API call is rejected as unauthorised (the console token expired). The console token has
/// no refresh (there is no console refresh endpoint), so the session is cleared and the shell re-prompts for
/// sign-in rather than silently refreshing — contrast the operative <see cref="ISessionRefresher"/>.
/// </summary>
public interface IManagerSessionExpiredHandler
{
    /// <summary>Clears the current manager session and signals that the app must re-authenticate.</summary>
    void HandleUnauthorized();
}

/// <summary>
/// Orchestrates manager/admin sign-in on the device (M7; refresh added in M8): console email + password against
/// the existing <c>/api/auth/login</c> surface, with the resulting console JWT held in <see cref="AccessTokenStore"/>
/// and the session (incl. a rotating refresh token) persisted (biometric-gated) in secure storage. From M8 an
/// expired access token is renewed <b>silently</b> from the stored refresh token (<see cref="IManagerSessionRefresher"/>)
/// rather than forcing a re-login; only when the refresh token itself is gone/rejected is <see cref="SessionExpired"/>
/// raised. Biometrics are a local unlock, not identity verification (R17).
/// </summary>
public sealed class ManagerSessionManager : IManagerSessionExpiredHandler, IManagerSessionRefresher
{
    private const string SessionKey = "tw.manager.session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AuthApiClient _auth;
    private readonly ISecureStore _store;
    private readonly IBiometricAuthenticator _biometrics;
    private readonly AccessTokenStore _tokens;
    private readonly TimeProvider _clock;

    /// <summary>Raised when the session cannot be renewed (the refresh token is gone/rejected), so the shell routes to sign-in.</summary>
    public event EventHandler? SessionExpired;

    /// <summary>The current signed-in manager session (name/role/company), or null when signed out. Lets the UI
    /// tailor itself to the role (e.g. hide review actions from a read-only Auditor, SF-23).</summary>
    public MobileSession? Current { get; private set; }

    /// <summary>Creates the manager over the console auth client, secure store, biometrics, token store and clock
    /// (the clock defaults to the system clock, so DI resolves it without a registration — as the sync engine does).</summary>
    public ManagerSessionManager(AuthApiClient auth, ISecureStore store, IBiometricAuthenticator biometrics, AccessTokenStore tokens, TimeProvider? clock = null)
    {
        _auth = auth;
        _store = store;
        _biometrics = biometrics;
        _tokens = tokens;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Whether this device holds a stored manager session (independent of whether it is still valid).</summary>
    public async Task<bool> HasStoredSessionAsync() => !string.IsNullOrEmpty(await _store.GetAsync(SessionKey));

    /// <summary>
    /// Signs a manager in with console email + password. On success the access token is set live and the session
    /// (incl. its refresh token) is persisted for a biometric-gated resume; invalid credentials and transport
    /// failures are reported distinctly.
    /// </summary>
    public async Task<ManagerLoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        AuthResultDto? result;
        try
        {
            result = await _auth.LoginAsync(email, password, cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            return new ManagerLoginResult(ManagerLoginStatus.Error, null);
        }

        if (result is null)
        {
            return new ManagerLoginResult(ManagerLoginStatus.InvalidCredentials, null);
        }

        var session = await ApplyAsync(result);
        return new ManagerLoginResult(ManagerLoginStatus.Success, session);
    }

    /// <summary>
    /// Attempts to resume a stored manager session on launch: biometric unlock (when available), then either use the
    /// still-valid access token or <b>silently refresh</b> it from the stored refresh token. When neither is possible
    /// the session is cleared and <see cref="ResumeStatus.Expired"/> is returned (re-login required).
    /// </summary>
    public async Task<ResumeResult> TryResumeAsync(CancellationToken cancellationToken = default)
    {
        var stored = await ReadAsync();
        if (stored is null)
        {
            return new ResumeResult(ResumeStatus.NotEnrolled, null);
        }

        var now = _clock.GetUtcNow();
        var accessValid = stored.ExpiresUtc > now;
        var canRefresh = !string.IsNullOrEmpty(stored.RefreshToken) && stored.RefreshTokenExpiresUtc > now;
        if (!accessValid && !canRefresh)
        {
            ClearStored();
            return new ResumeResult(ResumeStatus.Expired, null);
        }

        // Local unlock: when biometrics are enrolled they must succeed before the stored session is used (R17).
        if (await _biometrics.IsAvailableAsync())
        {
            var unlock = await _biometrics.AuthenticateAsync("Unlock Tedwren");
            if (unlock != BiometricResult.Success)
            {
                return new ResumeResult(ResumeStatus.Locked, null);
            }
        }

        if (accessValid)
        {
            var session = ToSession(stored);
            _tokens.AccessToken = session.Token;
            Current = session;
            return new ResumeResult(ResumeStatus.Resumed, session);
        }

        var refreshed = await RefreshAccessTokenAsync(cancellationToken);
        if (refreshed is null)
        {
            ClearStored();
            return new ResumeResult(ResumeStatus.Expired, null);
        }

        return new ResumeResult(ResumeStatus.Resumed, Current);
    }

    /// <summary>Silently renews the access token from the stored refresh token (no biometric); null when unavailable/rejected.</summary>
    public async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var stored = await ReadAsync();
        if (stored is null || string.IsNullOrEmpty(stored.RefreshToken) || stored.RefreshTokenExpiresUtc <= _clock.GetUtcNow())
        {
            return null;
        }

        AuthResultDto? result;
        try
        {
            result = await _auth.RefreshAsync(stored.RefreshToken!, cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            return null;
        }

        if (result is null)
        {
            return null; // rejected — the caller (handler / resume) clears + re-prompts
        }

        var session = await ApplyAsync(result);
        return session.Token;
    }

    /// <summary>Signs out on this device by discarding the stored session and the live access token.</summary>
    public void SignOut() => ClearStored();

    /// <summary>Clears the session on a terminal 401 and raises <see cref="SessionExpired"/> so the shell re-prompts.</summary>
    public void HandleUnauthorized()
    {
        ClearStored();
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Persists a fresh auth result, sets it live and updates <see cref="Current"/>; returns the session.</summary>
    private async Task<MobileSession> ApplyAsync(AuthResultDto result)
    {
        var session = new MobileSession(result.Token, result.ExpiresUtc, result.Name, result.Role, result.CompanyId);
        await _store.SetAsync(SessionKey, JsonSerializer.Serialize(
            new Stored(result.Token, result.ExpiresUtc, result.Name, result.Role, result.CompanyId, result.RefreshToken, result.RefreshTokenExpiresUtc),
            Json));
        _tokens.AccessToken = session.Token;
        Current = session;
        return session;
    }

    private void ClearStored()
    {
        _tokens.AccessToken = null;
        _store.Remove(SessionKey);
        Current = null;
    }

    private static MobileSession ToSession(Stored s) => new(s.Token, s.ExpiresUtc, s.Name, s.Role, s.CompanyId);

    private async Task<Stored?> ReadAsync()
    {
        var json = await _store.GetAsync(SessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Stored>(json, Json);
        }
        catch (JsonException)
        {
            _store.Remove(SessionKey);
            return null;
        }
    }

    /// <summary>The persisted shape of a manager session: a console JWT + identity + the rotating refresh token (M8).</summary>
    private sealed record Stored(
        string Token, DateTimeOffset ExpiresUtc, string Name, string Role, Guid CompanyId,
        string? RefreshToken, DateTimeOffset? RefreshTokenExpiresUtc);
}
