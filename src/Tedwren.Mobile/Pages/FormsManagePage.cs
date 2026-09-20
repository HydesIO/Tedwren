using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager forms library + assignments (M7): the company's form templates and where each is assigned, with an
/// action to assign a form and to remove an assignment. Reuses the existing <c>/api/forms/*</c> surface; loaded via
/// <see cref="ManagerDataService"/> (cache-then-network). Submission review is a separate screen.
/// </summary>
public class FormsManagePage : ContentPage
{
    private readonly ManagerDataService _data;
    private readonly ManagerFormsApiClient _forms;
    private readonly IServiceProvider _services;

    private readonly VerticalStackLayout _templatesBody = new() { Spacing = 8 };
    private readonly VerticalStackLayout _assignmentsBody = new() { Spacing = 8 };

    /// <summary>Builds the forms management page over the manager data + forms clients.</summary>
    public FormsManagePage(ManagerDataService data, ManagerFormsApiClient forms, IServiceProvider services)
    {
        _data = data;
        _forms = forms;
        _services = services;
        Title = "Forms";

        var assign = new Button { Text = "Assign a form" };
        assign.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        assign.Clicked += async (_, _) => await Navigation.PushAsync(_services.GetRequiredService<FormAssignPage>());

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Forms", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    assign,
                    new Label { Text = "Templates", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    new TwCard { Content = _templatesBody },
                    new Label { Text = "Assignments", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    new TwCard { Content = _assignmentsBody },
                },
            },
        };
    }

    /// <summary>Reloads templates + assignments each time the page appears (e.g. after assigning).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _templatesBody.Children.Clear();
        _assignmentsBody.Children.Clear();

        var templates = await _data.GetFormTemplatesAsync();
        if (templates is { Count: > 0 })
        {
            foreach (var t in templates)
            {
                _templatesBody.Children.Add(TemplateRow(t));
            }
        }
        else
        {
            _templatesBody.Children.Add(ManagerUi.Muted("No form templates yet."));
        }

        var assignments = await _data.GetFormAssignmentsAsync();
        if (assignments is { Count: > 0 })
        {
            foreach (var a in assignments)
            {
                _assignmentsBody.Children.Add(AssignmentRow(a));
            }
        }
        else
        {
            _assignmentsBody.Children.Add(ManagerUi.Muted("No assignments yet."));
        }
    }

    private static View TemplateRow(FormTemplateSummaryDto t)
    {
        var kind = t.Status switch { "Published" => TwStatusKind.Success, "Draft" => TwStatusKind.Warning, _ => TwStatusKind.Neutral };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children = { new Label { Text = t.Name, FontAttributes = FontAttributes.Bold, FontSize = 14 }, ManagerUi.Muted($"v{t.Version} · {t.FieldCount} fields") },
        }, 0, 0);
        grid.Add(new TwStatusPill { Text = t.Status, Kind = kind }, 1, 0);
        return grid;
    }

    private View AssignmentRow(FormAssignmentDto a)
    {
        var scope = a.Scope == "Site" && a.SiteName is { Length: > 0 } s ? $"Site: {s}" : a.Scope;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children = { new Label { Text = a.FormName, FontAttributes = FontAttributes.Bold, FontSize = 14 }, ManagerUi.Muted($"{scope} · {a.Schedule}") },
        }, 0, 0);

        var remove = new Button { Text = "Remove", Padding = new Thickness(10, 0) };
        remove.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        remove.Clicked += async (_, _) => await RemoveAsync(a);
        grid.Add(remove, 1, 0);
        return grid;
    }

    private async Task RemoveAsync(FormAssignmentDto a)
    {
        if (!await DisplayAlert("Remove assignment", $"Stop assigning \"{a.FormName}\"?", "Remove", "Cancel"))
        {
            return;
        }

        try
        {
            await _forms.DeleteAssignmentAsync(a.Id);
            await LoadAsync();
        }
        catch (ApiException)
        {
            await DisplayAlert("Couldn't remove", "You may not have permission, or the connection failed.", "OK");
        }
    }
}
