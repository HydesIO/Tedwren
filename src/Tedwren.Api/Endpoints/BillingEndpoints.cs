using Tedwren.Abstractions.Contracts.Billing;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Billing;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the platform admin-area direct-debit billing endpoints (<c>/api/admin/billing</c>). The whole group is
/// gated by the <c>PlatformAdmin</c> policy. Collection actions call GoCardless; when it is not configured they
/// return 503 (via <see cref="GoCardlessNotConfiguredException"/>) so an operator sees an actionable message.
/// </summary>
public static class BillingEndpoints
{
    /// <summary>Registers the platform-admin billing endpoint group.</summary>
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/billing")
            .WithTags("Admin Billing")
            .RequireAuthorization("PlatformAdmin");

        group.MapGet("/mandates", async (IBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetMandatesAsync(ct)))
            .WithName("AdminGetMandates");

        group.MapGet("/payments", async (IBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetPaymentsAsync(ct)))
            .WithName("AdminGetPayments");

        group.MapGet("/companies/{companyId:guid}/overview", async (Guid companyId, IBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetCompanyBillingAsync(companyId, ct)))
            .WithName("AdminGetCompanyBilling");

        group.MapGet("/events", async (IBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetWebhookEventsAsync(200, ct)))
            .WithName("AdminGetWebhookEvents");

        group.MapPost("/companies/{companyId:guid}/mandate/setup", async (Guid companyId, IBillingService billing, CancellationToken ct) =>
                await GuardProviderAsync(async () => Results.Ok(await billing.StartMandateSetupAsync(companyId, ct))))
            .WithName("AdminStartMandateSetup");

        group.MapPost("/companies/{companyId:guid}/mandate/cancel", async (Guid companyId, IBillingService billing, CancellationToken ct) =>
                await GuardProviderAsync(async () =>
                {
                    var cancelled = await billing.CancelMandateAsync(companyId, ct);
                    return cancelled ? Results.NoContent() : Results.NotFound();
                }))
            .WithName("AdminCancelMandate");

        group.MapPost("/companies/{companyId:guid}/payments", async (Guid companyId, TakePaymentRequest request, IBillingService billing, CancellationToken ct) =>
                await GuardProviderAsync(async () =>
                {
                    try
                    {
                        var payment = await billing.TakePaymentAsync(companyId, request, ct);
                        return Results.Created($"/api/admin/billing/payments/{payment.Id}", payment);
                    }
                    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
                }))
            .WithName("AdminTakePayment");

        group.MapPost("/payments/{paymentId:guid}/retry", async (Guid paymentId, IBillingService billing, CancellationToken ct) =>
                await GuardProviderAsync(async () =>
                {
                    try
                    {
                        var payment = await billing.RetryPaymentAsync(paymentId, ct);
                        return payment is null ? Results.NotFound() : Results.Ok(payment);
                    }
                    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
                }))
            .WithName("AdminRetryPayment");

        group.MapPut("/companies/{companyId:guid}/subscription", async (Guid companyId, SetSubscriptionRequest request, IBillingService billing, CancellationToken ct) =>
                {
                    try
                    {
                        return Results.Ok(await billing.SetSubscriptionAsync(companyId, request, ct));
                    }
                    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                })
            .WithName("AdminSetSubscription");

        return app;
    }

    /// <summary>Runs a collection action, turning a missing GoCardless configuration into a 503 with a clear message.</summary>
    private static async Task<IResult> GuardProviderAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (GoCardlessNotConfiguredException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
