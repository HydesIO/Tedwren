using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Assign a published form to a scope (M7, requirement 5): pick a form, the target (organisation / site / operator),
/// a cadence and an optional failed-check alert email. Posts to the existing <c>/api/forms/assignments</c> endpoint;
/// a read-only Auditor's write is refused server-side and surfaced here.
/// </summary>
public class FormAssignPage : ContentPage
{
    private static readonly string[] Scopes = { "Organisation", "Site", "Operator" };
    private static readonly string[] Schedules = { "AdHoc", "Daily", "Weekly", "Monthly" };

    private readonly ManagerDataService _data;
    private readonly ManagerFormsApiClient _forms;

    private readonly Picker _templatePicker = new() { Title = "Choose a form" };
    private readonly Picker _scopePicker = new() { Title = "Assign to", ItemsSource = Scopes };
    private readonly Picker _sitePicker = new() { Title = "Site", IsVisible = false };
    private readonly Picker _operativePicker = new() { Title = "Operative", IsVisible = false };
    private readonly Picker _schedulePicker = new() { Title = "How often", ItemsSource = Schedules };
    private readonly Entry _alertEmail = new() { Keyboard = Keyboard.Email, Placeholder = "Failed-check alert email (optional)" };
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };
    private readonly Button _assign;

    private IReadOnlyList<FormTemplateSummaryDto> _templates = Array.Empty<FormTemplateSummaryDto>();
    private IReadOnlyList<SiteSummary> _sites = Array.Empty<SiteSummary>();
    private IReadOnlyList<OperativeListItemDto> _operatives = Array.Empty<OperativeListItemDto>();
    private bool _busy;

    /// <summary>Builds the assign page over the manager data + forms clients.</summary>
    public FormAssignPage(ManagerDataService data, ManagerFormsApiClient forms)
    {
        _data = data;
        _forms = forms;
        Title = "Assign a form";

        _scopePicker.SelectedIndexChanged += (_, _) =>
        {
            var scope = FromArray(_scopePicker, Scopes);
            _sitePicker.IsVisible = scope == "Site";
            _operativePicker.IsVisible = scope == "Operator";
        };

        _assign = new Button { Text = "Assign" };
        _assign.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _assign.Clicked += OnAssignAsync;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Assign a form", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Form", FontAttributes = FontAttributes.Bold },
                    _templatePicker,
                    new Label { Text = "Assign to", FontAttributes = FontAttributes.Bold },
                    _scopePicker,
                    _sitePicker,
                    _operativePicker,
                    new Label { Text = "How often", FontAttributes = FontAttributes.Bold },
                    _schedulePicker,
                    _alertEmail,
                    _status,
                    _assign,
                },
            },
        };
    }

    /// <summary>Loads the published templates, sites and operatives once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_templates.Count == 0)
        {
            var templates = await _data.GetFormTemplatesAsync() ?? Array.Empty<FormTemplateSummaryDto>();
            _templates = templates.Where(t => t.Status == "Published").ToList();
            _templatePicker.ItemsSource = _templates.Select(t => $"{t.Name} (v{t.Version})").ToList();
        }

        if (_sites.Count == 0)
        {
            _sites = await _data.GetSitesAsync() ?? Array.Empty<SiteSummary>();
            _sitePicker.ItemsSource = _sites.Select(s => s.Name).ToList();
        }

        if (_operatives.Count == 0)
        {
            _operatives = await _data.GetOperativesAsync() ?? Array.Empty<OperativeListItemDto>();
            _operativePicker.ItemsSource = _operatives.Select(o => o.Name).ToList();
        }
    }

    private async void OnAssignAsync(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if (Selected(_templatePicker, _templates) is not { } template)
        {
            ShowError("Choose a form to assign.");
            return;
        }

        var scope = FromArray(_scopePicker, Scopes);
        if (scope is null)
        {
            ShowError("Choose who to assign it to.");
            return;
        }

        Guid? siteId = null;
        Guid? personId = null;
        if (scope == "Site")
        {
            if (Selected(_sitePicker, _sites) is not { } site)
            {
                ShowError("Choose a site.");
                return;
            }

            siteId = site.Id;
        }
        else if (scope == "Operator")
        {
            if (Selected(_operativePicker, _operatives) is not { } operative)
            {
                ShowError("Choose an operative.");
                return;
            }

            personId = operative.PersonId;
        }

        var schedule = FromArray(_schedulePicker, Schedules) ?? "AdHoc";
        var alertEmail = string.IsNullOrWhiteSpace(_alertEmail.Text) ? null : _alertEmail.Text.Trim();

        _busy = true;
        _assign.IsEnabled = false;
        try
        {
            await _forms.CreateAssignmentAsync(new CreateFormAssignmentRequest(template.FamilyId, scope, siteId, personId, null, schedule, alertEmail));
            await DisplayAlert("Assigned", $"\"{template.Name}\" is now assigned.", "OK");
            await Navigation.PopAsync();
        }
        catch (ApiException)
        {
            ShowError("Couldn't assign the form. You may not have permission, or the connection failed.");
        }
        finally
        {
            _busy = false;
            _assign.IsEnabled = true;
        }
    }

    private static string? FromArray(Picker picker, string[] items) =>
        picker.SelectedIndex >= 0 && picker.SelectedIndex < items.Length ? items[picker.SelectedIndex] : null;

    private static T? Selected<T>(Picker picker, IReadOnlyList<T> source) where T : class =>
        picker.SelectedIndex >= 0 && picker.SelectedIndex < source.Count ? source[picker.SelectedIndex] : null;

    private void ShowError(string message)
    {
        _status.Text = message;
        _status.IsVisible = true;
        _status.SetAppThemeColor(Label.TextColorProperty, TwPalette.DangerLight, TwPalette.DangerDark);
    }
}
