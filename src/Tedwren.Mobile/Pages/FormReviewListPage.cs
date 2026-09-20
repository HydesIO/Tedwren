using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager submissions review list (M7): completed forms with their status, filterable, each tapping through to
/// the full review. Loaded via <see cref="ManagerDataService"/> (cache-then-network). Reuses <c>/api/forms/submissions</c>.
/// </summary>
public class FormReviewListPage : ContentPage
{
    private static readonly string[] Filters = { "All", "Awaiting review", "Approved", "Rejected" };

    private readonly ManagerDataService _data;
    private readonly IServiceProvider _services;

    private readonly Picker _filter = new() { Title = "Status", ItemsSource = Filters };
    private readonly VerticalStackLayout _listBody = new() { Spacing = 8 };

    private IReadOnlyList<FormSubmissionSummaryDto> _submissions = Array.Empty<FormSubmissionSummaryDto>();

    /// <summary>Builds the submissions review list over the manager data service.</summary>
    public FormReviewListPage(ManagerDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
        Title = "Submissions";
        _filter.SelectedIndexChanged += (_, _) => Render();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Submissions", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    _filter,
                    _listBody,
                },
            },
        };
    }

    /// <summary>Reloads submissions each time the page appears (e.g. after approving/rejecting one).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _submissions = await _data.GetFormSubmissionsAsync() ?? Array.Empty<FormSubmissionSummaryDto>();
        Render();
    }

    private void Render()
    {
        _listBody.Children.Clear();
        var filter = _filter.SelectedIndex >= 0 ? Filters[_filter.SelectedIndex] : "All";
        var rows = filter switch
        {
            "Awaiting review" => _submissions.Where(s => s.Status == "Submitted"),
            "Approved" => _submissions.Where(s => s.Status == "Approved"),
            "Rejected" => _submissions.Where(s => s.Status == "Rejected"),
            _ => _submissions,
        };
        var list = rows.ToList();

        if (list.Count == 0)
        {
            _listBody.Children.Add(new TwEmptyState { Glyph = "✅", Message = "No submissions to show." });
            return;
        }

        foreach (var s in list)
        {
            _listBody.Children.Add(Row(s));
        }
    }

    private View Row(FormSubmissionSummaryDto s)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = s.FormName, FontAttributes = FontAttributes.Bold, FontSize = 15 },
                ManagerUi.Muted($"{s.SubmittedBy} · {UkTime.Format(s.SubmittedUtc, "dd MMM HH:mm")}"),
            },
        }, 0, 0);
        grid.Add(new TwStatusPill { Text = s.Status, Kind = ManagerUi.ForFormStatus(s.Status) }, 1, 0);

        return ManagerUi.TappableCard(grid, async () =>
        {
            var page = _services.GetRequiredService<FormReviewPage>();
            page.Load(s.Id, s.FormName);
            await Navigation.PushAsync(page);
        });
    }
}
