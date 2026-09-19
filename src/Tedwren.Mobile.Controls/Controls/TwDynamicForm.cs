using System.Globalization;
using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// Native renderer for a Tedwren form (M6) — the MAUI counterpart of the web <c>DynamicFormRenderer</c>. It builds
/// controls for all 14 <c>FormFieldKind</c>s from a template's <see cref="FormSectionDto"/> list and exposes the
/// same output contract: <see cref="GetAnswers"/> → <c>FormAnswerDto[]</c> and <see cref="GetFiles"/> →
/// <c>FormSubmissionFileInput[]</c> (base64), with signatures captured inline on the answer value as a PNG data URL.
/// Raises <see cref="Changed"/> after every edit so the fill page can autosave a draft. Built from raw MAUI controls
/// + Tedwren tokens (no field styles exist in the kit yet).
/// </summary>
public sealed class TwDynamicForm : VerticalStackLayout
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _multi = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<PendingFile>> _files = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TwSignaturePad> _signatures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _resumedSignatures = new(StringComparer.Ordinal);

    /// <summary>Creates an empty form layout; call <see cref="Load"/> to build it from a template.</summary>
    public TwDynamicForm() => Spacing = 16;

    /// <summary>Raised after any field edit (for draft autosave).</summary>
    public event EventHandler? Changed;

    /// <summary>Builds the form controls from a template's sections, optionally resuming a saved draft.</summary>
    public void Load(
        IReadOnlyList<FormSectionDto> sections,
        IReadOnlyList<FormAnswerDto>? resumeAnswers = null,
        IReadOnlyList<FormSubmissionFileInput>? resumeFiles = null)
    {
        Children.Clear();
        _values.Clear();
        _multi.Clear();
        _files.Clear();
        _signatures.Clear();
        _resumedSignatures.Clear();

        var answersById = (resumeAnswers ?? Array.Empty<FormAnswerDto>()).ToDictionary(a => a.FieldId, a => a, StringComparer.Ordinal);
        foreach (var file in resumeFiles ?? Array.Empty<FormSubmissionFileInput>())
        {
            _files.TryAdd(file.FieldId, new List<PendingFile>());
            _files[file.FieldId].Add(new PendingFile(file.FileName, file.ContentType, file.ContentBase64));
        }

        foreach (var section in sections.OrderBy(s => s.Order))
        {
            Children.Add(new Label { Text = section.Title, FontSize = 18, FontAttributes = FontAttributes.Bold });
            foreach (var field in section.Fields.OrderBy(f => f.Order))
            {
                Children.Add(BuildField(field, answersById.GetValueOrDefault(field.Id)));
            }
        }
    }

    /// <summary>The captured answers (single-valued in <c>Value</c>, multi-select in <c>Values</c>, signature as a PNG data URL).</summary>
    public IReadOnlyList<FormAnswerDto> GetAnswers()
    {
        var answers = new List<FormAnswerDto>();
        foreach (var (id, value) in _values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                answers.Add(new FormAnswerDto(id, value, Array.Empty<string>()));
            }
        }

        foreach (var (id, set) in _multi)
        {
            if (set.Count > 0)
            {
                answers.Add(new FormAnswerDto(id, null, set.ToList()));
            }
        }

        foreach (var (id, pad) in _signatures)
        {
            var url = pad.ToPngDataUrl() ?? _resumedSignatures.GetValueOrDefault(id);
            if (!string.IsNullOrEmpty(url))
            {
                answers.Add(new FormAnswerDto(id, url, Array.Empty<string>()));
            }
        }

        return answers;
    }

    /// <summary>The captured files (photos / uploads), base64, keyed by field id.</summary>
    public IReadOnlyList<FormSubmissionFileInput> GetFiles() =>
        _files.SelectMany(kv => kv.Value.Select(f => new FormSubmissionFileInput(kv.Key, f.FileName, f.ContentType, f.Base64))).ToList();

    // ---- field building --------------------------------------------------------------------------------

    private View BuildField(FormFieldDto field, FormAnswerDto? resume) => field.Kind switch
    {
        "Heading" => new Label { Text = field.Label, FontSize = 16, FontAttributes = FontAttributes.Bold },
        "Instruction" => Secondary(new Label { Text = field.Label, FontSize = 13 }),
        "LongText" => Wrap(field, BuildEntry(field, resume?.Value, lines: 4)),
        "Number" => Wrap(field, BuildEntry(field, resume?.Value, keyboard: Keyboard.Numeric)),
        "Date" => Wrap(field, BuildDate(field, resume?.Value)),
        "Time" => Wrap(field, BuildTime(field, resume?.Value)),
        "Dropdown" => Wrap(field, BuildDropdown(field, resume?.Value)),
        "MultiSelect" => Wrap(field, BuildMultiSelect(field, resume?.Values)),
        "YesNo" => Wrap(field, BuildYesNo(field, resume?.Value)),
        "RagStatus" => Wrap(field, BuildRag(field, resume?.Value)),
        "Photo" => Wrap(field, BuildFile(field, image: true)),
        "FileUpload" => Wrap(field, BuildFile(field, image: false)),
        "Signature" => Wrap(field, BuildSignature(field, resume?.Value)),
        _ => Wrap(field, BuildEntry(field, resume?.Value)), // ShortText + any unknown kind
    };

    /// <summary>Wraps a field's control with its label + required marker.</summary>
    private static View Wrap(FormFieldDto field, View control)
    {
        var label = new Label { Text = field.Required ? field.Label + " *" : field.Label, FontAttributes = FontAttributes.Bold, FontSize = 14 };
        var stack = new VerticalStackLayout { Spacing = 6, Children = { label } };
        if (!string.IsNullOrWhiteSpace(field.HelpText))
        {
            stack.Children.Add(Secondary(new Label { Text = field.HelpText, FontSize = 12 }));
        }

        stack.Children.Add(control);
        return stack;
    }

    private static Label Secondary(Label label)
    {
        label.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return label;
    }

    private View BuildEntry(FormFieldDto field, string? resume, int lines = 1, Keyboard? keyboard = null)
    {
        if (lines > 1)
        {
            var editor = new Editor { HeightRequest = 24 * lines, Text = resume };
            _values[field.Id] = resume;
            editor.TextChanged += (_, e) => Set(field.Id, e.NewTextValue);
            return editor;
        }

        var entry = new Entry { Text = resume, Keyboard = keyboard ?? Keyboard.Default };
        _values[field.Id] = resume;
        entry.TextChanged += (_, e) => Set(field.Id, e.NewTextValue);
        return entry;
    }

    private View BuildDate(FormFieldDto field, string? resume)
    {
        var picker = new DatePicker { Format = "yyyy-MM-dd" };
        if (DateTime.TryParse(resume, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            picker.Date = date;
            _values[field.Id] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        picker.DateSelected += (_, e) => Set(field.Id, e.NewDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return picker;
    }

    private View BuildTime(FormFieldDto field, string? resume)
    {
        var picker = new TimePicker { Format = "HH:mm" };
        if (TimeSpan.TryParse(resume, CultureInfo.InvariantCulture, out var time))
        {
            picker.Time = time;
            _values[field.Id] = FormatTime(time);
        }

        picker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
            {
                Set(field.Id, FormatTime(picker.Time));
            }
        };
        return picker;
    }

    private static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private View BuildDropdown(FormFieldDto field, string? resume)
    {
        var options = ParseOptions(field.OptionsJson);
        var picker = new Picker { Title = "Select…", ItemsSource = options.ToList() };
        if (resume is not null && options.Contains(resume))
        {
            picker.SelectedItem = resume;
            _values[field.Id] = resume;
        }

        picker.SelectedIndexChanged += (_, _) => Set(field.Id, picker.SelectedItem as string);
        return picker;
    }

    private View BuildMultiSelect(FormFieldDto field, IReadOnlyList<string>? resume)
    {
        var selected = new HashSet<string>(resume ?? Array.Empty<string>(), StringComparer.Ordinal);
        _multi[field.Id] = selected;
        var stack = new VerticalStackLayout { Spacing = 8 };
        foreach (var option in ParseOptions(field.OptionsJson))
        {
            var toggle = new Switch { IsToggled = selected.Contains(option) };
            toggle.Toggled += (_, e) =>
            {
                if (e.Value)
                {
                    selected.Add(option);
                }
                else
                {
                    selected.Remove(option);
                }

                Changed?.Invoke(this, EventArgs.Empty);
            };
            stack.Children.Add(new HorizontalStackLayout { Spacing = 12, Children = { toggle, new Label { Text = option, VerticalOptions = LayoutOptions.Center } } });
        }

        return stack;
    }

    private View BuildYesNo(FormFieldDto field, string? resume)
    {
        var toggle = new Switch { IsToggled = string.Equals(resume, "true", StringComparison.OrdinalIgnoreCase) };
        _values[field.Id] = toggle.IsToggled ? "true" : "false";
        toggle.Toggled += (_, e) => Set(field.Id, e.Value ? "true" : "false");
        return new HorizontalStackLayout { Spacing = 12, Children = { toggle, new Label { Text = "Yes / No", VerticalOptions = LayoutOptions.Center } } };
    }

    private View BuildRag(FormFieldDto field, string? resume)
    {
        _values[field.Id] = resume;
        var row = new HorizontalStackLayout { Spacing = 8 };
        var buttons = new List<Button>();
        foreach (var (label, colour) in new[] { ("Red", TwPalette.DangerLight), ("Amber", TwPalette.WarningLight), ("Green", TwPalette.SuccessLight) })
        {
            var button = new Button { Text = label, BackgroundColor = string.Equals(resume, label, StringComparison.OrdinalIgnoreCase) ? colour : Colors.Transparent, TextColor = colour, BorderColor = colour, BorderWidth = 1 };
            button.Clicked += (_, _) =>
            {
                Set(field.Id, label);
                foreach (var b in buttons)
                {
                    b.BackgroundColor = b == button ? colour : Colors.Transparent;
                }
            };
            buttons.Add(button);
            row.Children.Add(button);
        }

        return row;
    }

    private View BuildFile(FormFieldDto field, bool image)
    {
        _files.TryAdd(field.Id, new List<PendingFile>());
        var status = Secondary(new Label { FontSize = 12, Text = _files[field.Id].Count > 0 ? $"{_files[field.Id].Count} file(s) attached" : "No file yet" });
        var error = new Label { IsVisible = false, FontSize = 12 };
        error.SetAppThemeColor(Label.TextColorProperty, TwPalette.DangerLight, TwPalette.DangerDark);

        var button = new Button { Text = image ? "Take photo" : "Choose file" };
        button.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        button.Clicked += async (_, _) =>
        {
            try
            {
                var picked = image ? await CapturePhotoAsync() : await PickFileAsync();
                if (picked is null)
                {
                    return;
                }

                _files[field.Id].Add(picked);
                status.Text = $"{_files[field.Id].Count} file(s) attached";
                error.IsVisible = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException)
            {
                error.IsVisible = true;
                error.Text = image ? "Camera permission is needed." : "Couldn't open the file.";
            }
        };

        return new VerticalStackLayout { Spacing = 6, Children = { button, status, error } };
    }

    private View BuildSignature(FormFieldDto field, string? resume)
    {
        var pad = new TwSignaturePad();
        _signatures[field.Id] = pad;
        _resumedSignatures[field.Id] = resume; // preserved unless the operative draws a fresh signature
        pad.SetAppThemeColor(VisualElement.BackgroundColorProperty, TwPalette.SurfaceLight, TwPalette.SurfaceDark);

        var clear = new Button { Text = "Clear" };
        clear.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        clear.Clicked += (_, _) =>
        {
            pad.Clear();
            _resumedSignatures[field.Id] = null;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        return new VerticalStackLayout { Spacing = 6, Children = { pad, clear } };
    }

    private static async Task<PendingFile?> CapturePhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            return null;
        }

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo is null ? null : await ToPendingFileAsync(photo, "image/jpeg");
    }

    private static async Task<PendingFile?> PickFileAsync()
    {
        var file = await FilePicker.Default.PickAsync();
        return file is null ? null : await ToPendingFileAsync(file, "application/octet-stream");
    }

    private static async Task<PendingFile> ToPendingFileAsync(FileResult file, string fallbackContentType)
    {
        using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? fallbackContentType : file.ContentType;
        return new PendingFile(file.FileName, contentType, Convert.ToBase64String(memory.ToArray()));
    }

    private void Set(string fieldId, string? value)
    {
        _values[fieldId] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Parses an <c>OptionsJson</c> ([{id,text}] or a legacy ["a","b"]) into the option display texts.</summary>
    private static IReadOnlyList<string> ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var options = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    options.Add(element.GetString()!);
                }
                else if (element.ValueKind == JsonValueKind.Object
                         && (element.TryGetProperty("text", out var text) || element.TryGetProperty("Text", out text))
                         && text.ValueKind == JsonValueKind.String)
                {
                    options.Add(text.GetString()!);
                }
            }

            return options;
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>A captured file held in memory as base64 until submit.</summary>
    private sealed record PendingFile(string FileName, string ContentType, string Base64);
}
