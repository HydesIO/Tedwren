namespace Tedwren.Domain.Entities;

/// <summary>
/// A single quiz question in an induction template. The <see cref="CorrectOptionIndex"/> is held
/// <b>server-side only</b> and must never be included in anything sent to the operative's device (R5); the
/// device receives the prompt and options alone, and answers are scored on the server.
/// </summary>
public sealed record InductionQuizQuestion(string Id, string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex);
