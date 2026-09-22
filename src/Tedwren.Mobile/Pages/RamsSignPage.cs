using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Read &amp; sign the current live RAMS (Subcontractor Onboarding spec Gate 5) — the native mirror of the emulator's
/// <c>RamsSign.razor</c>. The operative reads the approved method statement for their subcontractor, confirms they
/// understand it and signs; the signature is recorded against the live version (append-only, R4/R16) so their next
/// site sign-in is admitted. Reached from the home menu or when a sign-in is blocked pending a signature. The two
/// heads share <see cref="RamsApiClient"/>, so the API behaviour is identical; only this MAUI UI differs.
/// </summary>
public class RamsSignPage : ContentPage
{
    private readonly RamsApiClient _api;
    private readonly VerticalStackLayout _root = new() { Spacing = 16 };
    private readonly Entry _nameEntry = new() { Placeholder = "e.g. Sam Taylor" };
    private readonly CheckBox _understood = new();

    private LiveRamsDto? _live;
    private bool _busy;
    private bool _signed;
    private bool _loaded;

    /// <summary>Builds the RAMS sign page shell; the body is rendered once the live RAMS loads.</summary>
    public RamsSignPage(RamsApiClient api)
    {
        _api = api;
        Title = "Site RAMS";
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 16, Children = { _root } } };
    }

    /// <summary>Loads the operative's current live approved RAMS once when the page first appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _root.Children.Add(new Label { Text = "Loading the RAMS…" });
        try
        {
            _live = await _api.GetLiveAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _root.Children.Clear();
            _root.Children.Add(Header("Couldn't load the RAMS"));
            _root.Children.Add(Secondary("Check your connection and try again."));
            return;
        }

        Render();
    }

    /// <summary>Rebuilds the page body from the current state (native has no reactive re-render).</summary>
    private void Render()
    {
        _root.Children.Clear();

        if (_live is null)
        {
            _root.Children.Add(Header("Nothing to sign"));
            _root.Children.Add(Secondary("There's no RAMS for you to sign right now. If you've been told to sign one, check with your site manager."));
            return;
        }

        if (_signed)
        {
            _root.Children.Add(Header("Signed"));
            var done = new VerticalStackLayout { Spacing = 4 };
            done.Children.Add(new Label { Text = $"Thanks — your signature is recorded for {_live.Title} (v{_live.Version}).", FontAttributes = FontAttributes.Bold });
            done.Children.Add(Secondary("You can sign in now."));
            _root.Children.Add(new TwCard { Content = done });

            var back = new Button { Text = "Back to sign-in" };
            back.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
            back.Clicked += async (_, _) => await Navigation.PopAsync();
            _root.Children.Add(back);
            return;
        }

        _root.Children.Add(Header("Read & sign"));
        _root.Children.Add(Secondary("Read the method statement below, confirm you understand it and sign. You must do this before you can sign in on site."));

        // The method statement details.
        var details = new VerticalStackLayout { Spacing = 4 };
        details.Children.Add(new Label { Text = _live.Title, FontAttributes = FontAttributes.Bold });
        details.Children.Add(Secondary($"{_live.Reference} — v{_live.Version}"));
        details.Children.Add(Secondary($"Contractor: {_live.ContractorName}"));
        if (_live.HasFile)
        {
            details.Children.Add(Secondary("A signed-off document is attached — read it with your site manager if you need it."));
        }

        _root.Children.Add(new TwCard { Content = details });

        // Sign.
        var sign = new VerticalStackLayout { Spacing = 8 };
        sign.Children.Add(new Label { Text = "Your full name" });
        sign.Children.Add(_nameEntry);
        sign.Children.Add(new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _understood, new Label { Text = "I have read and understood this RAMS and will work to it.", VerticalOptions = LayoutOptions.Center } },
        });

        var signButton = new Button { Text = "Sign RAMS", IsEnabled = !_busy };
        signButton.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        signButton.Clicked += async (_, _) => await SignAsync();
        sign.Children.Add(signButton);

        _root.Children.Add(new TwCard { Content = sign });
    }

    /// <summary>Records the operative's signature of the live RAMS version (Gate 5).</summary>
    private async Task SignAsync()
    {
        if (_busy || _live is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameEntry.Text) || !_understood.IsChecked)
        {
            await DisplayAlert("Site RAMS", "Enter your name and confirm you've read it to sign.", "OK");
            return;
        }

        _busy = true;
        try
        {
            var ack = await _api.SignAsync(new SignRamsRequest(_live.Id, _nameEntry.Text!.Trim()));
            if (ack is null)
            {
                // The version moved on while the page was open — reload the current one and ask them to sign again.
                await DisplayAlert("Site RAMS", "This RAMS was updated. Please read the latest version and sign again.", "OK");
                _live = await _api.GetLiveAsync();
                _understood.IsChecked = false;
            }
            else
            {
                _signed = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlert("Site RAMS", "Couldn't record your signature. Check your connection and try again.", "OK");
        }
        finally
        {
            _busy = false;
            Render();
        }
    }

    private static Label Header(string text) => new() { Text = text, FontSize = 22, FontAttributes = FontAttributes.Bold };

    private static Label Secondary(string text)
    {
        var label = new Label { Text = text, FontSize = 13 };
        label.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return label;
    }
}
