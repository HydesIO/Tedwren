using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Expiry;
using Tedwren.Application.Jobs;
using Tedwren.Application.Notifications;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Qualifications;

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
}
