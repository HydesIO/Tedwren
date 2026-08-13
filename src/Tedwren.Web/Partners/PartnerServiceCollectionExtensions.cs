namespace Tedwren.Web.Partners;

/// <summary>Registers the partner programme services (Web Plan §7) in the DI container.</summary>
public static class PartnerServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="PartnerOptions"/> and registers the in-memory store (singleton) plus the
    /// application/approval and referral/commission services. The store is the seam a database-backed
    /// implementation replaces later with no service changes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (for the "Partners" section).</param>
    public static IServiceCollection AddPartnerProgramme(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PartnerOptions>(configuration.GetSection(PartnerOptions.SectionName));
        services.TryAddSingletonTimeProvider();
        services.AddSingleton<IPartnerStore, InMemoryPartnerStore>();
        services.AddScoped<PartnerService>();
        services.AddScoped<ReferralService>();
        return services;
    }

    /// <summary>Registers the system <see cref="TimeProvider"/> once if nothing else has.</summary>
    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.All(d => d.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
