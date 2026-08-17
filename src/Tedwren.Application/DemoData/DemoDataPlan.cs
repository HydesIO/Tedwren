using Tedwren.Domain.Entities;

namespace Tedwren.Application.DemoData;

/// <summary>
/// The complete, deterministic set of records that make up the demonstration dataset. Produced once by
/// <see cref="DemoDataPlanBuilder"/> and consumed by <see cref="DemoDataService"/> for both seeding (insert
/// every record) and teardown (delete every record by id). Ordering within each list is dependency-friendly
/// for inserts; the service deletes in reverse.
/// </summary>
/// <param name="Companies">The two demo companies.</param>
/// <param name="Users">Administrator and supporting console users for each company.</param>
/// <param name="EnabledModules">(company, module-key) entitlement overrides to switch on.</param>
/// <param name="Sites">Sites for both companies (main = gated; sub = includes dispersed/retrofit no-gate).</param>
/// <param name="People">Operative people (identified by mobile, SF-1).</param>
/// <param name="Engagements">Each person's engagement with its company (the recorded name/trade).</param>
/// <param name="Cards">Qualification cards per operative (a realistic valid/expiring/expired mix).</param>
/// <param name="Attendance">Sign-in/out history across the sites.</param>
/// <param name="Mandates">One direct-debit mandate per company (commercial DB).</param>
/// <param name="Subscriptions">One metered subscription per company (commercial DB).</param>
/// <param name="Payments">Monthly payment history per company (commercial DB, for reporting).</param>
/// <param name="Payouts">Tedwren's BACS settlement payouts (commercial DB).</param>
public sealed record DemoDataPlan(
    IReadOnlyList<Company> Companies,
    IReadOnlyList<User> Users,
    IReadOnlyList<(Guid CompanyId, string ModuleKey)> EnabledModules,
    IReadOnlyList<Site> Sites,
    IReadOnlyList<Person> People,
    IReadOnlyList<Engagement> Engagements,
    IReadOnlyList<QualificationCard> Cards,
    IReadOnlyList<AttendanceRecord> Attendance,
    IReadOnlyList<Mandate> Mandates,
    IReadOnlyList<BillingSubscription> Subscriptions,
    IReadOnlyList<Payment> Payments,
    IReadOnlyList<Payout> Payouts);
