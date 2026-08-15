using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;

namespace Tedwren.DataAccess;

/// <summary>Dependency-injection registration helpers for the Dapper data-access layer.</summary>
public static class DataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the connection factory, dialect, Dapper repositories and migration runner for the
    /// given engine + connection string. The business service is unchanged — only the repositories it
    /// depends on differ from the mock registration.
    /// </summary>
    public static IServiceCollection AddSqlDataAccess(
        this IServiceCollection services, DatabaseProvider provider, string connectionString)
    {
        services.AddSingleton(new SqlDataAccessOptions { Provider = provider, ConnectionString = connectionString });
        services.AddSingleton<ISqlDialect>(_ =>
            provider == DatabaseProvider.PostgreSql ? new PostgresDialect() : new SqlServerDialect());
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyDocumentRepository, CompanyDocumentRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IEngagementRepository, EngagementRepository>();
        services.AddScoped<IQualificationTypeRepository, QualificationTypeRepository>();
        services.AddScoped<IQualificationCardRepository, QualificationCardRepository>();
        services.AddScoped<ITradeRequirementRepository, TradeRequirementRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IJobRunRepository, JobRunRepository>();
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<ISitePropertyRepository, SitePropertyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<ICompliancePackRepository, CompliancePackRepository>();
        services.AddScoped<IInductionTemplateRepository, InductionTemplateRepository>();
        services.AddScoped<IInductionSessionRepository, InductionSessionRepository>();
        services.AddScoped<IFormTemplateRepository, FormTemplateRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<IFormAssignmentRepository, FormAssignmentRepository>();
        services.AddScoped<Inductions.InductionTemplateSeeder>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IPermitRepository, PermitRepository>();
        services.AddScoped<IOnboardingLinkRepository, OnboardingLinkRepository>();
        services.AddScoped<IImageStore, ImageStore>();
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<Qualifications.QualificationLibrarySeeder>();
        services.AddSingleton<MigrationRunner>();
        return services;
    }

    /// <summary>
    /// Registers the <b>Commercial</b> database access: the admin connection factory (bound to the second
    /// connection string) and every repository whose data lives in the commercial catalogue — the billing
    /// plane (mandates, payments, subscriptions, webhook events, payouts) and the go-to-market slices (launch
    /// list, leads, affiliates). Called alongside <see cref="AddSqlDataAccess"/>; it reuses the shared
    /// <see cref="ISqlDialect"/> registered there and the <see cref="MigrationRunner"/>, which the composition
    /// root runs once per database (product factory then admin factory).
    /// </summary>
    public static IServiceCollection AddCommercialSqlDataAccess(
        this IServiceCollection services, DatabaseProvider provider, string connectionString)
    {
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
        return services;
    }
}
