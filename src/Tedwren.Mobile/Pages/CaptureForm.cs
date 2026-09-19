using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Shared building blocks for the M5 capture pages (evidence + hazard): camera capture, the "saved" / error status
/// banner, and note trimming. Kept small and inline for M5's two consumers; a reusable Mobile.Controls control is
/// deferred to M6 when the forms inbox becomes the third consumer.
/// </summary>
internal static class CaptureForm
{
    /// <summary>Captures a photo from the camera, returning its bytes + content type, or null when cancelled/unavailable.</summary>
    public static async Task<(byte[] Bytes, string ContentType)?> TakePhotoAsync(Page page)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await page.DisplayAlert("Camera", "This device can't capture photos.", "OK");
                return null;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null)
            {
                return null;
            }

            using var stream = await photo.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            var contentType = string.IsNullOrWhiteSpace(photo.ContentType) ? "image/jpeg" : photo.ContentType;
            return (memory.ToArray(), contentType);
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException)
        {
            await page.DisplayAlert("Camera", "Camera permission is needed to capture a photo.", "OK");
            return null;
        }
    }

    /// <summary>Shows the "saved — will sync" confirmation banner in the success token colour.</summary>
    public static void ShowSaved(Label banner, bool online)
    {
        banner.IsVisible = true;
        banner.Text = online ? "Saved — syncing now." : "Saved — it'll sync when you're online.";
        banner.SetAppThemeColor(Label.TextColorProperty, TwPalette.SuccessLight, TwPalette.SuccessDark);
        banner.SetAppThemeColor(VisualElement.BackgroundColorProperty, TwPalette.SuccessPaleLight, TwPalette.SuccessPaleDark);
    }

    /// <summary>Shows a save-failed banner in the danger token colour.</summary>
    public static void ShowError(Label banner)
    {
        banner.IsVisible = true;
        banner.Text = "Couldn't save. Please try again.";
        banner.SetAppThemeColor(Label.TextColorProperty, TwPalette.DangerLight, TwPalette.DangerDark);
        banner.SetAppThemeColor(VisualElement.BackgroundColorProperty, TwPalette.DangerPaleLight, TwPalette.DangerPaleDark);
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
