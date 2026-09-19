using System.Security.Cryptography;

namespace Tedwren.Application.Mobile;

/// <summary>Generates the numeric one-time codes texted to operatives during sign-in (M2).</summary>
public interface IOtpCodeGenerator
{
    /// <summary>Generates a fresh one-time code.</summary>
    string Generate();
}

/// <summary>A cryptographically-random six-digit one-time code generator.</summary>
public sealed class RandomOtpCodeGenerator : IOtpCodeGenerator
{
    /// <summary>Generates a zero-padded six-digit code.</summary>
    public string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
