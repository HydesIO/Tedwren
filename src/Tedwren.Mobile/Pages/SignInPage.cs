using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The app entry page: choose the operative or the manager/admin experience (the role-switch shell). Real auth —
/// mobile-number + one-time code for operatives (M2), console email + password for managers (M7) — replaces the
/// direct navigation here; this M1 skeleton demonstrates the two role homes.
/// </summary>
public class SignInPage : ContentPage
{
    /// <summary>Builds the branded entry page and wires the two role entry points.</summary>
    public SignInPage(IServiceProvider services)
    {
        Title = "Welcome";

        var brandHeader = new Border
        {
            BackgroundColor = TwPalette.Brand,
            StrokeThickness = 0,
            Padding = new Thickness(24, 44),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Tedwren", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = TwPalette.OnBrand, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Field companion", FontSize = 15, TextColor = TwPalette.OnBrand, HorizontalOptions = LayoutOptions.Center },
                },
            },
        };

        var operativeBtn = new Button { Text = "I'm an operative" };
        operativeBtn.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        operativeBtn.Clicked += async (_, _) => await Navigation.PushAsync(services.GetRequiredService<OperativeEnrolPage>());

        var managerBtn = new Button { Text = "Site manager / admin" };
        managerBtn.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        managerBtn.Clicked += async (_, _) => await Navigation.PushAsync(services.GetRequiredService<ManagerHomePage>());

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    brandHeader,
                    new VerticalStackLayout
                    {
                        Padding = new Thickness(16),
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Choose how you're signing in", FontSize = 18, FontAttributes = FontAttributes.Bold },
                            new Label { Text = "Operatives sign in with their mobile number and a one-time code, then unlock with biometrics. Managers and admins sign in with their console email and password." },
                            operativeBtn,
                            managerBtn,
                        },
                    },
                },
            },
        };
    }
}
