using Microsoft.EntityFrameworkCore;

namespace Tedwren.DataAccess.Commercial.Ef;

/// <summary>
/// EF Core context describing the <b>Commercial</b> database schema so migrations can create and evolve it
/// (DDL). It is NOT used for runtime data access — that stays on the Dapper commercial repositories (DML).
/// The mappings mirror the hand-written scripts under Migrations/Scripts/**/Commercial/ exactly (table/column
/// names, keys, unique indexes, computed columns) so a schema produced by EF is byte-compatible with what the
/// Dapper repositories expect, and the idempotent startup scripts remain a no-op over EF-created tables. On
/// PostgreSQL all identifiers are folded to lower case so the repositories' unquoted SQL resolves to them.
/// See docs/ef-migrations.md §8. SQL Server is the supported commercial EF path today; PostgreSQL is deferred.
/// </summary>
public sealed class CommercialDbContext : DbContext
{
    /// <summary>Creates the context with the provider options supplied by the design-time factory.</summary>
    public CommercialDbContext(DbContextOptions<CommercialDbContext> options) : base(options)
    {
    }

    public DbSet<MandateRecord> Mandates => Set<MandateRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<BillingSubscriptionRecord> BillingSubscriptions => Set<BillingSubscriptionRecord>();
    public DbSet<WebhookEventRecord> WebhookEvents => Set<WebhookEventRecord>();
    public DbSet<PayoutRecord> Payouts => Set<PayoutRecord>();
    public DbSet<LaunchSignupRecord> LaunchSignups => Set<LaunchSignupRecord>();
    public DbSet<LeadRecord> Leads => Set<LeadRecord>();
    public DbSet<LeadNoteRecord> LeadNotes => Set<LeadNoteRecord>();
    public DbSet<AffiliateRecord> Affiliates => Set<AffiliateRecord>();
    public DbSet<AffiliatePayoutRecord> AffiliatePayouts => Set<AffiliatePayoutRecord>();
    public DbSet<AffiliateAgreementRecord> AffiliateAgreements => Set<AffiliateAgreementRecord>();

