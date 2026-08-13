using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.Content;

/// <summary>Registers the JSON-backed content layer (Tedwren.Web Plan §3) in the DI container.</summary>
public static class ContentServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="ContentOptions"/> and registers <see cref="IContentProvider"/> as a singleton
    /// backed by <see cref="JsonContentProvider"/>, reading the content directory from options relative
    /// to the app content root. Content is loaded once at startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (for the "Content" section).</param>
    /// <param name="contentRootPath">The app content root, from the host environment.</param>
    public static IServiceCollection AddJsonContent(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.Configure<ContentOptions>(configuration.GetSection(ContentOptions.SectionName));

        services.AddSingleton<IContentProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ContentOptions>>().Value;
            var directory = Path.Combine(contentRootPath, options.Directory);
            return new JsonContentProvider(directory);
        });

        return services;
    }
}
