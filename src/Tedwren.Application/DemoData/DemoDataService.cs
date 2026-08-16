using Tedwren.Abstractions.Contracts.DemoData;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.DemoData;

/// <summary>
/// Builds, inspects and tears down the Tedwren demonstration dataset (see <see cref="IDemoDataService"/>). The
/// whole dataset is produced deterministically by <see cref="DemoDataPlanBuilder"/> so seeding, deleting and
/// recreating all operate over the same fixed set of records. Inserts reuse the ordinary repositories (one code
/// path for SQL Server, PostgreSQL and the in-memory test doubles); deletes remove exactly the planned records
/// by id, in reverse dependency order. Restricted to platform admins at the API. Never touches anything outside
/// the two demo companies.
/// </summary>
public sealed class DemoDataService : IDemoDataService
{
    private readonly ICompanyRepository _companies;
    private readonly IUserRepository _users;
    private readonly IEntitlementRepository _entitlements;
    private readonly ISiteRepository _sites;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly IQualificationCardRepository _cards;
    private readonly IAttendanceRepository _attendance;
    private readonly IMandateRepository _mandates;
    private readonly IPaymentRepository _payments;
    private readonly IBillingSubscriptionRepository _subscriptions;
    private readonly IPayoutRepository _payouts;
    private readonly DemoDataProgressState _progress;

    /// <summary>Creates the service over the repositories it seeds and the shared progress tracker.</summary>
    public DemoDataService(
        ICompanyRepository companies,
        IUserRepository users,
        IEntitlementRepository entitlements,
        ISiteRepository sites,
        IPersonRepository people,
        IEngagementRepository engagements,
        IQualificationCardRepository cards,
        IAttendanceRepository attendance,
        IMandateRepository mandates,
        IPaymentRepository payments,
        IBillingSubscriptionRepository subscriptions,
        IPayoutRepository payouts,
        DemoDataProgressState progress)
    {
        _companies = companies;
        _users = users;
        _entitlements = entitlements;
        _sites = sites;
        _people = people;
        _engagements = engagements;
        _cards = cards;
        _attendance = attendance;
        _mandates = mandates;
        _payments = payments;
        _subscriptions = subscriptions;
        _payouts = payouts;
        _progress = progress;
    }

    /// <inheritdoc />
    public DemoDataProgress GetProgress() => _progress.Snapshot();

    /// <inheritdoc />
    public async Task<DemoDataStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var plan = DemoDataPlanBuilder.Build();

        var present = await _companies.GetByIdAsync(DemoDataIds.MainCompanyId, cancellationToken) is not null;
        if (!present)
        {
            return new DemoDataStatus(false, 0, 0, 0, 0, 0, 0, 0);
        }

        var users = (await _users.GetAllAsync(cancellationToken)).Count(u => DemoDataIds.CompanyIds.Contains(u.CompanyId));
        var sites = (await _sites.GetAllAsync(cancellationToken)).Count(s => DemoDataIds.CompanyIds.Contains(s.CompanyId));

        var operatives = 0;
        foreach (var companyId in DemoDataIds.CompanyIds)
        {
            operatives += await _engagements.CountActiveByCompanyAsync(companyId, cancellationToken);
        }

        var personIds = plan.People.Select(p => p.Id).ToList();
        var cards = (await _cards.GetByPersonsAsync(personIds, cancellationToken)).Count;

        var attendance = 0;
        foreach (var site in plan.Sites)
        {
            attendance += (await _attendance.GetBySiteAsync(site.Id, int.MaxValue, cancellationToken)).Count;
        }

        var payments = 0;
        foreach (var companyId in DemoDataIds.CompanyIds)
        {
            payments += (await _payments.GetByCompanyAsync(companyId, cancellationToken)).Count;
        }

