using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Persistence;

namespace Tedwren.DataAccess.Storage;

/// <summary>Dependency-injection registration for the S3-compatible object store (LR-4).</summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers an S3-compatible <see cref="IImageStore"/> (iDrive e2 / AWS S3 / MinIO), overriding the database
    /// store. Fails fast with a clear message when required settings are missing, so a misconfigured Production
    /// deployment does not start silently mis-persisting private assets (R9). Credentials come from configuration
    /// (environment / secret store), never source.
    /// </summary>
    public static IServiceCollection AddS3ImageStore(this IServiceCollection services, S3StorageOptions options)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Bucket)) missing.Add("Storage:S3:Bucket");
        if (string.IsNullOrWhiteSpace(options.AccessKey)) missing.Add("Storage:S3:AccessKey");
        if (string.IsNullOrWhiteSpace(options.SecretKey)) missing.Add("Storage:S3:SecretKey");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Storage:Provider is 'S3' but required setting(s) are missing: {string.Join(", ", missing)}. " +
                "Set them via the environment / a secret store (for iDrive e2 also set Storage:S3:ServiceUrl).");
        }

        var config = new AmazonS3Config { ForcePathStyle = options.ForcePathStyle };
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            // An S3-compatible provider (e.g. iDrive e2): point at its endpoint and sign for the configured region.
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
        }
        else
        {
            // AWS S3 proper: resolve the region endpoint.
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, config));
        services.AddScoped<IImageStore>(sp => new S3ImageStore(
            sp.GetRequiredService<IAmazonS3>(), options.Bucket, options.KeyPrefix));
        return services;
    }
}
