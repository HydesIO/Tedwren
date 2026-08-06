using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Services;
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
}
