using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Billing;

/// <summary>
/// Catches the local billing state up with GoCardless by polling the provider for every mandate and payment
/// that is not yet in a terminal state. This backstops the webhooks (Phase C): if a webhook is missed or
/// arrives late, the next reconciliation still converges the status. Safe to run when GoCardless is not
/// configured — it simply does nothing.
/// </summary>
public sealed class BillingReconciliationService
{
    private readonly IGoCardlessClient _goCardless;
    private readonly IMandateRepository _mandates;
    private readonly IPaymentRepository _payments;

    /// <summary>Creates the service over the GoCardless client and the mandate/payment repositories.</summary>
    public BillingReconciliationService(IGoCardlessClient goCardless, IMandateRepository mandates, IPaymentRepository payments)
    {
        _goCardless = goCardless;
        _mandates = mandates;
        _payments = payments;
    }

    /// <summary>Polls GoCardless for non-terminal mandates/payments and applies any status change locally.</summary>
    public async Task<ReconciliationResult> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var mandatesUpdated = 0;
        var paymentsUpdated = 0;

        try
        {
            foreach (var mandate in await _mandates.GetAllAsync(cancellationToken))
            {
                if (IsTerminal(mandate.Status) || string.IsNullOrEmpty(mandate.GoCardlessMandateId))
                {
                    continue;
                }

                var remote = await _goCardless.GetMandateAsync(mandate.GoCardlessMandateId, cancellationToken);
                if (remote is not null && GoCardlessStatusMap.ToMandateStatus(remote.Status) is { } status && status != mandate.Status)
                {
                    mandate.Status = status;
                    mandate.UpdatedUtc = DateTimeOffset.UtcNow;
                    await _mandates.UpdateAsync(mandate, cancellationToken);
                    mandatesUpdated++;
                }
            }

            foreach (var payment in await _payments.GetAllAsync(cancellationToken))
            {
                if (IsTerminal(payment.Status) || string.IsNullOrEmpty(payment.GoCardlessPaymentId))
                {
                    continue;
                }

                var remote = await _goCardless.GetPaymentAsync(payment.GoCardlessPaymentId, cancellationToken);
                if (remote is not null && GoCardlessStatusMap.ToPaymentStatus(remote.Status) is { } status && status != payment.Status)
                {
                    payment.Status = status;
                    payment.UpdatedUtc = DateTimeOffset.UtcNow;
                    await _payments.UpdateAsync(payment, cancellationToken);
                    paymentsUpdated++;
                }
            }
        }
        catch (GoCardlessNotConfiguredException)
        {
            // No provider configured — nothing to reconcile.
        }

        return new ReconciliationResult(mandatesUpdated, paymentsUpdated);
    }

    /// <summary>A mandate in one of these states will not change again, so it is not polled.</summary>
    private static bool IsTerminal(MandateStatus status) =>
        status is MandateStatus.Cancelled or MandateStatus.Failed or MandateStatus.Expired;

    /// <summary>A payment in one of these states will not change again through polling, so it is not polled.</summary>
    private static bool IsTerminal(PaymentStatus status) =>
        status is PaymentStatus.PaidOut or PaymentStatus.Cancelled or PaymentStatus.ChargedBack or PaymentStatus.Failed;
}

/// <summary>Tally of a reconciliation pass: how many mandates and payments changed status.</summary>
public sealed record ReconciliationResult(int MandatesUpdated, int PaymentsUpdated);
