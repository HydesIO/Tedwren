using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The operative "forms due" inbox (M6): the forms assigned to them (cache-then-network), each with a schedule-aware
/// due/overdue badge (<see cref="FormsDueCalculator"/>) and an in-progress-draft marker, tapping through to the
/// offline fill page. Works offline from the cached assignment list.
/// </summary>
public class FormsInboxPage : ContentPage
{
    private const string AssignmentsKey = "forms.assignments";
    internal const string LastSubmittedKey = "forms.lastSubmitted";

    private readonly FormsApiClient _api;
    private readonly IReadCache _cache;
    private readonly IConnectivityService _connectivity;
    private readonly IFormDraftStore _drafts;
    private readonly IServiceProvider _services;

    private readonly VerticalStackLayout _list = new() { Spacing = 12 };

    /// <summary>Builds the inbox page.</summary>
    public FormsInboxPage(FormsApiClient api, IReadCache cache, IConnectivityService connectivity, IFormDraftStore drafts, IServiceProvider services)
    {
        _api = api;
        _cache = cache;
        _connectivity = connectivity;
        _drafts = drafts;
        _services = services;
        Title = "Forms due";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { new Label { Text = "Your forms", FontSize = 22, FontAttributes = FontAttributes.Bold }, _list },
            },
        };
    }

    /// <summary>Reloads the assignment list each time the page appears (a submit updates due state on return).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _list.Children.Clear();
        var assignments = await LoadAssignmentsAsync();
        var lastSubmitted = await _cache.GetAsync<Dictionary<Guid, DateTimeOffset>>(LastSubmittedKey) ?? new Dictionary<Guid, DateTimeOffset>();
        var now = DateTimeOffset.UtcNow;

        if (assignments.Count == 0)
        {
            _list.Children.Add(new Label { Text = "No forms assigned to you." });
            return;
        }

        foreach (var assignment in assignments)
        {
            DateTimeOffset? last = lastSubmitted.TryGetValue(assignment.FamilyId, out var t) ? t : null;
            var due = FormsDueCalculator.Evaluate(assignment.Schedule, last, now);
            var hasDraft = await _drafts.GetAsync(assignment.AssignmentId) is not null;
            _list.Children.Add(BuildCard(assignment, due, hasDraft));
        }
    }

    private View BuildCard(MobileFormAssignmentDto assignment, FormDueState due, bool hasDraft)
    {
        var subtitleText = assignment.SiteName is { } site ? $"{assignment.Schedule} · {site}" : assignment.Schedule;
        var subtitle = new Label { Text = subtitleText, FontSize = 13 };
        subtitle.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);

        var badge = new Label
        {
            Text = hasDraft ? "In progress" : due.ToString(),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
        };
        var (light, dark) = due switch
        {
            FormDueState.Overdue => (TwPalette.DangerLight, TwPalette.DangerDark),
            FormDueState.Due => (TwPalette.WarningLight, TwPalette.WarningDark),
            _ => (TwPalette.SuccessLight, TwPalette.SuccessDark),
        };
        badge.SetAppThemeColor(Label.TextColorProperty, light, dark);

        var card = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = assignment.FormName, FontAttributes = FontAttributes.Bold },
                    subtitle,
                    badge,
                },
            },
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OpenAsync(assignment)) });
        return card;
    }

    private async Task OpenAsync(MobileFormAssignmentDto assignment)
    {
        var page = _services.GetRequiredService<FormFillPage>();
        page.Load(assignment);
        await Navigation.PushAsync(page);
    }

    private async Task<IReadOnlyList<MobileFormAssignmentDto>> LoadAssignmentsAsync()
    {
        if (_connectivity.IsConnected)
        {
            try
            {
                var fresh = await _api.GetAssignmentsAsync();
                await _cache.SetAsync(AssignmentsKey, fresh);
                return fresh;
            }
            catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException)
            {
                // fall through to the cached copy — the inbox stays usable offline.
            }
        }

        return await _cache.GetAsync<IReadOnlyList<MobileFormAssignmentDto>>(AssignmentsKey) ?? Array.Empty<MobileFormAssignmentDto>();
    }
}
