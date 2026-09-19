namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Strongly-typed binding for the "Storage" configuration section — where the private binary assets behind
/// <c>IImageStore</c> (card photos, uploaded documents; R9) are held. It defaults to <see cref="StorageProvider.Database"/>
/// so nothing external is required until an object store is configured; the S3 credentials are secrets that belong in
/// the environment / a secret store, never in source.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Name of the configuration section this type binds to.</summary>
    public const string SectionName = "Storage";

    /// <summary>Which backing store to use. Defaults to <see cref="StorageProvider.Database"/>.</summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Database;

    /// <summary>S3-compatible object-store settings, used when <see cref="Provider"/> is <see cref="StorageProvider.S3"/>.</summary>
    public S3StorageOptions S3 { get; set; } = new();
}

/// <summary>
/// Settings for an S3-compatible object store (iDrive e2, AWS S3, MinIO, …). For iDrive e2 set
/// <see cref="ServiceUrl"/> to the bucket's e2 endpoint and keep <see cref="ForcePathStyle"/> true.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>The service endpoint URL (e.g. an iDrive e2 endpoint like <c>https://xxxx.idrivee2-nn.com</c>). Leave blank for AWS S3 proper.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>The signing region. A placeholder like <c>us-east-1</c> is fine for most S3-compatible providers.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>The bucket that holds the assets. Required when the provider is S3.</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>The access key id. Required when the provider is S3.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>The secret access key. Required when the provider is S3; treat as a secret (environment / secret store only).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Whether to use path-style bucket addressing. Most S3-compatible providers (including iDrive e2) require this.</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>An optional key prefix that namespaces the stored objects (e.g. <c>tedwren/</c>).</summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