    /// <summary>Maps every schema record to its table, keys and indexes, mirroring the hand-written scripts.</summary>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<MandateRecord>(e =>
        {
            e.ToTable("Mandates");                              // 022_billing.sql
            e.Property(x => x.GoCardlessBillingRequestId).HasMaxLength(64);
            e.Property(x => x.GoCardlessMandateId).HasMaxLength(64);
            e.Property(x => x.GoCardlessCustomerId).HasMaxLength(64);
            e.Property(x => x.Reference).HasMaxLength(140);
            e.Property(x => x.BankName).HasMaxLength(140);
            e.Property(x => x.AccountNumberEnding).HasMaxLength(8);
            e.HasIndex(x => new { x.CompanyId, x.CreatedUtc }).HasDatabaseName("IX_Mandates_Company");  // R15
            e.HasIndex(x => x.GoCardlessMandateId).HasDatabaseName("IX_Mandates_GcMandate");
        });

        model.Entity<PaymentRecord>(e =>
        {
            e.ToTable("Payments");                              // 022_billing.sql
            e.Property(x => x.GoCardlessPaymentId).HasMaxLength(64);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.CreatedUtc }).HasDatabaseName("IX_Payments_Company");  // R15
            e.HasIndex(x => x.GoCardlessPaymentId).HasDatabaseName("IX_Payments_GcPayment");
        });

        model.Entity<BillingSubscriptionRecord>(e =>
        {
            e.ToTable("BillingSubscriptions");                  // 022_billing.sql
            e.Property(x => x.MeterKey).HasMaxLength(64);
            e.Property(x => x.BandKey).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.CompanyId).IsUnique().HasDatabaseName("UX_BillingSubscriptions_Company");  // one per company
        });

        model.Entity<WebhookEventRecord>(e =>
        {
            e.ToTable("WebhookEvents");                         // 023_webhook_events.sql
            e.Property(x => x.GoCardlessEventId).HasMaxLength(64);
            e.Property(x => x.ResourceType).HasMaxLength(64);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.ResourceId).HasMaxLength(64);
            e.Property(x => x.Detail).HasMaxLength(500);
            e.HasIndex(x => x.GoCardlessEventId).IsUnique().HasDatabaseName("UX_WebhookEvents_EventId");  // dedupe
            e.HasIndex(x => x.ReceivedUtc).HasDatabaseName("IX_WebhookEvents_Received");
        });

        model.Entity<PayoutRecord>(e =>
        {
            e.ToTable("Payouts");                               // 024_payouts.sql
            e.Property(x => x.GoCardlessPayoutId).HasMaxLength(64);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Reference).HasMaxLength(140);
            e.HasIndex(x => x.GoCardlessPayoutId).IsUnique().HasDatabaseName("UX_Payouts_GcPayout");  // dedupe
        });

        model.Entity<LaunchSignupRecord>(e =>
        {
            e.ToTable("LaunchSignups");                         // 025_launch_signups.sql + 031_launch_unsubscribe.sql
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.UtmSource).HasMaxLength(128);
            e.Property(x => x.UtmMedium).HasMaxLength(128);
            e.Property(x => x.UtmCampaign).HasMaxLength(128);
            e.Property(x => x.Notified).HasDefaultValue(false);
            e.Property(x => x.Unsubscribed).HasDefaultValue(false);
            e.Property(x => x.UnsubscribeToken).HasMaxLength(64);
            // Persisted computed lower-cased email — the case-insensitive dedupe key. Length pinned to match
            // Email (nvarchar(256)), which the script's computed column inherits from LOWER(Email).
            e.Property(x => x.EmailLower).HasMaxLength(256).HasComputedColumnSql("LOWER(Email)", stored: true);
            e.HasIndex(x => x.EmailLower).IsUnique().HasDatabaseName("UX_LaunchSignups_Email");
            e.HasIndex(x => x.UnsubscribeToken).IsUnique().HasDatabaseName("UX_LaunchSignups_Unsub")
                .HasFilter("[UnsubscribeToken] IS NOT NULL");
        });

        model.Entity<LeadRecord>(e =>
        {
            e.ToTable("Leads");                                 // 026_leads.sql
            e.Property(x => x.CompanyName).HasMaxLength(256);
            e.Property(x => x.ContactName).HasMaxLength(160);
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.Property(x => x.Location).HasMaxLength(160);
            e.Property(x => x.Model).HasDefaultValue(0);
            e.Property(x => x.EstimatedRevenue).HasPrecision(18, 2);
            e.Property(x => x.Status).HasDefaultValue(0);
            e.Property(x => x.AccountManager).HasMaxLength(160);
            e.Property(x => x.CompanyNumber).HasMaxLength(32);
            e.Property(x => x.Source).HasMaxLength(64);
            e.HasIndex(x => x.UpdatedUtc).IsDescending().HasDatabaseName("IX_Leads_UpdatedUtc");
        });

        model.Entity<LeadNoteRecord>(e =>
        {
            e.ToTable("LeadNotes");                             // 027_lead_notes.sql
            e.Property(x => x.Author).HasMaxLength(160);
            e.Property(x => x.StatusChange).HasMaxLength(64);
            e.HasIndex(x => new { x.LeadId, x.CreatedUtc }).IsDescending(false, true).HasDatabaseName("IX_LeadNotes_Lead");
        });

        model.Entity<AffiliateRecord>(e =>
        {
            e.ToTable("Affiliates");                            // 028_affiliates.sql
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.Company).HasMaxLength(200);
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.Property(x => x.Status).HasDefaultValue(0);
            e.Property(x => x.AccountManager).HasMaxLength(160);
            e.Property(x => x.PayeeReference).HasMaxLength(140);
            e.Property(x => x.AffiliateRatePct).HasPrecision(9, 4).HasDefaultValue(0m);
            e.Property(x => x.ProfitMarginPct).HasPrecision(9, 4).HasDefaultValue(0m);
            e.HasIndex(x => x.UpdatedUtc).IsDescending().HasDatabaseName("IX_Affiliates_UpdatedUtc");
        });

        model.Entity<AffiliatePayoutRecord>(e =>
        {
            e.ToTable("AffiliatePayouts");                      // 029_affiliate_payouts.sql
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("GBP");
            e.Property(x => x.Status).HasDefaultValue(0);
            e.Property(x => x.Reference).HasMaxLength(200);
            e.HasIndex(x => new { x.AffiliateId, x.CreatedUtc }).IsDescending(false, true).HasDatabaseName("IX_AffiliatePayouts_Affiliate");
        });

        model.Entity<AffiliateAgreementRecord>(e =>
        {
            e.ToTable("AffiliateAgreements");                   // 030_affiliate_agreements.sql + 032_agreement_expiry.sql
            e.Property(x => x.Token).HasMaxLength(64);
            e.Property(x => x.Status).HasDefaultValue(0);
            e.Property(x => x.CountersignatoryName).HasMaxLength(160);
            e.Property(x => x.CountersignatoryTitle).HasMaxLength(160);
            e.Property(x => x.SignedByName).HasMaxLength(160);
            e.HasIndex(x => x.Token).IsUnique().HasDatabaseName("UX_AffiliateAgreements_Token");  // R9 token lookup
        });

        // PostgreSQL: the Dapper repositories issue unquoted SQL, which PostgreSQL folds to lower case, so the
        // EF-authored tables/columns must be lower case to match. SQL Server is case-insensitive and keeps
        // the readable PascalCase from the scripts. (PostgreSQL commercial EF is deferred — see §7/§8.)
        if (Database.IsNpgsql())
        {
            ApplyLowerCaseNames(model);
        }
    }

    /// <summary>Folds every table, column, key and index name to lower case (PostgreSQL parity with Dapper).</summary>
    private static void ApplyLowerCaseNames(ModelBuilder model)
    {
        foreach (var entity in model.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } table)
            {
                entity.SetTableName(table.ToLowerInvariant());
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToLowerInvariant());
            }

            foreach (var key in entity.GetKeys())
            {
                if (key.GetName() is { } keyName)
                {
                    key.SetName(keyName.ToLowerInvariant());
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                if (index.GetDatabaseName() is { } indexName)
                {
                    index.SetDatabaseName(indexName.ToLowerInvariant());
                }
            }
        }
    }
}
