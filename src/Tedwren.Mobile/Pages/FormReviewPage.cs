using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager submission review (M7): a completed form's answers (with RAG chips, inline signatures and photo
/// attachments), and — for a still-submitted form and a non-Auditor — approve / reject with a note. Field labels
/// are resolved from the submitted template version; images and attachments come through the authorised routes (R9).
/// </summary>
public class FormReviewPage : ContentPage
{
    private readonly ManagerFormsApiClient _forms;
    private readonly ManagerSessionManager _session;

    private readonly VerticalStackLayout _body = new() { Padding = new Thickness(16), Spacing = 16 };

    private Guid _id;
    private bool _loaded;

    /// <summary>Builds the review page over the forms client and the manager session (for the role gate).</summary>
    public FormReviewPage(ManagerFormsApiClient forms, ManagerSessionManager session)
    {
        _forms = forms;
        _session = session;
        Title = "Submission";
        Content = new ScrollView { Content = _body };
    }

    /// <summary>Sets the submission to review (called before navigation).</summary>
    public void Load(Guid id, string name)
    {
        _id = id;
        Title = name;
        _loaded = false;
    }

    /// <summary>Loads the submission (and its template, for labels) once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded || _id == Guid.Empty)
        {
            return;
        }

        _loaded = true;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _body.Children.Clear();
        FormSubmissionDetailDto? submission;
        try
        {
            submission = await _forms.GetSubmissionAsync(_id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            submission = null;
        }

        if (submission is null)
        {
            _body.Children.Add(new TwEmptyState { Glyph = "✅", Message = "Couldn't load this submission. Connect and try again." });
            return;
        }

        var fields = await ResolveFieldsAsync(submission.FormTemplateId);
        RenderHeader(submission);
        RenderAnswers(submission, fields);
        RenderAttachments(submission);
        RenderReviewActions(submission);
    }

