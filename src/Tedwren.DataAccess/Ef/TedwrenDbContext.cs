using Microsoft.EntityFrameworkCore;

namespace Tedwren.DataAccess.Ef;

/// <summary>
/// EF Core context describing the Tedwren database schema so migrations can create and evolve it (DDL).
/// It is NOT used for runtime data access — that stays on the Dapper repositories (DML). The mappings mirror
/// the hand-written migration scripts exactly (table/column names, keys, unique indexes) so a schema produced
/// by EF is byte-compatible with what the Dapper repositories expect. On PostgreSQL all identifiers are
/// folded to lower case so the repositories' unquoted SQL resolves to them. See docs/ef-migrations.md.
/// </summary>
public sealed class TedwrenDbContext : DbContext
{
    /// <summary>Creates the context with the provider options supplied by the host or the design-time factory.</summary>
    public TedwrenDbContext(DbContextOptions<TedwrenDbContext> options) : base(options)
    {
    }

    public DbSet<CompanyRecord> Companies => Set<CompanyRecord>();
    public DbSet<PersonRecord> Persons => Set<PersonRecord>();
    public DbSet<EngagementRecord> Engagements => Set<EngagementRecord>();
    public DbSet<QualificationTypeRecord> QualificationTypes => Set<QualificationTypeRecord>();
    public DbSet<QualificationCardRecord> QualificationCards => Set<QualificationCardRecord>();
    public DbSet<TradeQualificationRequirementRecord> TradeQualificationRequirements => Set<TradeQualificationRequirementRecord>();
    public DbSet<ExpiryNotificationRecord> ExpiryNotifications => Set<ExpiryNotificationRecord>();
    public DbSet<JobRunRecord> JobRuns => Set<JobRunRecord>();
    public DbSet<SiteRecord> Sites => Set<SiteRecord>();
    public DbSet<SitePropertyRecord> SiteProperties => Set<SitePropertyRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();
    public DbSet<ModuleEntitlementRecord> ModuleEntitlements => Set<ModuleEntitlementRecord>();
    public DbSet<AuditEntryRecord> AuditEntries => Set<AuditEntryRecord>();
    public DbSet<DecisionRecord> Decisions => Set<DecisionRecord>();
    public DbSet<TimesheetRecord> Timesheets => Set<TimesheetRecord>();
    public DbSet<TimesheetEntryRecord> TimesheetEntries => Set<TimesheetEntryRecord>();
    public DbSet<CompliancePackRecord> CompliancePacks => Set<CompliancePackRecord>();
    public DbSet<PackAccessEventRecord> PackAccessEvents => Set<PackAccessEventRecord>();
    public DbSet<InductionTemplateRecord> InductionTemplates => Set<InductionTemplateRecord>();
    public DbSet<InductionSessionRecord> InductionSessions => Set<InductionSessionRecord>();

    /// <summary>Maps every schema record to its table, keys and indexes, mirroring the hand-written scripts.</summary>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CompanyRecord>(e => e.ToTable("Companies"));

        model.Entity<PersonRecord>(e =>
        {
            e.ToTable("Persons");
            e.Property(x => x.PhoneNumber).HasMaxLength(32);
            e.HasIndex(x => x.PhoneNumber).IsUnique();          // SF-1
        });

        model.Entity<EngagementRecord>(e =>
        {
            e.ToTable("Engagements");
            e.HasIndex(x => new { x.CompanyId, x.PersonId }).IsUnique();  // SF-2
        });

        model.Entity<QualificationTypeRecord>(e => e.ToTable("QualificationTypes"));

        model.Entity<QualificationCardRecord>(e =>
        {
            e.ToTable("QualificationCards");
            e.HasIndex(x => x.PersonId);
        });

        model.Entity<TradeQualificationRequirementRecord>(e =>
        {
            e.ToTable("TradeQualificationRequirements");
            e.Property(x => x.Trade).HasMaxLength(128);
            e.HasIndex(x => new { x.Trade, x.QualificationTypeId }).IsUnique();  // SF-11
        });

        model.Entity<ExpiryNotificationRecord>(e =>
        {
            e.ToTable("ExpiryNotifications");
            e.Property(x => x.Recipient).HasMaxLength(256);
            e.HasIndex(x => new { x.CardId, x.Stage, x.Channel, x.Recipient }).IsUnique();  // SF-9
        });

        model.Entity<JobRunRecord>(e =>
        {
            e.ToTable("JobRuns");
            e.Property(x => x.JobName).HasMaxLength(128);
            e.HasIndex(x => new { x.JobName, x.StartedUtc });
        });

        model.Entity<SiteRecord>(e =>
        {
            e.ToTable("Sites");
            e.HasIndex(x => x.CompanyId);
        });

        model.Entity<SitePropertyRecord>(e =>
        {
            e.ToTable("SiteProperties");
            e.HasIndex(x => x.SiteId);
        });

        model.Entity<UserRecord>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();               // one account per email
            e.HasIndex(x => x.CompanyId);
        });

        model.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("Attendance");
            e.HasIndex(x => new { x.PersonId, x.OccurredUtc });
            e.HasIndex(x => new { x.SiteId, x.OccurredUtc });
        });

        model.Entity<ModuleEntitlementRecord>(e =>
        {
            e.ToTable("ModuleEntitlements");
            e.Property(x => x.ModuleKey).HasMaxLength(64);
            e.HasIndex(x => new { x.CompanyId, x.ModuleKey }).IsUnique();  // Q2
        });

        model.Entity<AuditEntryRecord>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasIndex(x => new { x.CompanyId, x.OccurredUtc });
        });

        model.Entity<DecisionRecord>(e =>
        {
            e.ToTable("Decisions");
            e.HasIndex(x => new { x.PersonId, x.OccurredUtc });
            e.HasIndex(x => new { x.SiteId, x.OccurredUtc });
        });

        model.Entity<TimesheetRecord>(e =>
        {
            e.ToTable("Timesheets");
            e.Property(x => x.OperativeName).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.PersonId, x.WeekStart }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.WeekStart });
        });

        model.Entity<TimesheetEntryRecord>(e =>
        {
            e.ToTable("TimesheetEntries");
            e.Property(x => x.Hours).HasPrecision(9, 2);
            e.HasIndex(x => new { x.TimesheetId, x.CreatedUtc });
        });

        model.Entity<CompliancePackRecord>(e =>
        {
            e.ToTable("CompliancePacks");
            e.Property(x => x.Token).HasMaxLength(128);
            e.HasIndex(x => x.Token).IsUnique();               // R9
            e.HasIndex(x => new { x.CompanyId, x.CreatedUtc });
        });

        model.Entity<PackAccessEventRecord>(e =>
        {
            e.ToTable("PackAccessEvents");
            e.HasIndex(x => new { x.PackId, x.Kind });
        });

        model.Entity<InductionTemplateRecord>(e =>
        {
            e.ToTable("InductionTemplates");
            e.HasIndex(x => x.CompanyId);
        });

        model.Entity<InductionSessionRecord>(e =>
        {
            e.ToTable("InductionSessions");
            e.HasIndex(x => new { x.CompanyId, x.StartedUtc });
            e.HasIndex(x => new { x.CompanyId, x.TemplateId, x.PersonId, x.Status });
        });

        // PostgreSQL: the Dapper repositories issue unquoted SQL, which PostgreSQL folds to lower case, so the
        // EF-authored tables/columns must be lower case to match. SQL Server is case-insensitive and keeps
        // the readable PascalCase from the scripts.
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
