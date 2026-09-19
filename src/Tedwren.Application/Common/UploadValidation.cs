namespace Tedwren.Application.Common;

/// <summary>Upload category, selecting the content-type allow-list applied by <see cref="UploadValidation"/>.</summary>
public enum UploadKind
{
    /// <summary>A photo/image (card capture, form photo).</summary>
    Image,

    /// <summary>A document (RAMS, insurance, accreditation) — images plus PDF/office formats.</summary>
    Document,
}

/// <summary>
/// Server-side validation for client-supplied file uploads (R9): it caps the size and restricts the content type
/// to a known allow-list, before the bytes are decoded or stored. Applied at every point that accepts an uploaded
/// file — the anonymous flows especially (trade documents, induction form files, onboarding card photos) — so an
/// unauthenticated caller cannot exhaust memory/storage or store an unexpected/executable file type. Throws
/// <see cref="ArgumentException"/> (mapped to HTTP 400 by the endpoints) on a violation. Single responsibility:
/// decide whether an upload is acceptable.
/// </summary>
public static class UploadValidation
{
    /// <summary>Maximum accepted decoded file size, in bytes (10 MB).</summary>
    public const int MaxFileBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif",
    };

    private static readonly HashSet<string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif", "application/pdf",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    /// <summary>
    /// Validates a base64-encoded upload's size and content type, throwing <see cref="ArgumentException"/> when it
    /// exceeds <see cref="MaxFileBytes"/> or declares a content type not allowed for <paramref name="kind"/>. A
    /// null/empty payload is treated as "no file" and passes (callers decide whether a file is required); a missing
    /// content type is size-checked but not type-checked, so a client that omits it is not rejected.
    /// </summary>
    public static void Validate(string? base64, string? contentType, UploadKind kind)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return;
        }

        // Estimate the decoded size from the base64 length (~3 bytes per 4 chars) so an oversized payload is
        // rejected before it is decoded into memory. Tolerate a "data:...;base64," prefix.
        var comma = base64.IndexOf(',');
        var payload = base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0
            ? base64[(comma + 1)..]
            : base64;
        var estimatedBytes = payload.Length * 3L / 4L;
        if (estimatedBytes > MaxFileBytes)
        {
            throw new ArgumentException($"The uploaded file exceeds the {MaxFileBytes / (1024 * 1024)} MB limit.");
        }

        var allowed = kind == UploadKind.Image ? ImageTypes : DocumentTypes;
        if (!string.IsNullOrWhiteSpace(contentType) && !allowed.Contains(contentType.Trim()))
        {
            throw new ArgumentException($"The uploaded file type '{contentType}' is not accepted.");
        }
    }

    /// <summary>
    /// Validates an already-decoded (streamed) upload's length and content type — the multipart counterpart to
    /// <see cref="Validate"/>, used by the mobile <c>/api/mobile/uploads</c> endpoint so a file is checked without
    /// base64-encoding it first. Throws <see cref="ArgumentException"/> when it exceeds <see cref="MaxFileBytes"/> or
    /// declares a content type not allowed for <paramref name="kind"/>. A missing content type is size-checked but
    /// not type-checked, mirroring <see cref="Validate"/>.
    /// </summary>
    public static void ValidateFile(long lengthBytes, string? contentType, UploadKind kind)
    {
        if (lengthBytes > MaxFileBytes)
        {
            throw new ArgumentException($"The uploaded file exceeds the {MaxFileBytes / (1024 * 1024)} MB limit.");
        }

        var allowed = kind == UploadKind.Image ? ImageTypes : DocumentTypes;
        if (!string.IsNullOrWhiteSpace(contentType) && !allowed.Contains(contentType.Trim()))
        {
            throw new ArgumentException($"The uploaded file type '{contentType}' is not accepted.");
        }
    }
}
