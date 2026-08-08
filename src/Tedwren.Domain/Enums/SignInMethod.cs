namespace Tedwren.Domain.Enums;

/// <summary>
/// How a worker reached the sign-in confirmation. Both routes produce a record identical in kind and
/// evidential weight (SF-13, SF-25).
/// </summary>
public enum SignInMethod
{
    /// <summary>Scanning a QR code at a fixed point of presence (SF-13).</summary>
    QrScan = 0,

    /// <summary>An assignment-tied link, for a scheme with nothing to scan (SF-25).</summary>
    AssignmentLink = 1,
}
