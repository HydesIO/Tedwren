using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.DemoData;

/// <summary>
/// Ensures the published demonstration sign-in accounts exist and are signable-in on a demo/staging deployment,
/// so the console and the browser emulator can always be logged into with the documented credentials without a
/// platform admin first running "Generate demo data". Idempotent and self-healing: each account is created when
/// missing, and an existing one has its password and active status repaired so the published credential always
/// works (these are fixed, shared demo credentials by design, not user-chosen secrets).
///
/// It seeds:
/// <list type="bullet">
///   <item>the two demo tenants (<see cref="DemoDataIds.MainCompanyId"/> / <see cref="DemoDataIds.SubCompanyId"/>)
///     so the demo-tenant administrators land in a real company;</item>
///   <item>the two demo-tenant administrators — <c>contractor@tedwren.com</c> and <c>subcontractor@tedwren.com</c>
///     (<see cref="DemoCredentials.TenantPassword"/>);</item>
///   <item>the named Tedwren platform administrators (<see cref="AdminUserSeeder.MasterAdmins"/>) at the demo admin
///     password (<see cref="DemoCredentials.AdminPassword"/>), healing the placeholder password that
///     <see cref="AdminUserSeeder"/> creates them with;</item>
///   <item>the demo operative's <see cref="Person"/> + <see cref="Engagement"/> so the emulator's Development-only
///     operative demo sign-in (<c>operative@tedwren.com</c>) resolves.</item>
/// </list>
///
/// Gated on <see cref="DemoOptions.Enabled"/>: it never runs unless demo mode is on, which
/// <c>StartupSecurity</c> refuses to allow in Production — so these fixed credentials can never reach a real
/// deployment. It reuses the canonical records from <see cref="DemoDataPlanBuilder"/>, and its records share the
/// same deterministic ids as the on-demand demo seed, so the two coexist (the demo-data seed skips whatever is
/// already present).
/// </summary>
public sealed class DemoLoginSeeder
{
    private const string ContractorEmail = "contractor@tedwren.com";
    private const string SubcontractorEmail = "subcontractor@tedwren.com";

    private readonly ICompanyRepository _companies;
    private readonly IUserRepository _users;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly DemoOptions _demo;

    /// <summary>Creates the seeder over the company, user, person and engagement repositories and the demo toggle.</summary>
    public DemoLoginSeeder(
        ICompanyRepository companies,
        IUserRepository users,
        IPersonRepository people,
        IEngagementRepository engagements,
        DemoOptions demo)
    {
        _companies = companies;
        _users = users;
        _people = people;
        _engagements = engagements;
        _demo = demo;
    }

    /// <summary>Ensures every published demo login account exists and is signable-in. No-op unless demo mode is enabled.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Fail-closed: never seed fixed demo credentials outside demo mode (Production refuses Demo:Enabled).
        if (!_demo.Enabled)
        {
            return;
        }

        var plan = DemoDataPlanBuilder.Build();

        // The two demo tenants, so contractor@ / subcontractor@ have a real company to sign into.
        foreach (var company in plan.Companies)
        {
            await EnsureCompanyAsync(company, cancellationToken);
        }

        // The two demo-tenant administrators, at the published demo password.
        foreach (var email in new[] { ContractorEmail, SubcontractorEmail })
        {
            var template = plan.Users.Single(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            await EnsureLoginAsync(template, DemoCredentials.TenantPassword, cancellationToken);
        }

        // The named Tedwren platform administrators, at the demo admin password (healing AdminUserSeeder's placeholder).
        foreach (var admin in AdminUserSeeder.MasterAdmins)
        {
            await EnsureAdminLoginAsync(admin, cancellationToken);
        }

        // The demo operative's identity, so the emulator's operative@ demo sign-in resolves to an active engagement.
        foreach (var person in plan.People.Where(p => p.Id == DemoOperatives.PersonId))
        {
            await EnsurePersonAsync(person, cancellationToken);
        }
        foreach (var engagement in plan.Engagements.Where(e => e.Id == DemoOperatives.EngagementId))
        {
            await EnsureEngagementAsync(engagement, cancellationToken);
        }
    }

    /// <summary>Creates the demo company when it does not already exist (its editable fields are left untouched otherwise).</summary>
    private async Task EnsureCompanyAsync(Company company, CancellationToken cancellationToken)
    {
        if (await _companies.GetByIdAsync(company.Id, cancellationToken) is null)
        {
            await _companies.AddAsync(company, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a demo-tenant login from the canonical plan record when missing, else repairs its password and
    /// active status so the published credential always signs in. Uses the given plaintext so an existing account
    /// whose password has drifted is reset to the documented value.
    /// </summary>
    private async Task EnsureLoginAsync(User template, string password, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(template.Email, cancellationToken);
        if (existing is null)
        {
            template.PasswordHash = PasswordHasher.Hash(password);
            template.PasswordSetUtc = DateTimeOffset.UtcNow;
            template.Status = UserStatus.Active;
            await _users.AddAsync(template, cancellationToken);
            return;
        }

        await HealCredentialAsync(existing, password, cancellationToken);
    }

    /// <summary>
    /// Creates a named platform administrator in the Tedwren tenant when missing (<see cref="AdminUserSeeder"/>
    /// normally does this first), else repairs its password and active status. Leaves the role as-is so a
    /// deliberate change is never clobbered; a freshly-created one is a full <see cref="AccessRole.Administrator"/>.
    /// </summary>
    private async Task EnsureAdminLoginAsync(SeedAdmin admin, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(admin.Email, cancellationToken);
        if (existing is null)
        {
            await _users.AddAsync(
                new User
                {
                    CompanyId = AdminUserSeeder.SeedCompanyId,
                    Name = admin.Name,
                    Email = admin.Email,
                    Role = AccessRole.Administrator,
                    Status = UserStatus.Active,
                    PasswordHash = PasswordHasher.Hash(DemoCredentials.AdminPassword),
                    PasswordSetUtc = DateTimeOffset.UtcNow,
                },
                cancellationToken);
            return;
        }

        await HealCredentialAsync(existing, DemoCredentials.AdminPassword, cancellationToken);
    }

    /// <summary>Resets an existing account's password to the published value and re-activates it, but only when either has drifted.</summary>
    private async Task HealCredentialAsync(User existing, string password, CancellationToken cancellationToken)
    {
        var changed = false;

        if (!PasswordHasher.Verify(password, existing.PasswordHash))
        {
            existing.PasswordHash = PasswordHasher.Hash(password);
            existing.PasswordSetUtc = DateTimeOffset.UtcNow;
            changed = true;
        }

        if (existing.Status != UserStatus.Active)
        {
            existing.Status = UserStatus.Active;
            changed = true;
        }

        if (changed)
        {
            await _users.UpdateAsync(existing, cancellationToken);
        }
    }

    /// <summary>Creates the demo operative's person record when it does not already exist.</summary>
    private async Task EnsurePersonAsync(Person person, CancellationToken cancellationToken)
    {
        if (await _people.GetByIdAsync(person.Id, cancellationToken) is null)
        {
            await _people.AddAsync(person, cancellationToken);
        }
    }

    /// <summary>Creates the demo operative's engagement when the company does not already hold it.</summary>
    private async Task EnsureEngagementAsync(Engagement engagement, CancellationToken cancellationToken)
    {
        if (await _engagements.GetAsync(engagement.CompanyId, engagement.Id, cancellationToken) is null)
        {
            await _engagements.AddAsync(engagement, cancellationToken);
        }
    }
}
