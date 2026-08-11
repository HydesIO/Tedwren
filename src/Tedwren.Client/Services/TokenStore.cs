namespace Tedwren.Client.Services;

/// <summary>Holds the current bearer token in memory for the request handler. A leaf dependency (no cycle).</summary>
public interface ITokenStore
{
    /// <summary>The current bearer token, or null when signed out.</summary>
    string? Token { get; set; }
}

/// <summary>In-memory <see cref="ITokenStore"/> (one per app instance).</summary>
public sealed class TokenStore : ITokenStore
{
    /// <inheritdoc />
    public string? Token { get; set; }
}
