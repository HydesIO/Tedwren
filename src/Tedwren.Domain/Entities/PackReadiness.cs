using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// Evaluates whether the operatives in a pack are site-ready (SUB-14). Not-site-ready problems must be
/// surfaced to the sender <b>before</b> the pack goes out, so an expired card is never sent unnoticed; the
/// sender must explicitly acknowledge blocking issues to proceed. Kept pure so the check is deterministic and
/// unit-testable.
/// </summary>
public static class PackReadiness
{
    /// <summary>One readiness problem found in a pack's subjects.</summary>
    public sealed record Issue(string SubjectName, string QualificationName, bool Blocking, string Detail);

    /// <summary>Returns the readiness problems across all subjects — expired cards block, expiring-soon cards warn.</summary>
    public static IReadOnlyList<Issue> Evaluate(IEnumerable<PackSubject> subjects)
    {
        var issues = new List<Issue>();
        foreach (var subject in subjects)
        {
            foreach (var card in subject.Cards)
            {
                switch (card.Status)
                {
                    case CardStatus.Expired:
                        issues.Add(new Issue(subject.Name, card.QualificationName, Blocking: true, "Card has expired"));
                        break;
                    case CardStatus.ExpiringSoon:
                        issues.Add(new Issue(subject.Name, card.QualificationName, Blocking: false, "Card is expiring soon"));
                        break;
                }
            }
        }

        return issues;
    }

    /// <summary>Whether any evaluated issue is blocking (an expired card) — requires explicit acknowledgement to send.</summary>
    public static bool HasBlocking(IEnumerable<Issue> issues) => issues.Any(i => i.Blocking);
}
