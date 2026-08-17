using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;
using PasswordHasher = Tedwren.Application.Auth.PasswordHasher;

namespace Tedwren.Application.DemoData;

/// <summary>
/// Produces the deterministic demonstration dataset — two companies (a main contractor and a subcontractor)
/// with administrators, sites, operatives, qualification history, attendance history and a full commercial
/// payment history. Every record's id is derived from a stable key (<see cref="DemoDataIds.Derive"/>), so the
/// output is reproducible and can be deleted precisely. All content lives here so the shape of the demo is in
/// one place.
/// </summary>
public static class DemoDataPlanBuilder
{
    // Qualification-type ids from the seeded default library (DefaultQualificationLibrary).
    private static readonly Guid CscsCard = new("11111111-1111-4111-8111-000000000001");
    private static readonly Guid Sssts = new("11111111-1111-4111-8111-000000000002");
    private static readonly Guid Smsts = new("11111111-1111-4111-8111-000000000003");
    private static readonly Guid FirstAid = new("11111111-1111-4111-8111-000000000004");
    private static readonly Guid WorkingAtHeight = new("11111111-1111-4111-8111-000000000006");

    private static readonly string[] ModuleKeys = { "forms", "permits", "timesheets", "inductions", "compliance-packs" };

    private static readonly string[] Trades =
    {
        "Groundworks", "Bricklayer", "Electrician", "Scaffolder", "Site Supervisor",
        "Labourer", "Carpenter", "Plant Operator", "Steel Fixer", "Plasterer",
    };

    // 25 uniquely-named operatives for the main contractor.
    private static readonly string[] MainOperativeNames =
    {
        "Liam Bennett", "Noah Clarke", "Oliver Dawson", "George Ellis", "Harry Fisher",
        "Jack Gibson", "Charlie Hughes", "Thomas Ingram", "Oscar Jennings", "William Knight",
        "James Lawson", "Henry Mercer", "Leo Nash", "Alfie Owen", "Freddie Parker",
        "Archie Quinn", "Theo Roberts", "Joshua Sutton", "Alexander Turner", "Daniel Vaughan",
        "Samuel Walsh", "Ethan Young", "Benjamin Adams", "Lucas Barnes", "Mason Cole",
    };

    // 5 uniquely-named contractors for the subcontractor.
    private static readonly string[] SubContractorNames =
    {
        "Isla Fraser", "Sophie Grant", "Amelia Hart", "Grace Lloyd", "Ava Murray",
    };

    /// <summary>Builds the full demo plan. Deterministic apart from per-run password salts (which never key ids).</summary>
    public static DemoDataPlan Build()
    {
        var now = DateTimeOffset.UtcNow;
        var passwordHash = PasswordHasher.Hash("Demo123!");

        var companies = new[]
        {
            new Company
            {
                Id = DemoDataIds.MainCompanyId,
                Name = "Demo Contractors Ltd",
                Type = "Main Contractor",
                Trade = "Principal Contractor",
                RegistrationNumber = "DC0000001",
                Address = "1 Riverside Way, London, SE1 2AA",
                ContactName = "Demo Main Admin",
                ContactEmail = "contractor@tedwren.com",
                ContactPhone = "+447700900001",
            },
            new Company
            {
                Id = DemoDataIds.SubCompanyId,
                Name = "Demo Sub Contractors Ltd",
                Type = "Subcontractor",
                Trade = "Specialist Trades",
                RegistrationNumber = "DS0000002",
                Address = "42 Kiln Road, Manchester, M1 4BT",
                ContactName = "Demo Sub Admin",
                ContactEmail = "subcontractor@tedwren.com",
                ContactPhone = "+447700900002",
            },
        };

        var users = new List<User>
        {
            MakeUser(DemoDataIds.MainCompanyId, "main-admin", "Demo Main Administrator", "contractor@tedwren.com", AccessRole.Administrator, passwordHash, now),
            MakeUser(DemoDataIds.MainCompanyId, "main-compliance", "Demo Main Compliance", "compliance.main@tedwren.com", AccessRole.ComplianceManager, passwordHash, now),
            MakeUser(DemoDataIds.MainCompanyId, "main-sitemgr", "Demo Main Site Manager", "site.main@tedwren.com", AccessRole.SiteManager, passwordHash, now),
            MakeUser(DemoDataIds.SubCompanyId, "sub-admin", "Demo Sub Administrator", "subcontractor@tedwren.com", AccessRole.Administrator, passwordHash, now),
            MakeUser(DemoDataIds.SubCompanyId, "sub-compliance", "Demo Sub Compliance", "compliance.sub@tedwren.com", AccessRole.ComplianceManager, passwordHash, now),
        };

        var enabledModules = new List<(Guid, string)>();
        foreach (var companyId in DemoDataIds.CompanyIds)
        {
            foreach (var key in ModuleKeys)
            {
                enabledModules.Add((companyId, key));
            }
        }

        var sites = BuildSites(now);
        var mainSites = sites.Where(s => s.CompanyId == DemoDataIds.MainCompanyId).ToList();
        var subSites = sites.Where(s => s.CompanyId == DemoDataIds.SubCompanyId).ToList();

        var people = new List<Person>();
        var engagements = new List<Engagement>();
        var cards = new List<QualificationCard>();
        var attendance = new List<AttendanceRecord>();

        BuildWorkforce(DemoDataIds.MainCompanyId, "main", MainOperativeNames, mainSites, people, engagements, cards, attendance, now);
        BuildWorkforce(DemoDataIds.SubCompanyId, "sub", SubContractorNames, subSites, people, engagements, cards, attendance, now);

        var (mandates, subscriptions, payments, payouts) = BuildCommercial(now);

        return new DemoDataPlan(companies, users, enabledModules, sites, people, engagements, cards, attendance, mandates, subscriptions, payments, payouts);
    }