        return new DemoDataStatus(true, DemoDataIds.CompanyIds.Count, users, sites, operatives, cards, attendance, payments);
    }

    /// <inheritdoc />
    public async Task<DemoDataStatus> SeedAsync(CancellationToken cancellationToken = default)
    {
        var plan = DemoDataPlanBuilder.Build();
        _progress.Begin("Seeding", totalStages: 9);
        try
        {
            // Recreate semantics: clear any existing demo dataset first so ids never clash.
            _progress.Advance("Clearing any existing demo data");
            await PurgeAsync(plan, cancellationToken);

            _progress.Advance("Companies");
            foreach (var company in plan.Companies)
            {
                await _companies.AddAsync(company, cancellationToken);
            }

            _progress.Advance("Users & module access");
            foreach (var user in plan.Users)
            {
                await _users.AddAsync(user, cancellationToken);
            }
            foreach (var (companyId, moduleKey) in plan.EnabledModules)
            {
                await _entitlements.SetAsync(companyId, moduleKey, true, cancellationToken);
            }

            _progress.Advance("Sites");
            foreach (var site in plan.Sites)
            {
                await _sites.AddAsync(site, cancellationToken);
            }

            _progress.Advance("Operatives");
            foreach (var person in plan.People)
            {
                await _people.AddAsync(person, cancellationToken);
            }
            foreach (var engagement in plan.Engagements)
            {
                await _engagements.AddAsync(engagement, cancellationToken);
            }

            _progress.Advance("Qualification cards");
            foreach (var card in plan.Cards)
            {
                await _cards.AddAsync(card, cancellationToken);
            }

            _progress.Advance("Attendance history");
            foreach (var record in plan.Attendance)
            {
                await _attendance.AddAsync(record, cancellationToken);
            }

            _progress.Advance("Commercial: mandates & subscriptions");
            foreach (var mandate in plan.Mandates)
            {
                await _mandates.AddAsync(mandate, cancellationToken);
            }
            foreach (var subscription in plan.Subscriptions)
            {
                await _subscriptions.AddAsync(subscription, cancellationToken);
            }

            _progress.Advance("Commercial: payment & payout history");
            foreach (var payment in plan.Payments)
            {
                await _payments.AddAsync(payment, cancellationToken);
            }
            foreach (var payout in plan.Payouts)
            {
                await _payouts.AddAsync(payout, cancellationToken);
            }

            _progress.Complete();
        }
        catch (Exception ex)
        {
            _progress.Fail(ex.Message);
            throw;
        }

        return await GetStatusAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var plan = DemoDataPlanBuilder.Build();
        _progress.Begin("Deleting", totalStages: 1);
        try
        {
            await PurgeAsync(plan, cancellationToken);
            _progress.Complete();
        }
        catch (Exception ex)
        {
            _progress.Fail(ex.Message);
            throw;
        }
    }

    /// <summary>Removes every planned record, in reverse dependency order. Safe when nothing exists.</summary>
    private async Task PurgeAsync(DemoDataPlan plan, CancellationToken cancellationToken)
    {
        foreach (var payout in plan.Payouts)
        {
            await _payouts.DeleteAsync(payout.Id, cancellationToken);
        }
        foreach (var payment in plan.Payments)
        {
            await _payments.DeleteAsync(payment.Id, cancellationToken);
        }
        foreach (var subscription in plan.Subscriptions)
        {
            await _subscriptions.DeleteAsync(subscription.Id, cancellationToken);
        }
        foreach (var mandate in plan.Mandates)
        {
            await _mandates.DeleteAsync(mandate.Id, cancellationToken);
        }
        foreach (var record in plan.Attendance)
        {
            await _attendance.DeleteAsync(record.Id, cancellationToken);
        }
        foreach (var card in plan.Cards)
        {
            await _cards.DeleteAsync(card.Id, cancellationToken);
        }
        foreach (var engagement in plan.Engagements)
        {
            await _engagements.DeleteAsync(engagement.Id, cancellationToken);
        }
        foreach (var person in plan.People)
        {
            await _people.DeleteAsync(person.Id, cancellationToken);
        }
        foreach (var site in plan.Sites)
        {
            await _sites.DeleteAsync(site.Id, cancellationToken);
        }
        foreach (var user in plan.Users)
        {
            await _users.DeleteAsync(user.Id, cancellationToken);
        }
        foreach (var companyId in DemoDataIds.CompanyIds)
        {
            await _entitlements.ClearForCompanyAsync(companyId, cancellationToken);
        }
        foreach (var company in plan.Companies)
        {
            await _companies.DeleteAsync(company.Id, cancellationToken);
        }
    }
}
