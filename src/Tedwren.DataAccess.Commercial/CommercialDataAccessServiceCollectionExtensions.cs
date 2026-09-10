using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Repositories;

namespace Tedwren.DataAccess;

/// <summary>Dependency-injection registration helpers for the <b>Commercial</b> Dapper data-access layer.</summary>
public static class CommercialDataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <b>Commercial</b> database access: the admin connection factory (bound to the second
    /// connection string) and every repository whose data lives in the commercial catalogue — the billing
    /// plane (mandates, payments, subscriptions, webhook events, payouts) and the go-to-market slices (launch
    /// list, leads, affiliates). Called alongside <see cref="DataAccessServiceCollectionExtensions.AddSqlDataAccess"/>;
    /// it reuses the shared <see cref="Dialects.ISqlDialect"/> and the <see cref="Migrations.MigrationRunner"/>
    /// registered there, which the composition root runs once per database (product factory then admin factory).
    /// </summary>
    public static IServiceCollection AddCommercialSqlDataAccess(
        this IServiceCollection services, DatabaseProvider provider, string connectionString)
    {
        // Ensure the Dapper DateOnly/TimeOnly handlers are registered even if this plane is wired up
        // on its own; registration is idempotent and process-wide.
        Tedwren.DataAccess.TypeHandlers.DapperTypeHandlers.EnsureRegistered();

        services.AddSingleton(new AdminSqlDataAccessOptions { Provider = provider, ConnectionString = connectionString });
        services.AddSingleton<IAdminDbConnectionFactory, AdminDbConnectionFactory>();

        // Billing plane (relocated from the product database — see docs/ef-migrations.md).
        services.AddScoped<IMandateRepository, MandateRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IBillingSubscriptionRepository, BillingSubscriptionRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();

        // Go-to-market slices.
        services.AddScoped<ILaunchSignupRepository, LaunchSignupRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IAffiliateRepository, AffiliateRepository>();
        return services;
    }
}
