using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Completes an assigned form offline (M6): loads the published template (cache-first by immutable version id),
/// renders it with <see cref="TwDynamicForm"/>, autosaves a draft on every edit (resume on return), validates
/// required fields client-side, and on submit queues the whole submission to the encrypted outbox — synced
/// idempotently when online. Offline-first: the save always succeeds locally.
/// </summary>
public class FormFillPage : ContentPage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly FormsApiClient _api;
    private readonly IReadCache _cache;
    private readonly IConnectivityService _connectivity;
    private readonly IOutboxStore _outbox;
    private readonly SyncEngine _sync;
    private readonly IFormDraftStore _drafts;

    private readonly TwDynamicForm _form = new();
    private readonly Label _title = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, Text = "Form" };
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };
    private readonly Button _submit;

    private MobileFormAssignmentDto? _assignment;
    private FormTemplateDto? _template;
    private bool _loaded;
    private bool _busy;

    /// <summary>Builds the fill page.</summary>
    public FormFillPage(FormsApiClient api, IReadCache cache, IConnectivityService connectivity, IOutboxStore outbox, SyncEngine sync, IFormDraftStore drafts)
    {
        _api = api;
        _cache = cache;
        _connectivity = connectivity;
        _outbox = outbox;
        _sync = sync;
        _drafts = drafts;
        Title = "Complete form";

        _submit = new Button { Text = "Submit form", IsEnabled = false };
        _submit.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _submit.Clicked += OnSubmitAsync;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { _title, _form, _status, _submit },
            },
        };
    }

    /// <summary>Sets the assignment to complete (called before navigation).</summary>
    public void Load(MobileFormAssignmentDto assignment)
    {
        _assignment = assignment;
        _title.Text = assignment.FormName;
    }

    /// <summary>Loads the template + any saved draft once when the page first appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded || _assignment is null)
        {
            return;
        }

        _loaded = true;
        var assignment = _assignment;
        _template = await LoadTemplateAsync(assignment.TemplateVersionId);
        if (_template is null)
        {
            ShowStatus("Couldn't load this form. Connect and try again.", error: true);
            return;
        }

        var draft = await _drafts.GetAsync(assignment.AssignmentId);
        if (draft is not null && draft.TemplateVersionId == assignment.TemplateVersionId
            && JsonSerializer.Deserialize<DraftPayload>(draft.PayloadJson, Json) is { } saved)
        {
            _form.Load(_template.Sections, saved.Answers, saved.Files);
        }
        else
        {
            _form.Load(_template.Sections);
        }

        _form.Changed += OnFormChanged;
        _submit.IsEnabled = true;
    }

    private async void OnFormChanged(object? sender, EventArgs e) => await SaveDraftAsync();

    private async Task SaveDraftAsync()
    {
        if (_assignment is null)
        {
            return;
        }

        var assignment = _assignment;
        var payload = new DraftPayload(_form.GetAnswers(), _form.GetFiles());
        await _drafts.SaveAsync(new FormDraft
        {
            Key = assignment.AssignmentId,
            TemplateVersionId = assignment.TemplateVersionId,
            PayloadJson = JsonSerializer.Serialize(payload, Json),
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
    }

    private async void OnSubmitAsync(object? sender, EventArgs e)
    {
        if (_busy || _assignment is null || _template is null)
        {
            return;
        }

        var assignment = _assignment;
        var template = _template;
        var answers = _form.GetAnswers();
        var files = _form.GetFiles();
        var missing = FormValidation.MissingRequired(template, answers, files);
        if (missing.Count > 0)
        {
            await DisplayAlert("Not finished", "Please complete: " + string.Join(", ", missing), "OK");
            return;
        }

        _busy = true;
        _submit.IsEnabled = false;
        try
        {
            var request = new CreateFormSubmissionRequest(
                assignment.TemplateVersionId, assignment.Scope, assignment.SiteId, null, answers, files, ClientId: Guid.NewGuid());
            await _outbox.EnqueueAsync(new OutboxItem
            {
                Id = request.ClientId!.Value,
                Kind = FormsOutboxHandler.ItemKind,
                PayloadJson = JsonSerializer.Serialize(request, Json),
                CreatedUtc = DateTimeOffset.UtcNow,
            });
            _sync.RequestSync();

            await RecordSubmittedAsync(assignment.FamilyId);
            await _drafts.DeleteAsync(assignment.AssignmentId);

            await DisplayAlert("Saved", _connectivity.IsConnected
                ? "Your form is saved and syncing now."
                : "Your form is saved and will sync when you're online.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ShowStatus("Couldn't save. Please try again.", error: true);
            _busy = false;
            _submit.IsEnabled = true;
        }
    }

    /// <summary>Records the family's last-submitted time locally so the inbox's due calculation updates on return.</summary>
    private async Task RecordSubmittedAsync(Guid familyId)
    {
        var map = await _cache.GetAsync<Dictionary<Guid, DateTimeOffset>>(FormsInboxPage.LastSubmittedKey) ?? new Dictionary<Guid, DateTimeOffset>();
        map[familyId] = DateTimeOffset.UtcNow;
        await _cache.SetAsync(FormsInboxPage.LastSubmittedKey, map);
    }

    private async Task<FormTemplateDto?> LoadTemplateAsync(Guid versionId)
    {
        var key = $"forms.template.{versionId}";
        var cached = await _cache.GetAsync<FormTemplateDto>(key);
        if (cached is not null)
        {
            return cached; // published versions are immutable — the cache is authoritative.
        }

        if (!_connectivity.IsConnected)
        {
            return null;
        }

        try
        {
            var fresh = await _api.GetTemplateAsync(versionId);
            if (fresh is not null)
            {
                await _cache.SetAsync(key, fresh);
            }

            return fresh;
        }
        catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException)
        {
            return null;
        }
    }

    private void ShowStatus(string text, bool error)
    {
        _status.IsVisible = true;
        _status.Text = text;
        _status.SetAppThemeColor(Label.TextColorProperty,
            error ? TwPalette.DangerLight : TwPalette.SuccessLight,
            error ? TwPalette.DangerDark : TwPalette.SuccessDark);
    }

    /// <summary>The in-progress capture persisted in a draft.</summary>
    private sealed record DraftPayload(IReadOnlyList<FormAnswerDto> Answers, IReadOnlyList<FormSubmissionFileInput> Files);
}
