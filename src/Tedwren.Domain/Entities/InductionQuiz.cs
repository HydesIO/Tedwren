namespace Tedwren.Domain.Entities;

/// <summary>
/// Scores an induction quiz <b>on the server</b> (R5): given the template's questions (which carry the correct
/// answers) and the operative's submitted answers, it returns the number correct and whether the pass mark is
/// met. Kept pure so scoring is deterministic and unit-testable, and so the correct answers never have to leave
/// the server to grade an attempt.
/// </summary>
public static class InductionQuiz
{
    /// <summary>The outcome of a scored attempt.</summary>
    public sealed record Result(int Correct, int Total, bool Passed);

    /// <summary>Scores answers (question id → chosen option index) against the questions and pass mark.</summary>
    public static Result Score(IReadOnlyList<InductionQuizQuestion> questions, IReadOnlyDictionary<string, int> answers, int passMark)
    {
        var correct = questions.Count(q => answers.TryGetValue(q.Id, out var chosen) && chosen == q.CorrectOptionIndex);
        return new Result(correct, questions.Count, correct >= passMark);
    }
}
