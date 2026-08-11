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
        services.AddScoped<Inductions.InductionTemplateSeeder>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IPermitRepository, PermitRepository>();
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<Qualifications.QualificationLibrarySeeder>();
        services.AddSingleton<MigrationRunner>();
        return services;
    }
}
