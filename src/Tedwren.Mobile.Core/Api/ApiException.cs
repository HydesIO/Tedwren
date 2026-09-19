namespace Tedwren.Mobile.Core.Api;

/// <summary>Raised when the Tedwren API returns an unexpected, non-success response the caller cannot handle.</summary>
public sealed class ApiException : Exception
{
    /// <summary>The HTTP status code returned by the API.</summary>
    public int StatusCode { get; }

    /// <summary>Creates the exception with the offending status code and a human-readable message.</summary>
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
