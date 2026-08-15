using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Expiry;
using Tedwren.Application.Jobs;
using Tedwren.Application.Notifications;
using Tedwren.Application.Attendance;
using Tedwren.Application.Audit;
using Tedwren.Application.Auth;
using Tedwren.Application.Billing;
using Tedwren.Application.Dashboard;
using Tedwren.Application.Decisions;
using Tedwren.Application.Entitlements;
using Tedwren.Application.Onboarding;
using Tedwren.Application.Organisation;
using Tedwren.Application.Permits;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Qualifications;
using Tedwren.Application.Reference;
using Tedwren.Application.Settings;
using Tedwren.Application.Sites;
using Tedwren.Application.Users;
using Tedwren.Application.Workforce;

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
        services.AddScoped<ICompanyDocumentRepository, InMemoryCompanyDocumentRepository>();
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

    /// <summary>Registers the store-agnostic Forms Library services — templates, submissions and assignments (PRD-Phase 2 checklist/inspection engine).</summary>
    public static IServiceCollection AddFormCore(this IServiceCollection services)
    {
        services.AddScoped<IFormTemplateService, Forms.FormTemplateService>();
        services.AddScoped<IFormSubmissionService, Forms.FormSubmissionService>();
        services.AddScoped<IFormAssignmentService, Forms.FormAssignmentService>();
        services.AddScoped<Forms.RecurringFormReminderJob>();
        return services;
    }

    /// <summary>Registers the in-memory form repositories and their shared seeded store (test-only mock mode).</summary>
    public static IServiceCollection AddInMemoryFormStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryFormStore>();
        services.AddScoped<IFormTemplateRepository, InMemoryFormTemplateRepository>();
        services.AddScoped<IFormSubmissionRepository, InMemoryFormSubmissionRepository>();
        services.AddScoped<IFormAssignmentRepository, InMemoryFormAssignmentRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic compliance-pack service (SUB-13–SUB-26, R7–R9) and the recipient-access throttle.</summary>
    public static IServiceCollection AddCompliancePackCore(this IServiceCollection services)
    {
        services.AddSingleton<CompliancePacks.IPackAccessThrottle, CompliancePacks.InMemoryPackAccessThrottle>();
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

    /// <summary>
    /// Registers the console authentication service (login + invite acceptance). The token issuer
    /// (<see cref="ITokenIssuer"/>) is provided by the API composition root (JWT).
    /// </summary>
    public static IServiceCollection AddAuthCore(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    /// <summary>
    /// Registers the org-wide workforce read model. It reuses the organisation, qualification and decision
    /// repositories/services, so no dedicated store registration is required.
    /// </summary>
    public static IServiceCollection AddWorkforceCore(this IServiceCollection services)
    {
        services.AddScoped<IWorkforceService, WorkforceService>();
        return services;
    }

    /// <summary>
    /// Registers the dashboard aggregation service. It reuses the organisation, qualification, site and
    /// expiry repositories/services, so no dedicated store registration is required.
    /// </summary>
    public static IServiceCollection AddDashboardCore(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }

    /// <summary>Registers the store-agnostic reference-data service (form option lists).</summary>
    public static IServiceCollection AddReferenceDataCore(this IServiceCollection services)
    {
        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        return services;
    }

    /// <summary>Registers the in-memory reference-data repository seeded with the canonical option lists (test double).</summary>
    public static IServiceCollection AddInMemoryReferenceDataStore(this IServiceCollection services)
    {
        services.AddScoped<IReferenceDataRepository, InMemoryReferenceDataRepository>();
        return services;
    }

    /// <summary>Registers the store-agnostic self-service onboarding service (SF-4, SUB-2).</summary>
    public static IServiceCollection AddOnboardingCore(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        return services;
    }

    /// <summary>Registers the in-memory onboarding-link + image stores (singletons so they persist across test requests).</summary>
    public static IServiceCollection AddInMemoryOnboardingStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryOnboardingLinkRepository>();
        services.AddScoped<IOnboardingLinkRepository>(sp => sp.GetRequiredService<InMemoryOnboardingLinkRepository>());
        services.AddSingleton<InMemoryImageStore>();
        services.AddScoped<IImageStore>(sp => sp.GetRequiredService<InMemoryImageStore>());
        return services;
    }

    /// <summary>Registers the store-agnostic launch-list service (Web Content Spec §6.9). Uses the ambient email sender.</summary>
    public static IServiceCollection AddLaunchListCore(this IServiceCollection services)
    {
        services.AddScoped<ILaunchListService, LaunchList.LaunchListService>();
        return services;
    }

    /// <summary>Registers the in-memory launch-signup store (singleton so signups persist across test requests).</summary>
    public static IServiceCollection AddInMemoryLaunchListStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryLaunchSignupRepository>();
        services.AddScoped<ILaunchSignupRepository>(sp => sp.GetRequiredService<InMemoryLaunchSignupRepository>());
        return services;
    }

    /// <summary>Registers the store-agnostic sales-lead pipeline service (Web Plan §7).</summary>
    public static IServiceCollection AddLeadsCore(this IServiceCollection services)
    {
        services.AddScoped<ILeadService, Leads.LeadService>();
        return services;
    }

    /// <summary>Registers the in-memory lead store (singleton so leads/notes persist across test requests).</summary>
    public static IServiceCollection AddInMemoryLeadsStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryLeadRepository>();
        services.AddScoped<ILeadRepository>(sp => sp.GetRequiredService<InMemoryLeadRepository>());
        return services;
    }

    /// <summary>Registers the store-agnostic permits-to-work service.</summary>
    public static IServiceCollection AddPermitCore(this IServiceCollection services)
    {
        services.AddScoped<IPermitService, PermitService>();
        return services;
    }

    /// <summary>Registers the in-memory permit repository (singleton so raised permits persist across test requests).</summary>
    public static IServiceCollection AddInMemoryPermitStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryPermitRepository>();
        services.AddScoped<IPermitRepository>(sp => sp.GetRequiredService<InMemoryPermitRepository>());
        return services;
    }

    /// <summary>Registers the store-agnostic per-company general-settings service (System Configuration).</summary>
    public static IServiceCollection AddSettingsCore(this IServiceCollection services)
    {
        services.AddScoped<ISettingsService, SettingsService>();
        return services;
    }

    /// <summary>Registers the in-memory settings repository (singleton so saved settings persist across test requests).</summary>
    public static IServiceCollection AddInMemorySettingsStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemorySettingsRepository>();
        services.AddScoped<ISettingsRepository>(sp => sp.GetRequiredService<InMemorySettingsRepository>());
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

    /// <summary>
    /// Registers the direct-debit billing service (admin area). The GoCardless transport defaults to the
    /// "not configured" stub; the API composition root overrides it with the real typed-<c>HttpClient</c>
    /// client when an access token is set — mirroring how the Resend sender overrides the outbox default.
    /// </summary>
    public static IServiceCollection AddBillingCore(this IServiceCollection services)
    {
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IGoCardlessClient, UnconfiguredGoCardlessClient>();
        services.AddScoped<GoCardlessWebhookProcessor>();
        services.AddScoped<BillingReconciliationService>();
        services.AddScoped<PayoutSyncService>();
        return services;
    }

    /// <summary>Registers the in-memory mandate, payment and subscription repositories (test-only double).</summary>
    public static IServiceCollection AddInMemoryBillingStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMandateRepository>();
        services.AddScoped<IMandateRepository>(sp => sp.GetRequiredService<InMemoryMandateRepository>());
        services.AddSingleton<InMemoryPaymentRepository>();
        services.AddScoped<IPaymentRepository>(sp => sp.GetRequiredService<InMemoryPaymentRepository>());
        services.AddSingleton<InMemoryBillingSubscriptionRepository>();
        services.AddScoped<IBillingSubscriptionRepository>(sp => sp.GetRequiredService<InMemoryBillingSubscriptionRepository>());
        services.AddSingleton<InMemoryWebhookEventRepository>();
        services.AddScoped<IWebhookEventRepository>(sp => sp.GetRequiredService<InMemoryWebhookEventRepository>());
        services.AddSingleton<InMemoryPayoutRepository>();
        services.AddScoped<IPayoutRepository>(sp => sp.GetRequiredService<InMemoryPayoutRepository>());
        return services;
    }
}
