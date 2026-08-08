using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies server-side quiz scoring (R5): the correct count is computed from the template's answers against
/// the submitted answers, and the pass mark decides the outcome. Missing or wrong answers do not count.
/// </summary>
public sealed class InductionQuizTests
{
    private static readonly IReadOnlyList<InductionQuizQuestion> Questions = new[]
    {
        new InductionQuizQuestion("q1", "?", new[] { "a", "b" }, CorrectOptionIndex: 0),
        new InductionQuizQuestion("q2", "?", new[] { "a", "b" }, CorrectOptionIndex: 1),
        new InductionQuizQuestion("q3", "?", new[] { "a", "b" }, CorrectOptionIndex: 0),
    };

    [Fact]
    public void AllCorrect_Passes()
    {
        var answers = new Dictionary<string, int> { ["q1"] = 0, ["q2"] = 1, ["q3"] = 0 };

        var result = InductionQuiz.Score(Questions, answers, passMark: 3);

        Assert.Equal(3, result.Correct);
        Assert.Equal(3, result.Total);
        Assert.True(result.Passed);
    }

    [Fact]
    public void BelowPassMark_Fails()
    {
        var answers = new Dictionary<string, int> { ["q1"] = 0, ["q2"] = 0, ["q3"] = 1 };   // only q1 correct

        var result = InductionQuiz.Score(Questions, answers, passMark: 3);

        Assert.Equal(1, result.Correct);
        Assert.False(result.Passed);
    }

    [Fact]
    public void MissingAnswers_DoNotCount()
    {
        var result = InductionQuiz.Score(Questions, new Dictionary<string, int> { ["q1"] = 0 }, passMark: 2);

        Assert.Equal(1, result.Correct);
        Assert.False(result.Passed);
    }
}
