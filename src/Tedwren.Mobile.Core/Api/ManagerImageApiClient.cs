namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Fetches a private image (a qualification card photo or a field-evidence photo) by its image-store reference,
/// over the console-token auth handler — the bytes are served only through the authorised <c>GET /api/images/{id}</c>
/// route, never a permanent public URL (R9). Returns the bytes, or null when the image is missing/forbidden, so a
/// page can hide the image gracefully rather than error.
/// </summary>
public sealed class ManagerImageApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerImageApiClient(HttpClient http) => _http = http;

    /// <summary>Returns the image bytes for a reference, or null when it cannot be read.</summary>
    public async Task<byte[]?> GetImageAsync(string reference, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/images/{Uri.EscapeDataString(reference)}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(cancellationToken) : null;
    }
}
