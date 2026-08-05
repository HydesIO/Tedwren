using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;

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

    /// <summary>Registers the in-memory organisation repositories and their shared seeded store (mock mode).</summary>
    public static IServiceCollection AddInMemoryOrganisationStore(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryOrganisationStore>();
        services.AddScoped<ICompanyRepository, InMemoryCompanyRepository>();
        services.AddScoped<IPersonRepository, InMemoryPersonRepository>();
        services.AddScoped<IEngagementRepository, InMemoryEngagementRepository>();
        return services;
    }
}
