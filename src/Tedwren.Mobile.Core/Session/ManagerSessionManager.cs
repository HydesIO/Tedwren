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
/// Orchestrates manager/admin sign-in on the device (M7): console email + password against the existing
/// <c>/api/auth/login</c> surface, with the resulting console JWT held in <see cref="AccessTokenStore"/> and
/// persisted (biometric-gated) in secure storage so a relaunch resumes without re-typing credentials while the
/// token is still valid. Unlike the operative flow there is <b>no refresh token</b> (the console issues none), so
/// an expired token means re-login — surfaced through <see cref="SessionExpired"/> when a call returns 401.
/// Biometrics are a local unlock, not identity verification (R17).
/// </summary>
public sealed class ManagerSessionManager : IManagerSessionExpiredHandler
{
    private const string SessionKey = "tw.manager.session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AuthApiClient _auth;
    private readonly ISecureStore _store;
    private readonly IBiometricAuthenticator _biometrics;
    private readonly AccessTokenStore _tokens;
    private readonly TimeProvider _clock;

    /// <summary>Raised when a manager API call is rejected as unauthorised, so the shell routes to sign-in.</summary>
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
    /// Signs a manager in with console email + password. On success the access token is set live and the session is
    /// persisted for a biometric-gated resume; invalid credentials and transport failures are reported distinctly.
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

        var session = new MobileSession(result.Token, result.ExpiresUtc, result.Name, result.Role, result.CompanyId);
        await PersistAsync(session);
        _tokens.AccessToken = session.Token;
        Current = session;
        return new ManagerLoginResult(ManagerLoginStatus.Success, session);
    }

    /// <summary>
    /// Attempts to resume a stored manager session on launch: biometric unlock (when available), then, if the token
    /// is still valid, sets it live. An expired token is cleared (re-login required); there is no silent refresh.
    /// </summary>
    public async Task<ResumeResult> TryResumeAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var stored = await ReadAsync();
        if (stored is null)
        {
            return new ResumeResult(ResumeStatus.NotEnrolled, null);
        }

        // No console refresh token exists: an expired access token can only be replaced by signing in again.
        if (stored.IsExpired(_clock.GetUtcNow()))
        {
            _store.Remove(SessionKey);
            return new ResumeResult(ResumeStatus.Expired, null);
        }

        // Local unlock: when biometrics are enrolled they must succeed before the stored token is used (R17).
        if (await _biometrics.IsAvailableAsync())
        {
            var unlock = await _biometrics.AuthenticateAsync("Unlock Tedwren");
            if (unlock != BiometricResult.Success)
            {
                return new ResumeResult(ResumeStatus.Locked, null);
            }
        }

        _tokens.AccessToken = stored.Token;
        Current = stored;
        return new ResumeResult(ResumeStatus.Resumed, stored);
    }

    /// <summary>Signs out on this device by discarding the stored session and the live access token.</summary>
    public void SignOut()
    {
        _tokens.AccessToken = null;
        _store.Remove(SessionKey);
        Current = null;
    }

    /// <summary>Clears the session on a 401 and raises <see cref="SessionExpired"/> so the shell re-prompts (no refresh).</summary>
    public void HandleUnauthorized()
    {
        _tokens.AccessToken = null;
        _store.Remove(SessionKey);
        Current = null;
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }

    private Task PersistAsync(MobileSession session) =>
        _store.SetAsync(SessionKey, JsonSerializer.Serialize(
            new Stored(session.Token, session.ExpiresUtc, session.Name, session.Role, session.CompanyId), Json));

    private async Task<MobileSession?> ReadAsync()
    {
        var json = await _store.GetAsync(SessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var s = JsonSerializer.Deserialize<Stored>(json, Json);
            return s is null ? null : new MobileSession(s.Token, s.ExpiresUtc, s.Name, s.Role, s.CompanyId);
        }
        catch (JsonException)
        {
            _store.Remove(SessionKey);
            return null;
        }
    }

    /// <summary>The persisted shape of a manager session (a console JWT + identity; no refresh token exists).</summary>
    private sealed record Stored(string Token, DateTimeOffset ExpiresUtc, string Name, string Role, Guid CompanyId);
}