    /// <summary>Creates one console user with the demo password.</summary>
    private static User MakeUser(Guid companyId, string key, string name, string email, AccessRole role, string passwordHash, DateTimeOffset now) => new()
    {
        Id = DemoDataIds.Derive($"user:{key}"),
        CompanyId = companyId,
        Name = name,
        Email = email,
        Role = role,
        Status = UserStatus.Active,
        PasswordHash = passwordHash,
        PasswordSetUtc = now,
    };

    /// <summary>Builds the sites: ten gated main-contractor sites and a distinct set of subcontractor sites (some dispersed/retrofit, no gate).</summary>
    private static List<Site> BuildSites(DateTimeOffset now)
    {
        var sites = new List<Site>();

        var mainSiteNames = new[]
        {
            ("Battersea Riverside Phase 2", "Thames Living", "London", 51.4790, -0.1450),
            ("Canary Wharf Tower 5", "Docklands Estates", "London", 51.5054, -0.0235),
            ("Stratford Gateway", "Olympic Regeneration", "London", 51.5416, -0.0042),
            ("Croydon Interchange", "Southern Rail Partners", "London", 51.3720, -0.1010),
            ("Reading Green Park", "Thames Valley Devs", "South East", 51.4210, -0.9760),
            ("Milton Keynes Central", "MK Growth", "South East", 52.0340, -0.7740),
            ("Bristol Harbourside", "Avon Regeneration", "South West", 51.4490, -2.5980),
            ("Birmingham Curzon", "Midlands Connect", "Midlands", 52.4790, -1.8900),
            ("Leeds South Bank", "Yorkshire Developments", "North", 53.7900, -1.5450),
            ("Manchester Piccadilly East", "Northern Gateway", "North", 53.4770, -2.2300),
        };

        for (var i = 0; i < mainSiteNames.Length; i++)
        {
            var (name, client, region, lat, lng) = mainSiteNames[i];
            sites.Add(new Site
            {
                Id = DemoDataIds.Derive($"site:main:{i}"),
                CompanyId = DemoDataIds.MainCompanyId,
                Name = name,
                Client = client,
                Region = region,
                Address = $"{name}, {region}",
                HasCompound = true,
                IsDispersed = false,
                Boundary = new Geofence(lat, lng, 150),
                LocationPolicy = LocationPolicy.RecordAndFlag,
            });
        }

        // Subcontractor's own sites — deliberately including dispersed retrofit sites with no gate/compound.
        var subSiteNames = new[]
        {
            // (name, client, region, isDispersed, hasCompound)
            ("Northgate Fit-Out Unit 3", "Retail Estates", "North", false, true),
            ("Victoria Line Retrofit Programme", "Transport for London", "London", true, false),
            ("Social Housing Retrofit — Salford", "Salford Homes", "North", true, false),
            ("Heritage Church Restoration", "National Trust", "South West", true, false),
            ("Distributed Solar Retrofit — Kent", "GreenRoof Energy", "South East", true, false),
            ("Hospital Ward Refurbishment", "NHS Estates", "Midlands", false, true),
        };

        for (var i = 0; i < subSiteNames.Length; i++)
        {
            var (name, client, region, isDispersed, hasCompound) = subSiteNames[i];
            sites.Add(new Site
            {
                Id = DemoDataIds.Derive($"site:sub:{i}"),
                CompanyId = DemoDataIds.SubCompanyId,
                Name = name,
                Client = client,
                Region = region,
                Address = $"{name}, {region}",
                HasCompound = hasCompound,
                IsDispersed = isDispersed,
                // Dispersed retrofit sites have no fixed gate, so no geofence — attendance is recorded and flagged.
                Boundary = isDispersed ? null : new Geofence(53.4 + i * 0.01, -2.3 + i * 0.01, 120),
                LocationPolicy = LocationPolicy.RecordAndFlag,
            });
        }

        return sites;
    }

