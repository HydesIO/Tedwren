namespace Tedwren.Web.Leads;

/// <summary>
/// The seam that delivers a captured launch-list email to where it is stored (Web Content Spec §6.9). The
/// marketing site captures and validates; this forwards the address to the API's launch-list endpoint. A seam
/// (rather than a direct call) keeps the controller testable and lets the delivery mechanism change without
/// controller changes, matching <see cref="ILeadRouter"/>.
/// </summary>
public interface ILaunchSignupSink
{
    /// <summary>Forwards a validated launch-list email for storage. Never throws — a delivery failure is swallowed so the visitor still sees the confirmation.</summary>
    /// <param name="email">The email address to add.</param>
    /// <param name="source">Where the signup came from (e.g. "landing").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubmitAsync(string email, string source, CancellationToken cancellationToken = default);
}
