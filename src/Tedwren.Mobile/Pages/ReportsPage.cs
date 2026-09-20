using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager reports surface (M7): a summary of the compliance evidence pack (section counts + total), reusing the
/// console <c>/api/evidence/summary</c>. The full multi-file ZIP export stays on the web console; on mobile this is a
/// read-only overview. The pack is <c>hse</c>-gated, so a company without the module sees a "not enabled" state.
/// </summary>
public class ReportsPage : ContentPage
{
    private readonly ManagerEvidenceApiClient _evidence;

    private readonly Label _generated = new() { FontSize = 12, IsVisible = false };
    private readonly VerticalStackLayout _body = new() { Spacing = 8 };

    /// <summary>Builds the reports page over the manager evidence client.</summary>
    public ReportsPage(ManagerEvidenceApiClient evidence)
    {
        _evidence = evidence;
        Title = "Reports";
        _generated.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Evidence pack", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "A summary of the compliance evidence available for export. Download the full pack from the web console.", FontSize = 13 },
                    _generated,
                    new TwCard { Content = _body },
                },
            },
        };
    }

    /// <summary>Loads the evidence-pack summary each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _body.Children.Clear();
        try
        {
            var summary = await _evidence.GetEvidenceSummaryAsync();
            if (summary is null)
            {
                _body.Children.Add(ManagerUi.Muted("No evidence summary available."));
                return;
            }

            _generated.IsVisible = true;
            _generated.Text = $"As at {UkTime.Format(summary.GeneratedUtc, "dd MMM yyyy HH:mm")} · {summary.TotalRecords} records";

            if (summary.Sections.Count == 0)
            {
                _body.Children.Add(ManagerUi.Muted("No records yet."));
            }

            foreach (var s in summary.Sections)
            {
                _body.Children.Add(SectionRow(s));
            }
        }
        catch (ApiException)
        {
            _body.Children.Add(new TwEmptyState { Glyph = "📊", Message = "The evidence pack isn't enabled for your organisation." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _body.Children.Add(ManagerUi.Muted("Couldn't load the evidence summary."));
        }
    }

    private static View SectionRow(EvidenceSectionDto s)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new Label { Text = s.Name, FontSize = 14 }, 0, 0);
        grid.Add(new Label { Text = s.Count.ToString(), FontAttributes = FontAttributes.Bold }, 1, 0);
        return grid;
    }
}
