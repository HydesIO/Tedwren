using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Storage;

/// <summary>
/// <see cref="IImageStore"/> backed by an S3-compatible object store (iDrive e2, AWS S3, MinIO; LR-4). Bytes are
/// stored as private objects — no permanent public URL (R9); they are served only through the authorised image
/// endpoint, which reads them back through this store. The object key is a GUID (optionally namespaced by a prefix),
/// and the returned reference is that GUID string, so it stays interchangeable with the database store's references.
/// </summary>
public sealed class S3ImageStore : IImageStore
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _keyPrefix;

    /// <summary>Creates the store over an S3 client, bucket and optional key prefix.</summary>
    public S3ImageStore(IAmazonS3 s3, string bucket, string keyPrefix)
    {
        _s3 = s3;
        _bucket = bucket;
        _keyPrefix = keyPrefix ?? string.Empty;
    }

    /// <summary>Stores bytes as a private object and returns its reference (the object's GUID).</summary>
    public async Task<string> SaveAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        using var stream = new MemoryStream(bytes);
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = KeyFor(_keyPrefix, id.ToString()),
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            AutoCloseStream = false,
        }, cancellationToken);
        return id.ToString();
    }

    /// <summary>Returns a stored image by reference, or null when it is not a valid reference or does not exist.</summary>
    public async Task<StoredImage?> GetAsync(string reference, CancellationToken cancellationToken = default)
    {
        // Only a GUID reference is valid — this prevents a caller-supplied reference from addressing an arbitrary key (R9).
        if (!Guid.TryParse(reference, out var id))
        {
            return null;
        }

        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, KeyFor(_keyPrefix, id.ToString()), cancellationToken);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            var lastModified = response.LastModified is { } modified
                ? new DateTimeOffset(DateTime.SpecifyKind(modified, DateTimeKind.Utc))
                : DateTimeOffset.UtcNow;
            return new StoredImage
            {
                Id = id,
                ContentType = string.IsNullOrWhiteSpace(response.Headers.ContentType) ? "application/octet-stream" : response.Headers.ContentType,
                Bytes = buffer.ToArray(),
                CreatedUtc = lastModified,
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;   // no such object
        }
    }

    /// <summary>Builds the object key for a reference, applying the optional namespace prefix.</summary>
    public static string KeyFor(string keyPrefix, string reference) =>
        string.IsNullOrEmpty(keyPrefix) ? reference : keyPrefix.TrimEnd('/') + "/" + reference;
}
