namespace Tedwren.Abstractions.Configuration;

/// <summary>Which backing store holds private binary assets (card photos, uploaded documents; R9).</summary>
public enum StorageProvider
{
    /// <summary>Store bytes as BLOBs in the product database (the default; no external dependency).</summary>
    Database = 0,

    /// <summary>Store bytes in an S3-compatible object store (e.g. iDrive e2, AWS S3, MinIO).</summary>
    S3 = 1,
}
