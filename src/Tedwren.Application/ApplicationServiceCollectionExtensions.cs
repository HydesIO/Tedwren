using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Expiry;
using Tedwren.Application.Jobs;
using Tedwren.Application.Notifications;
using Tedwren.Application.Attendance;
using Tedwren.Application.Audit;
using Tedwren.Application.Decisions;
using Tedwren.Application.Entitlements;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Qualifications;
using Tedwren.Application.Sites;
using Tedwren.Application.Users;

namespace Tedwren.Application;

/// <summary>Dependency-injection registration helpers for the application layer.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the store-agnostic organisation business service. Repositories (in-memory or Dapper)
    /// are registered separately by the composition root, which is what the data-source switch selects.
    /// </summary>
    public static IServiceCollection AddOrganisationCore(this IServiceCollection services)
    {
        services.AddScoped<IOrganisationService, OrganisationService>();
        return services;
    }

    /// <summary>
    /// Registers the store-agnostic qualification business service (SF-5–SF-8, SF-10–SF-12). Repositories
    /// are registered separately by the composition root, matching the data-source switch.
    /// </summary>
    public static IServiceCollection AddQualificationCore(this IServiceCollection services)
    {
        services.AddScoped<IQualificationService, QualificationService>();
        return services;
    }

    /// <summary>Registers the in-memory organisation repositories and their shared seeded store (mock mode).</summary>
    public static IServiceCollection AddInMemoryOrganisationStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryOrganisationStore>();
        services.AddScoped<ICompanyRepository, InMemoryCompanyRepository>();
        services.AddScoped<IPersonRepository, InMemoryPersonRepository>();
        services.AddScoped<IEngagementRepository, InMemoryEngagementRepository>();
        return services;
    }

    /// <summary>Registers the in-memory qualification repositories and their shared seeded store (mock mode).</summary>
    public static IServiceCollection AddInMemoryQualificationStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryQualificationStore>();
        services.AddScoped<IQualificationTypeRepository, InMemoryQualificationTypeRepository>();
        services.AddScoped<IQualificationCardRepository, InMemoryQualificationCardRepository>();
        services.AddScoped<ITradeRequirementRepository, InMemoryTradeRequirementRepository>();
        return services;
    }

    /// <summary>
    /// Registers the expiry engine (SF-9), weekly digest (SUB-5) and job heartbeat (SF-21/R12): the jobs,
    /// the read query service, the runner/monitor, and the stub SMS/email senders writing to a shared
    /// outbox. The notification-log and job-run repositories are registered separately by the composition
    /// root (in-memory or Dapper), matching the data-source switch.
    /// </summary>
    public static IServiceCollection AddExpiryCore(this IServiceCollection services)
    {
        services.AddSingleton<ExpiryJobOptions>();
        services.AddSingleton<INotificationOutbox, NotificationOutbox>();
        services.AddScoped<ISmsSender, OutboxSmsSender>();
        services.AddScoped<IEmailSender, OutboxEmailSender>();
        services.AddScoped<ExpiryWarningJob>();
        services.AddScoped<WeeklyDigestJob>();
        services.AddScoped<JobRunner>();
        services.AddScoped<JobHeartbeatMonitor>();
        services.AddScoped<IExpiryQueryService, ExpiryQueryService>();
        return services;
    }

    /// <summary>Registers the in-memory expiry repositories and their shared store (mock mode).</summary>
    public static IServiceCollection AddInMemoryExpiryStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryExpiryStore>();
        services.AddScoped<INotificationLogRepository, InMemoryNotificationLogRepository>();
        services.AddScoped<IJobRunRepository, InMemoryJobRunRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic console user-management service (SF-20/SF-23/Q2).</summary>
    public static IServiceCollection AddUserCore(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    /// <summary>Registers the in-memory user repository and its shared seeded store (mock mode).</summary>
    public static IServiceCollection AddInMemoryUserStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryUserStore>();
        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic sites business service (SF-6/SF-14/SF-25/SF-26).</summary>
    public static IServiceCollection AddSiteCore(this IServiceCollection services)
    {
        services.AddScoped<ISiteService, SiteService>();
        return services;
    }

    /// <summary>Registers the in-memory site repositories and their shared seeded store (mock mode).</summary>
    public static IServiceCollection AddInMemorySiteStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemorySiteStore>();
        services.AddScoped<ISiteRepository, InMemorySiteRepository>();
        services.AddScoped<ISitePropertyRepository, InMemorySitePropertyRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic attendance service and the overnight still-in job (SF-13–SF-19).</summary>
    public static IServiceCollection AddAttendanceCore(this IServiceCollection services)
    {
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<OvernightSignInJob>();
        return services;
    }

    /// <summary>Registers the in-memory attendance repository and its shared store (mock mode).</summary>
    public static IServiceCollection AddInMemoryAttendanceStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryAttendanceStore>();
        services.AddScoped<IAttendanceRepository, InMemoryAttendanceRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic timesheet service (SUB-7–SUB-12, SUB-27, MC-24).</summary>
    public static IServiceCollection AddTimesheetCore(this IServiceCollection services)
    {
        services.AddScoped<ITimesheetService, Timesheets.TimesheetService>();
        return services;
    }

    /// <summary>Registers the in-memory timesheet repository and its seeded shared store (mock mode).</summary>
    public static IServiceCollection AddInMemoryTimesheetStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryTimesheetStore>();
        services.AddScoped<ITimesheetRepository, InMemoryTimesheetRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic site-entry decision &amp; muster service (MC-8–MC-14, R2, R3, R10, R14). Aggregates other slices' repositories/services, so those cores must also be registered.</summary>
    public static IServiceCollection AddSiteEntryCore(this IServiceCollection services)
    {
        services.AddScoped<ISiteEntryService, SiteEntry.SiteEntryService>();
        return services;
    }

    /// <summary>Registers the store-agnostic digital-induction service (MC-1–MC-7, MC-20, R5).</summary>
    public static IServiceCollection AddInductionCore(this IServiceCollection services)
    {
        services.AddScoped<IInductionService, Inductions.InductionService>();
        return services;
    }

    /// <summary>Registers the in-memory induction repositories and their seeded shared store (mock mode).</summary>
    public static IServiceCollection AddInMemoryInductionStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryInductionStore>();
        services.AddScoped<IInductionTemplateRepository, InMemoryInductionTemplateRepository>();
        services.AddScoped<IInductionSessionRepository, InMemoryInductionSessionRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic compliance-pack service (SUB-13–SUB-26, R7–R9).</summary>
    public static IServiceCollection AddCompliancePackCore(this IServiceCollection services)
    {
        services.AddScoped<ICompliancePackService, CompliancePacks.CompliancePackService>();
        return services;
    }

    /// <summary>Registers the in-memory compliance-pack repository and its shared store (mock mode).</summary>
    public static IServiceCollection AddInMemoryCompliancePackStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryCompliancePackStore>();
        services.AddScoped<ICompliancePackRepository, InMemoryCompliancePackRepository>();
        return services;
    }

    /// <summary>Registers the console-foundation services: module entitlements (Q2), audit (SF-20) and the decision store (R10).</summary>
    public static IServiceCollection AddConsoleFoundationCore(this IServiceCollection services)
    {
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDecisionService, DecisionService>();
        return services;
    }

    /// <summary>Registers the in-memory entitlement, audit and decision repositories (mock mode).</summary>
    public static IServiceCollection AddInMemoryConsoleFoundationStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryEntitlementRepository>();
        services.AddScoped<IEntitlementRepository>(sp => sp.GetRequiredService<InMemoryEntitlementRepository>());
        services.AddSingleton<InMemoryAuditStore>();
        services.AddScoped<IAuditRepository, InMemoryAuditRepository>();
        services.AddSingleton<InMemoryDecisionRepository>();
        services.AddScoped<IDecisionRepository>(sp => sp.GetRequiredService<InMemoryDecisionRepository>());
        return services;
    }
}
