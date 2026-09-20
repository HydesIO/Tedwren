using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager operative register (M7): the company's operatives with compliance status, filterable by name/trade
/// (client-side), each tapping through to the operative's profile. Loaded via <see cref="ManagerDataService"/>
/// (cache-then-network) so it stays readable offline.
/// </summary>
public class OperativesPage : ContentPage
{
    private readonly ManagerDataService _data;
    private readonly IServiceProvider _services;

    private readonly SearchBar _search = new() { Placeholder = "Search name or trade" };
    private readonly VerticalStackLayout _listBody = new() { Spacing = 8 };

    private IReadOnlyList<OperativeListItemDto> _operatives = Array.Empty<OperativeListItemDto>();

    /// <summary>Builds the operatives register over the manager data service.</summary>
    public OperativesPage(ManagerDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
        Title = "Operatives";
        _search.TextChanged += (_, _) => Render();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Operatives", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    _search,
                    _listBody,
                },
            },
        };
    }

    /// <summary>Loads the register each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _operatives = await _data.GetOperativesAsync() ?? Array.Empty<OperativeListItemDto>();
        Render();
    }

    private void Render()
    {
        _listBody.Children.Clear();
        var term = _search.Text?.Trim();
        var rows = string.IsNullOrEmpty(term)
            ? _operatives
            : _operatives.Where(o =>
                o.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (o.Trade?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        if (rows.Count == 0)
        {
            _listBody.Children.Add(new TwEmptyState { Glyph = "🧑‍🔧", Message = "No operatives match." });
            return;
        }

        foreach (var op in rows)
        {
            _listBody.Children.Add(Row(op));
        }
    }

    private View Row(OperativeListItemDto op)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        var text = new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = op.Name, FontAttributes = FontAttributes.Bold, FontSize = 15 },
                ManagerUi.Muted($"{op.Trade ?? "—"} · {op.Company}"),
            },
        };
        grid.Add(text, 0, 0);
        grid.Add(new TwStatusPill { Text = op.StatusLabel, Kind = ManagerUi.ForCompliance(op.State) }, 1, 0);

        return ManagerUi.TappableCard(grid, async () =>
        {
            var page = _services.GetRequiredService<OperativeDetailPage>();
            page.Load(op.Slug, op.Name);
            await Navigation.PushAsync(page);
        });
    }
}
