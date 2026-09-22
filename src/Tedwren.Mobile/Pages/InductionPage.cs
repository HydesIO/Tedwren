using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The operative induction take-flow (Subcontractor Onboarding spec Stage 4 / Gate 4) — the native mirror of the
/// emulator's <c>Induction.razor</c>. It resolves the operative's main-contractor induction, works through the
/// steps, takes the server-scored quiz (R5) and signs + consents to finalise, receiving the induction number.
/// A spent attempt limit prompts a manager reset (MC-6). The two heads share <see cref="InductionApiClient"/>, so
/// the API behaviour is identical; only this MAUI UI differs from the Blazor page.
/// </summary>
public class InductionPage : ContentPage
{
    private readonly InductionApiClient _api;
    private readonly VerticalStackLayout _root = new() { Spacing = 16 };
    private readonly Entry _nameEntry = new() { Placeholder = "e.g. Sam Taylor" };
    private readonly CheckBox _consent = new();

    private InductionSessionDto? _session;
    private QuizResultDto? _quizResult;
    private readonly Dictionary<string, int> _answers = new();
    private bool _busy;
    private bool _quizPassed;
    private bool _loaded;

    /// <summary>Builds the induction page shell; the body is rendered once the session loads.</summary>
    public InductionPage(InductionApiClient api)
    {
        _api = api;
        Title = "Induction";
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 16, Children = { _root } } };
    }

    /// <summary>Loads (resuming or starting) the operative's current induction once when the page first appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _root.Children.Add(new Label { Text = "Loading your induction…" });
        try
        {
            _session = await _api.GetCurrentAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _session = null;
        }

        Render();
    }

    /// <summary>Rebuilds the page body from the current state (native has no reactive re-render).</summary>
    private void Render()
    {
        _root.Children.Clear();

        if (_session is null)
        {
            _root.Children.Add(Header("No induction assigned"));
            _root.Children.Add(Secondary("Your main contractor hasn't set up an induction for you yet."));
            return;
        }

        if (_session.Status == "Passed")
        {
            _root.Children.Add(Header(_session.TemplateName));
            var lines = new VerticalStackLayout { Spacing = 4 };
            lines.Children.Add(new Label { Text = "Induction complete", FontAttributes = FontAttributes.Bold });
            lines.Children.Add(Secondary($"Reference {_session.CompletionReference}"));
            if (_session.ExpiresUtc is { } expiry)
            {
                lines.Children.Add(Secondary($"Valid until {expiry:dd MMM yyyy}"));
            }

            _root.Children.Add(new TwCard { Content = lines });
            return;
        }

        _root.Children.Add(Header(_session.TemplateName));

        // Steps.
        _root.Children.Add(SectionLabel("Steps"));
        var steps = new VerticalStackLayout { Spacing = 12 };
        foreach (var step in _session.Steps)
        {
            var done = _session.CompletedStepIds.Contains(step.Id);
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            row.Add(new Label { Text = step.Label + (step.Required ? string.Empty : " (optional)"), VerticalOptions = LayoutOptions.Center }, 0, 0);
            if (done)
            {
                row.Add(new Label { Text = "Done ✓", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 1, 0);
            }
            else
            {
                var stepId = step.Id;
                var mark = new Button { Text = "Mark as read", IsEnabled = !_busy };
                mark.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
                mark.Clicked += async (_, _) => await CompleteStepAsync(stepId);
                row.Add(mark, 1, 0);
            }

            steps.Children.Add(row);
        }

        _root.Children.Add(new TwCard { Content = steps });

        // Quiz.
        if (_session.Questions.Count > 0)
        {
            _root.Children.Add(SectionLabel("Quiz"));
            var quiz = new VerticalStackLayout { Spacing = 12 };
            foreach (var q in _session.Questions)
            {
                quiz.Children.Add(new Label { Text = q.Prompt, FontAttributes = FontAttributes.Bold });
                for (var i = 0; i < q.Options.Count; i++)
                {
                    var questionId = q.Id;
                    var optionIndex = i;
                    var radio = new RadioButton
                    {
                        Content = q.Options[i],
                        GroupName = q.Id,
                        IsChecked = _answers.TryGetValue(q.Id, out var chosen) && chosen == i,
                    };
                    radio.CheckedChanged += (_, e) =>
                    {
                        if (e.Value)
                        {
                            _answers[questionId] = optionIndex;
                        }
                    };
                    quiz.Children.Add(radio);
                }
            }

            var submit = new Button { Text = "Submit answers", IsEnabled = !_busy && !_quizPassed };
            submit.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
            submit.Clicked += async (_, _) => await SubmitQuizAsync();
            quiz.Children.Add(submit);

            if (_quizResult is not null)
            {
                quiz.Children.Add(Secondary(QuizResultText()));
            }

            _root.Children.Add(new TwCard { Content = quiz });
        }

        // Sign & complete.
        _root.Children.Add(SectionLabel("Sign & complete"));
        var sign = new VerticalStackLayout { Spacing = 8 };
        sign.Children.Add(new Label { Text = "Your full name" });
        sign.Children.Add(_nameEntry);
        sign.Children.Add(new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _consent, new Label { Text = "I consent to my induction record being stored (optional).", VerticalOptions = LayoutOptions.Center } },
        });

        var finalize = new Button { Text = "Complete induction", IsEnabled = CanFinalize };
        finalize.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        finalize.Clicked += async (_, _) => await FinalizeAsync();
        sign.Children.Add(finalize);
        if (!CanFinalize)
        {
            sign.Children.Add(Secondary(FinalizeHint));
        }

        _root.Children.Add(new TwCard { Content = sign });
    }

    /// <summary>All required steps are complete (a precondition for finalising, MC-4).</summary>
    private bool RequiredStepsDone =>
        _session is not null && _session.Steps.Where(s => s.Required).All(s => _session.CompletedStepIds.Contains(s.Id));

    /// <summary>The quiz is satisfied — passed this session, or the template has no quiz.</summary>
    private bool QuizSatisfied => _session is not null && (_session.Questions.Count == 0 || _quizPassed);

    /// <summary>Whether the induction can be finalised now (MC-4/MC-5).</summary>
    private bool CanFinalize => !_busy && RequiredStepsDone && QuizSatisfied && !string.IsNullOrWhiteSpace(_nameEntry.Text);

    /// <summary>A short hint explaining why the complete button is disabled.</summary>
    private string FinalizeHint => !RequiredStepsDone
        ? "Complete every required step first."
        : !QuizSatisfied ? "Pass the quiz to continue." : "Enter your name to sign.";

    private string QuizResultText() => _quizResult!.Passed
        ? $"Passed — {_quizResult.Correct} of {_quizResult.Total} correct."
        : _quizResult.AttemptsExhausted
            ? $"No attempts left ({_quizResult.Correct} of {_quizResult.Total}). Ask your manager to reset your induction."
            : $"Not passed — {_quizResult.Correct} of {_quizResult.Total} correct. Review the steps and try again.";

    private async Task CompleteStepAsync(string stepId)
    {
        if (_busy || _session is null)
        {
            return;
        }

        _busy = true;
        try
        {
            var updated = await _api.CompleteStepAsync(_session.Id, stepId);
            if (updated is not null)
            {
                _session = updated;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlert("Induction", "Couldn't save that step. Check your connection and try again.", "OK");
        }
        finally
        {
            _busy = false;
            Render();
        }
    }

    private async Task SubmitQuizAsync()
    {
        if (_busy || _session is null)
        {
            return;
        }

        _busy = true;
        try
        {
            _quizResult = await _api.SubmitQuizAsync(_session.Id, new SubmitQuizRequest(_answers));
            _quizPassed = _quizResult?.Passed == true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlert("Induction", "Couldn't score the quiz. Check your connection and try again.", "OK");
        }
        finally
        {
            _busy = false;
            Render();
        }
    }

    private async Task FinalizeAsync()
    {
        if (!CanFinalize || _session is null)
        {
            return;
        }

        _busy = true;
        try
        {
            var done = await _api.FinalizeAsync(_session.Id, new FinalizeInductionRequest(_nameEntry.Text!.Trim(), _consent.IsChecked));
            if (done is null)
            {
                await DisplayAlert("Induction", "Couldn't complete the induction. Make sure the steps are done and the quiz is passed.", "OK");
            }
            else
            {
                _session = done;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlert("Induction", "Couldn't complete the induction. Check your connection and try again.", "OK");
        }
        finally
        {
            _busy = false;
            Render();
        }
    }

    private static Label Header(string text) => new() { Text = text, FontSize = 22, FontAttributes = FontAttributes.Bold };

    private static Label SectionLabel(string text) => new() { Text = text, FontSize = 18, FontAttributes = FontAttributes.Bold };

    private static Label Secondary(string text)
    {
        var label = new Label { Text = text, FontSize = 13 };
        label.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return label;
    }
}
