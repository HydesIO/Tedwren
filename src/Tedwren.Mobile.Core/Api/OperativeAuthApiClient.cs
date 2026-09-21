using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Mobile.Core.Api;

/// <summary>Raised when the API rejects device binding because the operative or the device is already bound elsewhere (HTTP 409).</summary>
public sealed class DeviceConflictException : Exception
{
    /// <summary>Creates the exception with the server-supplied guidance message.</summary>
    public DeviceConflictException(string message) : base(message)
    {
    }
}

/// <summary>
/// Calls the operative (mobile) authentication endpoints — request one-time code, verify code + bind device,
/// and rotate the refresh token. Operatives sign in by mobile number (SF-1); the device binding is the
/// buddy-punching deterrent.
/// </summary>
public sealed class OperativeAuthApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public OperativeAuthApiClient(HttpClient http) => _http = http;

    /// <summary>Requests a one-time code by mobile number. Completes without revealing whether the number is known.</summary>
    public async Task RequestOtpAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/auth/request-otp", new RequestOtpRequest(mobileNumber), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Could not request a code ({(int)response.StatusCode}).");
        }
    }

    /// <summary>
    /// Verifies the code and binds this device. Returns the tokens on success, null on a wrong/expired code
    /// (401), or throws <see cref="DeviceConflictException"/> when the operative/device is bound elsewhere (409).
    /// </summary>
    public async Task<MobileAuthResultDto?> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/auth/verify-otp", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new DeviceConflictException(await SafeReadMessage(response, "This operative is already set up on another device."));
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Verification failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<MobileAuthResultDto>(cancellationToken);
    }

    /// <summary>Rotates a device-bound refresh token for a fresh access token. Returns null when the refresh token is rejected (401).</summary>
    public async Task<MobileAuthResultDto?> RefreshAsync(string refreshToken, string deviceId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/auth/refresh", new RefreshTokenRequest(refreshToken, deviceId), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Refresh failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<MobileAuthResultDto>(cancellationToken);
    }

    /// <summary>
    /// Development/demo-only sign-in used by the browser emulator: exchanges a known demo email for a real
    /// operative token bound to this device, without an SMS code. Returns null when the demo sign-in is rejected
    /// or not available (401), i.e. it is disabled or the endpoint isn't mapped (Production).
    /// </summary>
    public async Task<MobileAuthResultDto?> DemoSignInAsync(string email, string deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/auth/demo-sign-in", new DemoSignInRequest(email, deviceId, deviceName), cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Demo sign-in failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<MobileAuthResultDto>(cancellationToken);
    }

    private static async Task<string> SafeReadMessage(HttpResponseMessage response, string fallback)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body) ? fallback : body;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
        {
            return fallback;
        }
    }
}
