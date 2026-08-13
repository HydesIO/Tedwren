namespace Tedwren.Domain.Entities;

/// <summary>
/// A file captured against a form submission — a photograph or uploaded attachment for a photo/file field
/// (requirement 4: "stored on disk e.g. in the database"). Stored as a database blob, owned by the same company
/// as its submission (R15) and referencing the field it answers.
/// </summary>
public sealed class FormSubmissionFile
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the file (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The submission this file belongs to.</summary>
    public required Guid SubmissionId { get; init; }

    /// <summary>The field this file answers.</summary>
    public required string FieldId { get; init; }

    /// <summary>The original file name.</summary>
    public required string FileName { get; init; }

    /// <summary>The file's MIME content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>The file's bytes.</summary>
    public required byte[] Content { get; init; }

    /// <summary>When the file was captured (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset UploadedUtc { get; init; } = DateTimeOffset.UtcNow;
}
