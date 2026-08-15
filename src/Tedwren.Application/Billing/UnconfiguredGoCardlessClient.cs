namespace Tedwren.Application.Billing;

/// <summary>
/// Default <see cref="IGoCardlessClient"/> used when no GoCardless access token is configured. Every call
/// throws a clear <see cref="GoCardlessNotConfiguredException"/> so the read surfaces (served from the local
/// database) keep working while collection actions fail with an actionable message instead of a null-reference
/// or a mis-configured HTTP call. The real <see cref="GoCardlessClient"/> replaces this registration when a
/// token is present — mirroring how the Resend sender overrides the outbox default.
/// </summary>
public sealed class UnconfiguredGoCardlessClient : IGoCardlessClient
{
    /// <inheritdoc />
    public Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default) => throw NotConfigured();

    /// <inheritdoc />
    public Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default) => throw NotConfigured();

    /// <inheritdoc />
    public Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default) => throw NotConfigured();

    /// <inheritdoc />
    public Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default) => throw NotConfigured();

    /// <inheritdoc />
    public Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => throw NotConfigured();

    /// <inheritdoc />
    public Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => throw NotConfigured();

    private static GoCardlessNotConfiguredException NotConfigured() => new();
}

/// <summary>
/// Thrown when a collection action is attempted but GoCardless has no access token configured. Surfaced by the
/// API as a 503 so an operator sees "billing provider not configured" rather than a generic error.
/// </summary>
public sealed class GoCardlessNotConfiguredException : InvalidOperationException
{
    /// <summary>Creates the exception with a standard message.</summary>
    public GoCardlessNotConfiguredException()
        : base("GoCardless is not configured (no access token). Set the GoCardless:AccessToken to enable direct-debit collection.")
    {
    }
}