    private async Task<IReadOnlyDictionary<string, FormFieldDto>> ResolveFieldsAsync(Guid templateVersionId)
    {
        try
        {
            var template = await _forms.GetTemplateAsync(templateVersionId);
            if (template is not null)
            {
                return template.Sections.SelectMany(s => s.Fields).ToDictionary(f => f.Id);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fall back to field ids when the template can't be loaded.
        }

        return new Dictionary<string, FormFieldDto>();
    }

    private void RenderHeader(FormSubmissionDetailDto s)
    {
        _body.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = s.FormName, FontSize = 22, FontAttributes = FontAttributes.Bold },
                ManagerUi.Muted($"v{s.FormTemplateVersion} · {s.Scope} · {s.SubmittedBy} · {UkTime.Format(s.SubmittedUtc, "dd MMM yyyy HH:mm")}"),
                new TwStatusPill { Text = s.Status, Kind = ManagerUi.ForFormStatus(s.Status) },
            },
        });

        if (!string.IsNullOrWhiteSpace(s.ReviewNote))
        {
            var note = new Label { Text = $"Review note: {s.ReviewNote}", Padding = new Thickness(12) };
            note.SetAppThemeColor(VisualElement.BackgroundColorProperty, TwPalette.WarningPaleLight, TwPalette.WarningPaleDark);
            note.SetAppThemeColor(Label.TextColorProperty, TwPalette.WarningLight, TwPalette.WarningDark);
            _body.Children.Add(note);
        }
    }

    private void RenderAnswers(FormSubmissionDetailDto s, IReadOnlyDictionary<string, FormFieldDto> fields)
    {
        var body = new VerticalStackLayout { Spacing = 12 };
        foreach (var answer in s.Answers)
        {
            var label = fields.TryGetValue(answer.FieldId, out var field) ? field.Label : answer.FieldId;

            if (FormAnswerFormatter.IsImage(answer) && DataUrlBytes(answer.Value!) is { } sigBytes)
            {
                body.Children.Add(AnswerImage(label, sigBytes));
                continue;
            }

            var isRag = (fields.TryGetValue(answer.FieldId, out var f) && f.Kind == "RagStatus") || answer.Value is "Red" or "Amber" or "Green";
            if (isRag && !string.IsNullOrWhiteSpace(answer.Value))
            {
                var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                grid.Add(new Label { Text = label, VerticalOptions = LayoutOptions.Center }, 0, 0);
                grid.Add(new TwStatusPill { Text = answer.Value!, Kind = ManagerUi.ForRag(answer.Value!) }, 1, 0);
                body.Children.Add(grid);
                continue;
            }

            var text = FormAnswerFormatter.Format(answer);
            if (!string.IsNullOrEmpty(text))
            {
                body.Children.Add(new VerticalStackLayout
                {
                    Spacing = 1,
                    Children = { ManagerUi.Muted(label), new Label { Text = text, FontSize = 14 } },
                });
            }
        }

        if (body.Children.Count == 0)
        {
            body.Children.Add(ManagerUi.Muted("No answers recorded."));
        }

        _body.Children.Add(Section("Responses", body));
    }

    private void RenderAttachments(FormSubmissionDetailDto s)
    {
        if (s.Files.Count == 0)
        {
            return;
        }

        var body = new VerticalStackLayout { Spacing = 10 };
        foreach (var file in s.Files)
        {
            var caption = ManagerUi.Muted(file.FileName);
            body.Children.Add(caption);
            if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var image = new Image { HeightRequest = 180, Aspect = Aspect.AspectFit, IsVisible = false };
                body.Children.Add(image);
                _ = LoadAttachmentAsync(image, file.Id);
            }
        }

        _body.Children.Add(Section("Attachments", body));
    }

    private void RenderReviewActions(FormSubmissionDetailDto s)
    {
        var isAuditor = string.Equals(_session.Current?.Role, "Auditor", StringComparison.OrdinalIgnoreCase);
        if (s.Status != "Submitted" || isAuditor)
        {
            return;
        }

        var approve = new Button { Text = "Approve" };
        approve.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        approve.Clicked += async (_, _) => await ReviewAsync(approve: true);

        var reject = new Button { Text = "Reject" };
        reject.SetDynamicResource(VisualElement.StyleProperty, "TwDestructiveButton");
        reject.Clicked += async (_, _) => await ReviewAsync(approve: false);

        _body.Children.Add(Section("Review", new VerticalStackLayout { Spacing = 10, Children = { approve, reject } }));
    }

    private async Task ReviewAsync(bool approve)
    {
        try
        {
            if (approve)
            {
                var note = await DisplayPromptAsync("Approve", "Add a note (optional):", accept: "Approve", cancel: "Cancel");
                if (note is null)
                {
                    return; // cancelled
                }

                await _forms.ApproveAsync(_id, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
            }
            else
            {
                var note = await DisplayPromptAsync("Reject", "Reason for rejection:", accept: "Reject", cancel: "Cancel");
                if (string.IsNullOrWhiteSpace(note))
                {
                    return; // a rejection needs a reason
                }

                await _forms.RejectAsync(_id, note.Trim());
            }

            await ReloadAsync();
        }
        catch (ApiException)
        {
            await DisplayAlert("Couldn't save", "You may not have permission, or the connection failed.", "OK");
        }
    }

    private async Task LoadAttachmentAsync(Image image, Guid fileId)
    {
        try
        {
            var bytes = await _forms.GetSubmissionFileAsync(fileId);
            if (bytes is { Length: > 0 })
            {
                image.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                image.IsVisible = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An attachment that can't be loaded simply stays hidden.
        }
    }

    private static View AnswerImage(string label, byte[] bytes) => new VerticalStackLayout
    {
        Spacing = 4,
        Children =
        {
            ManagerUi.Muted(label),
            new Image { Source = ImageSource.FromStream(() => new MemoryStream(bytes)), HeightRequest = 120, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Start },
        },
    };

    private static byte[]? DataUrlBytes(string value)
    {
        var comma = value.IndexOf(',');
        if (comma < 0)
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(value[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static View Section(string title, View body) => new VerticalStackLayout
    {
        Spacing = 8,
        Children =
        {
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold },
            new TwCard { Content = body },
        },
    };
}
