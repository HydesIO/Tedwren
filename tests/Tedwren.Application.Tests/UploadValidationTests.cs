using Tedwren.Application.Common;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="UploadValidation"/> (R9): size cap + content-type allow-list on client-supplied
/// uploads, with a null payload and a missing content type both treated leniently.
/// </summary>
public sealed class UploadValidationTests
{
    private static string SmallBase64() => Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

    [Fact]
    public void NullOrEmpty_Passes()
    {
        UploadValidation.Validate(null, "image/png", UploadKind.Image);
        UploadValidation.Validate("", "image/png", UploadKind.Image);
        UploadValidation.Validate("   ", "image/png", UploadKind.Image);
    }

    [Fact]
    public void SmallAllowedImage_Passes()
    {
        UploadValidation.Validate(SmallBase64(), "image/png", UploadKind.Image);
        UploadValidation.Validate(SmallBase64(), "image/jpeg", UploadKind.Image);
    }

    [Fact]
    public void SmallDocument_AllowsPdfAndImages()
    {
        UploadValidation.Validate(SmallBase64(), "application/pdf", UploadKind.Document);
        UploadValidation.Validate(SmallBase64(), "image/png", UploadKind.Document);
    }

    [Fact]
    public void MissingContentType_IsSizeCheckedButNotTypeChecked()
    {
        // No content type + small payload passes (a client that omits it is not rejected).
        UploadValidation.Validate(SmallBase64(), null, UploadKind.Image);
        UploadValidation.Validate(SmallBase64(), "", UploadKind.Document);
    }

    [Fact]
    public void DisallowedTypeForImage_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            UploadValidation.Validate(SmallBase64(), "application/pdf", UploadKind.Image));
        Assert.Contains("not accepted", ex.Message);
    }

    [Fact]
    public void ExecutableType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            UploadValidation.Validate(SmallBase64(), "application/x-msdownload", UploadKind.Document));
    }

    [Fact]
    public void Oversized_Throws()
    {
        // Just over the 10 MB decoded limit (base64 is ~4/3 the byte size).
        var oversized = new string('A', (UploadValidation.MaxFileBytes * 4 / 3) + 8);
        var ex = Assert.Throws<ArgumentException>(() =>
            UploadValidation.Validate(oversized, "image/png", UploadKind.Image));
        Assert.Contains("exceeds", ex.Message);
    }

    [Fact]
    public void DataUrlPrefix_IsStrippedBeforeSizeEstimate()
    {
        var dataUrl = "data:image/png;base64," + SmallBase64();
        UploadValidation.Validate(dataUrl, "image/png", UploadKind.Image);
    }

    [Fact] // M5 multipart overload
    public void ValidateFile_SmallAllowedImage_Passes()
    {
        UploadValidation.ValidateFile(1024, "image/jpeg", UploadKind.Image);
        UploadValidation.ValidateFile(1024, null, UploadKind.Image); // missing type is size-checked only
    }

    [Fact] // M5 multipart overload
    public void ValidateFile_Oversized_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            UploadValidation.ValidateFile(UploadValidation.MaxFileBytes + 1, "image/png", UploadKind.Image));
        Assert.Contains("exceeds", ex.Message);
    }

    [Fact] // M5 multipart overload
    public void ValidateFile_DisallowedType_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            UploadValidation.ValidateFile(1024, "application/pdf", UploadKind.Image));
        Assert.Contains("not accepted", ex.Message);
    }
}