    /// <summary>Builds people, engagements, qualification cards and attendance history for a company's workforce.</summary>
    private static void BuildWorkforce(
        Guid companyId,
        string companyKey,
        string[] names,
        IReadOnlyList<Site> companySites,
        List<Person> people,
        List<Engagement> engagements,
        List<QualificationCard> cards,
        List<AttendanceRecord> attendance,
        DateTimeOffset now)
    {
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var personKey = $"{companyKey}:{i}";
            var personId = DemoDataIds.Derive($"person:{personKey}");
            var phone = PhoneNumber.Parse($"+447700{900100 + (companyKey == "main" ? 0 : 500) + i}");
            var trade = Trades[i % Trades.Length];

            people.Add(new Person { Id = personId, PhoneNumber = phone, CreatedUtc = now.AddMonths(-6) });

            engagements.Add(new Engagement
            {
                Id = DemoDataIds.Derive($"engagement:{personKey}"),
                CompanyId = companyId,
                PersonId = personId,
                Name = name,
                Trade = trade,
                InternalReference = $"{companyKey.ToUpperInvariant()}-{1000 + i}",
                Status = EngagementStatus.Active,
                CreatedUtc = now.AddMonths(-6),
            });

            cards.AddRange(BuildCards(personKey, personId, name, trade, i, now));

            var site = companySites[i % companySites.Count];
            attendance.AddRange(BuildAttendance(personKey, personId, site, now));
        }
    }

    /// <summary>Builds a realistic set of qualification cards for one operative (valid / expiring-soon / expired mix).</summary>
    private static IEnumerable<QualificationCard> BuildCards(string personKey, Guid personId, string name, string trade, int index, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // CSCS for everyone; expiry varies by index so the compliance roll-up has valid, expiring and expired cards.
        DateOnly cscsExpiry = (index % 5) switch
        {
            0 => today.AddDays(-30),   // expired
            1 => today.AddDays(20),    // expiring soon
            _ => today.AddYears(2),    // valid
        };

        yield return new QualificationCard
        {
            Id = DemoDataIds.Derive($"card:{personKey}:cscs"),
            PersonId = personId,
            QualificationTypeId = CscsCard,
            CardNumber = $"CSCS-{10000 + index:00000}",
            HolderName = name,
            IssuedOn = cscsExpiry.AddYears(-5),
            ExpiresOn = cscsExpiry,
            CaptureSource = CardCaptureSource.Photo,
            VerificationState = CardVerificationState.CscsVerified,
            NeedsReview = cscsExpiry < today,
            ConfirmedBy = "Demo Main Compliance",
            ConfirmedUtc = now.AddMonths(-3),
        };

        // Supervisors carry a supervision/management card.
        if (trade == "Site Supervisor")
        {
            yield return new QualificationCard
            {
                Id = DemoDataIds.Derive($"card:{personKey}:sssts"),
                PersonId = personId,
                QualificationTypeId = index % 2 == 0 ? Sssts : Smsts,
                CardNumber = $"SUP-{20000 + index:00000}",
                HolderName = name,
                IssuedOn = today.AddYears(-2),
                ExpiresOn = today.AddYears(3),
                CaptureSource = CardCaptureSource.Photo,
                VerificationState = CardVerificationState.CustomerChecked,
                ConfirmedBy = "Demo Main Compliance",
                ConfirmedUtc = now.AddMonths(-2),
            };
        }

        // Scaffolders carry a working-at-height card; every third operative also has first aid.
        if (trade == "Scaffolder")
        {
            yield return new QualificationCard
            {
                Id = DemoDataIds.Derive($"card:{personKey}:height"),
                PersonId = personId,
                QualificationTypeId = WorkingAtHeight,
                CardNumber = $"WAH-{30000 + index:00000}",
                HolderName = name,
                IssuedOn = today.AddYears(-1),
                ExpiresOn = today.AddYears(4),
                CaptureSource = CardCaptureSource.Photo,
                VerificationState = CardVerificationState.CustomerChecked,
            };
        }

        if (index % 3 == 0)
        {
            yield return new QualificationCard
            {
                Id = DemoDataIds.Derive($"card:{personKey}:firstaid"),
                PersonId = personId,
                QualificationTypeId = FirstAid,
                CardNumber = $"FAW-{40000 + index:00000}",
                HolderName = name,
                IssuedOn = today.AddYears(-1),
                ExpiresOn = today.AddYears(2),
                CaptureSource = CardCaptureSource.Photo,
                VerificationState = CardVerificationState.CustomerChecked,
            };
        }
    }

    /// <summary>Builds ten working days of sign-in/out attendance for one operative at their assigned site.</summary>
    private static IEnumerable<AttendanceRecord> BuildAttendance(string personKey, Guid personId, Site site, DateTimeOffset now)
    {
        var gated = site.HasCompound && !site.IsDispersed;

        for (var day = 1; day <= 10; day++)
        {
            var date = now.Date.AddDays(-day);
            // Skip weekends.
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            var signIn = new DateTimeOffset(date.AddHours(7).AddMinutes(30), TimeSpan.Zero);
            var signOut = new DateTimeOffset(date.AddHours(16).AddMinutes(30), TimeSpan.Zero);

            yield return new AttendanceRecord
            {
                Id = DemoDataIds.Derive($"attendance:{personKey}:{day}:in"),
                PersonId = personId,
                SiteId = site.Id,
                Type = AttendanceEventType.SignIn,
                // Dispersed retrofit sites have no gate, so location can't be confirmed → flagged (MC retrofit path).
                Outcome = gated ? AttendanceOutcome.Accepted : AttendanceOutcome.Flagged,
                Method = gated ? SignInMethod.QrScan : SignInMethod.AssignmentLink,
                Latitude = site.Boundary?.CentreLatitude,
                Longitude = site.Boundary?.CentreLongitude,
                WithinBoundary = gated ? true : null,
                Reason = gated ? null : "No site boundary (dispersed/retrofit)",
                OccurredUtc = signIn,
                CreatedUtc = signIn,
            };

            yield return new AttendanceRecord
            {
                Id = DemoDataIds.Derive($"attendance:{personKey}:{day}:out"),
                PersonId = personId,
                SiteId = site.Id,
                Type = AttendanceEventType.SignOut,
                Outcome = AttendanceOutcome.Accepted,
                Method = gated ? SignInMethod.QrScan : SignInMethod.AssignmentLink,
                Latitude = site.Boundary?.CentreLatitude,
                Longitude = site.Boundary?.CentreLongitude,
                WithinBoundary = gated ? true : null,
                OccurredUtc = signOut,
                CreatedUtc = signOut,
            };
        }
    }

    /// <summary>Builds the commercial-DB records: a mandate and subscription per company, twelve months of payments, and settlement payouts.</summary>
    private static (List<Mandate>, List<BillingSubscription>, List<Payment>, List<Payout>) BuildCommercial(DateTimeOffset now)
    {
        var mandates = new List<Mandate>();
        var subscriptions = new List<BillingSubscription>();
        var payments = new List<Payment>();

        var billing = new[]
        {
            (DemoDataIds.MainCompanyId, "main", "sites", "large", 24_900, "Main Contractor plan — up to 10 sites"),
            (DemoDataIds.SubCompanyId, "sub", "operatives", "small", 7_900, "Subcontractor plan — up to 25 operatives"),
        };

        foreach (var (companyId, key, meter, band, monthlyPence, description) in billing)
        {
            var mandateId = DemoDataIds.Derive($"mandate:{key}");
            mandates.Add(new Mandate
            {
                Id = mandateId,
                CompanyId = companyId,
                GoCardlessMandateId = $"DEMO-MD-{key}",
                Reference = $"TEDWREN-{key.ToUpperInvariant()}",
                BankName = "Demo Bank plc",
                AccountNumberEnding = key == "main" ? "4321" : "8765",
                Status = MandateStatus.Active,
                CreatedUtc = now.AddMonths(-13),
                UpdatedUtc = now.AddMonths(-13),
            });

            subscriptions.Add(new BillingSubscription
            {
                Id = DemoDataIds.Derive($"subscription:{key}"),
                CompanyId = companyId,
                MandateId = mandateId,
                MeterKey = meter,
                BandKey = band,
                Description = description,
                Status = SubscriptionStatus.Active,
                CreatedUtc = now.AddMonths(-13),
                UpdatedUtc = now.AddMonths(-1),
            });

            // Twelve months of monthly collections, most paid out, one failed-then-retaken, the latest still pending.
            for (var month = 12; month >= 1; month--)
            {
                var charge = now.AddMonths(-month);
                var chargeDate = DateOnly.FromDateTime(charge.UtcDateTime);
                var (status, failure) = month switch
                {
                    1 => (PaymentStatus.Submitted, (string?)null),   // latest, in flight
                    4 => (PaymentStatus.Failed, "insufficient_funds"),
                    _ => (PaymentStatus.PaidOut, (string?)null),
                };

                payments.Add(new Payment
                {
                    Id = DemoDataIds.Derive($"payment:{key}:{month}"),
                    CompanyId = companyId,
                    MandateId = mandateId,
                    GoCardlessPaymentId = $"DEMO-PM-{key}-{month:00}",
                    AmountPence = monthlyPence,
                    Currency = "GBP",
                    Description = $"{description} — {chargeDate:MMMM yyyy}",
                    Status = status,
                    ChargeDate = chargeDate,
                    FailureReason = failure,
                    CreatedUtc = charge,
                    UpdatedUtc = charge.AddDays(3),
                });

                // The failed month was re-taken successfully a few days later.
                if (month == 4)
                {
                    payments.Add(new Payment
                    {
                        Id = DemoDataIds.Derive($"payment:{key}:{month}:retry"),
                        CompanyId = companyId,
                        MandateId = mandateId,
                        GoCardlessPaymentId = $"DEMO-PM-{key}-{month:00}-R",
                        AmountPence = monthlyPence,
                        Currency = "GBP",
                        Description = $"{description} — {chargeDate:MMMM yyyy} (re-taken)",
                        Status = PaymentStatus.PaidOut,
                        ChargeDate = chargeDate.AddDays(7),
                        CreatedUtc = charge.AddDays(7),
                        UpdatedUtc = charge.AddDays(10),
                    });
                }
            }
        }

        // Tedwren's own BACS settlements (not tenant-scoped): a handful of recent payouts.
        var payouts = new List<Payout>();
        for (var i = 1; i <= 6; i++)
        {
            var arrival = now.AddMonths(-i);
            payouts.Add(new Payout
            {
                Id = DemoDataIds.Derive($"payout:{i}"),
                GoCardlessPayoutId = $"DEMO-PO-{i:00}",
                AmountPence = 30_000 + i * 1_500,
                Currency = "GBP",
                Status = i == 1 ? PayoutStatus.Pending : PayoutStatus.Paid,
                Reference = $"DEMO-PAYOUT-{i:00}",
                ArrivalDate = DateOnly.FromDateTime(arrival.UtcDateTime),
                CreatedUtc = arrival,
                UpdatedUtc = arrival,
            });
        }

        return (mandates, subscriptions, payments, payouts);
    }
}
