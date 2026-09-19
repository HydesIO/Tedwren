using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Storage;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// Unit tests for the object-storage configuration (LR-4): the "Storage" binding defaults, the S3 provider
/// registration and its fail-fast validation, and the S3 store's key/reference handling — none of which touch a
/// live bucket (a real S3 round-trip is a deployment-time check, like the LocalDB integration suite).
/// </summary>
public sealed class StorageConfigurationTests
{
    [Fact]
    public void StorageOptions_DefaultsToDatabase()
    {
        var options = new StorageOptions();

        Assert.Equal(StorageProvider.Database, options.Provider);
        Assert.True(options.S3.ForcePathStyle);   // most S3-compatible providers require path-style
    }

    [Fact]
    public void StorageOptions_BindsS3Section()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "S3",
            ["Storage:S3:ServiceUrl"] = "https://x.idrivee2-1.com",
            ["Storage:S3:Bucket"] = "tedwren-assets",
            ["Storage:S3:AccessKey"] = "AK",
            ["Storage:S3:SecretKey"] = "SK",
        }).Build();

        var options = config.GetSection(StorageOptions.SectionName).Get<StorageOptions>()!;

        Assert.Equal(StorageProvider.S3, options.Provider);
        Assert.Equal("tedwren-assets", options.S3.Bucket);
        Assert.Equal("https://x.idrivee2-1.com", options.S3.ServiceUrl);
    }

    [Fact] // fail-fast so a misconfigured Production deployment does not silently mis-persist assets (R9)
    public void AddS3ImageStore_MissingCredentials_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddS3ImageStore(new S3StorageOptions { ServiceUrl = "https://x", Bucket = "" }));

        Assert.Contains("Storage:S3:Bucket", ex.Message);
    }

    [Fact] // the S3 registration overrides the earlier database IImageStore registration (last wins)
    public void AddS3ImageStore_Valid_RegistersS3Store()
    {
        var services = new ServiceCollection();
        services.AddScoped<IImageStore>(_ => null!);   // stand-in for the database store that S3 overrides

        services.AddS3ImageStore(new S3StorageOptions
        {
            ServiceUrl = "https://x.idrivee2-1.com",
            Bucket = "b",
            AccessKey = "AK",
            SecretKey = "SK",
        });

        using var provider = services.BuildServiceProvider();
        Assert.IsType<S3ImageStore>(provider.GetRequiredService<IImageStore>());
    }

    [Theory]
    [InlineData("", "abc", "abc")]
    [InlineData("tedwren/", "abc", "tedwren/abc")]
    [InlineData("tedwren", "abc", "tedwren/abc")]
    public void KeyFor_AppliesPrefix(string prefix, string reference, string expected) =>
        Assert.Equal(expected, S3ImageStore.KeyFor(prefix, reference));

    [Fact] // a non-GUID reference is rejected before any S3 call, so a reference cannot address an arbitrary key (R9)
    public async Task GetAsync_NonGuidReference_ReturnsNull()
    {
        var s3 = new AmazonS3Client(new BasicAWSCredentials("x", "y"),
            new AmazonS3Config { ServiceURL = "https://example.invalid", ForcePathStyle = true });
        var store = new S3ImageStore(s3, "bucket", "prefix");

        Assert.Null(await store.GetAsync("not-a-guid"));
    }
}
