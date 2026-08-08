namespace Tedwren.Domain.Enums;

/// <summary>The kind of recipient interaction recorded against a compliance pack (SUB-20).</summary>
public enum PackAccessKind
{
    /// <summary>The recipient opened/viewed the pack.</summary>
    Opened = 0,

    /// <summary>The recipient downloaded the pack (PDF/ZIP).</summary>
    Downloaded = 1,
}
