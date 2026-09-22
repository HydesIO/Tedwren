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
    public DbSet<CompanyDocumentRecord> CompanyDocuments => Set<CompanyDocumentRecord>();
    public DbSet<PersonRecord> Persons => Set<PersonRecord>();
    public DbSet<EngagementRecord> Engagements => Set<EngagementRecord>();
    public DbSet<QualificationTypeRecord> QualificationTypes => Set<QualificationTypeRecord>();
    public DbSet<QualificationCardRecord> QualificationCards => Set<QualificationCardRecord>();
    public DbSet<TradeQualificationRequirementRecord> TradeQualificationRequirements => Set<TradeQualificationRequirementRecord>();
    public DbSet<ExpiryNotificationRecord> ExpiryNotifications => Set<ExpiryNotificationRecord>();
    public DbSet<JobRunRecord> JobRuns => Set<JobRunRecord>();
    public DbSet<SiteRecord> Sites => Set<SiteRecord>();
    public DbSet<SitePropertyRecord> SiteProperties => Set<SitePropertyRecord>();
    public DbSet<SiteAssignmentRecord> SiteAssignments => Set<SiteAssignmentRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();
    public DbSet<ModuleEntitlementRecord> ModuleEntitlements => Set<ModuleEntitlementRecord>();
    public DbSet<ReferenceValueRecord> ReferenceValues => Set<ReferenceValueRecord>();
    public DbSet<MasterListItemRecord> MasterListItems => Set<MasterListItemRecord>();
    public DbSet<CompanySettingsRecord> CompanySettings => Set<CompanySettingsRecord>();
    public DbSet<PermitRecord> Permits => Set<PermitRecord>();
    public DbSet<AssetRecord> Assets => Set<AssetRecord>();
    public DbSet<RamsSubmissionRecord> RamsSubmissions => Set<RamsSubmissionRecord>();
    public DbSet<RamsAcknowledgementRecord> RamsAcknowledgements => Set<RamsAcknowledgementRecord>();
    public DbSet<DocumentDistributionRecord> DocumentDistributions => Set<DocumentDistributionRecord>();
    public DbSet<DocumentAcknowledgementRecord> DocumentAcknowledgements => Set<DocumentAcknowledgementRecord>();
    public DbSet<HazardReportRecord> HazardReports => Set<HazardReportRecord>();
    public DbSet<IncidentReportRecord> IncidentReports => Set<IncidentReportRecord>();
    public DbSet<HavsExposureRow> HavsExposureRecords => Set<HavsExposureRow>();
    public DbSet<OnboardingLinkRecord> OnboardingLinks => Set<OnboardingLinkRecord>();
    public DbSet<InductionLinkRecord> InductionLinks => Set<InductionLinkRecord>();
    public DbSet<TradeInviteRecord> TradeInvites => Set<TradeInviteRecord>();
    public DbSet<SubcontractorOnboardingConfigRecord> SubcontractorOnboardingConfigs => Set<SubcontractorOnboardingConfigRecord>();
    public DbSet<OperativeDeviceRecord> OperativeDevices => Set<OperativeDeviceRecord>();
    public DbSet<OtpChallengeRecord> OtpChallenges => Set<OtpChallengeRecord>();
    public DbSet<UserRefreshTokenRecord> UserRefreshTokens => Set<UserRefreshTokenRecord>();
    public DbSet<EvidenceItemRecord> EvidenceItems => Set<EvidenceItemRecord>();
    public DbSet<StoredImageRecord> StoredImages => Set<StoredImageRecord>();
    public DbSet<AuditEntryRecord> AuditEntries => Set<AuditEntryRecord>();
    public DbSet<DecisionRecord> Decisions => Set<DecisionRecord>();
    public DbSet<TimesheetRecord> Timesheets => Set<TimesheetRecord>();
    public DbSet<TimesheetEntryRecord> TimesheetEntries => Set<TimesheetEntryRecord>();
    public DbSet<CompliancePackRecord> CompliancePacks => Set<CompliancePackRecord>();
    public DbSet<PackAccessEventRecord> PackAccessEvents => Set<PackAccessEventRecord>();
    public DbSet<InductionTemplateRecord> InductionTemplates => Set<InductionTemplateRecord>();
    public DbSet<InductionSessionRecord> InductionSessions => Set<InductionSessionRecord>();
    public DbSet<FormTemplateRecord> FormTemplates => Set<FormTemplateRecord>();
    public DbSet<FormSubmissionRecord> FormSubmissions => Set<FormSubmissionRecord>();
    public DbSet<FormSubmissionFileRecord> FormSubmissionFiles => Set<FormSubmissionFileRecord>();
    public DbSet<FormAssignmentRecord> FormAssignments => Set<FormAssignmentRecord>();

    /// <summary>Maps every schema record to its table, keys and indexes, mirroring the hand-written scripts.</summary>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CompanyRecord>(e => e.ToTable("Companies"));

        model.Entity<CompanyDocumentRecord>(e =>
        {
            e.ToTable("CompanyDocuments");
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Type).HasMaxLength(128);
            e.Property(x => x.Reference).HasMaxLength(128);
            e.Property(x => x.FileReference).HasMaxLength(128);
            e.Property(x => x.Version).HasDefaultValue(1);      // MC-27: existing rows are version 1
            e.HasIndex(x => x.CompanyId);                       // SUB-4
        });

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
            e.HasIndex(x => new { x.Source, x.SubjectId, x.Stage, x.Channel, x.Recipient }).IsUnique();  // SF-9
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

        model.Entity<SiteAssignmentRecord>(e =>
        {
            e.ToTable("SiteAssignments");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.SiteId }).IsUnique();
        });

        model.Entity<UserRecord>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Mobile).HasMaxLength(32);
            e.Property(x => x.PasswordHash).HasMaxLength(512);
            e.Property(x => x.InviteToken).HasMaxLength(128);
            e.HasIndex(x => x.Email).IsUnique();               // one account per email
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => x.InviteToken);
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

        model.Entity<ReferenceValueRecord>(e =>
        {
            e.ToTable("ReferenceValues");
            e.Property(x => x.ListKey).HasMaxLength(64);
            e.Property(x => x.Value).HasMaxLength(256);
            e.HasIndex(x => new { x.ListKey, x.Value }).IsUnique();
        });

        model.Entity<MasterListItemRecord>(e =>
        {
            e.ToTable("MasterListItems");
            e.Property(x => x.ListKey).HasMaxLength(64);
            e.Property(x => x.Value).HasMaxLength(256);
            e.HasIndex(x => new { x.ListKey, x.CompanyId });   // list lookups scoped to global + a tenant (R15, spec §5–§8)
        });

        model.Entity<CompanySettingsRecord>(e =>
        {
            e.ToTable("CompanySettings");
            e.HasKey(x => x.CompanyId);
        });

        model.Entity<OnboardingLinkRecord>(e =>
        {
            e.ToTable("OnboardingLinks");
            e.Property(x => x.Token).HasMaxLength(128);
            e.Property(x => x.PasscodeHash).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Trade).HasMaxLength(128);
            e.HasIndex(x => x.Token).IsUnique();
        });

        model.Entity<InductionLinkRecord>(e =>
        {
            e.ToTable("InductionLinks");
            e.Property(x => x.Token).HasMaxLength(128);
            e.Property(x => x.PasscodeHash).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.Token).IsUnique();
        });

        model.Entity<TradeInviteRecord>(e =>
        {
            e.ToTable("TradeInvites");
            e.Property(x => x.Token).HasMaxLength(128);
            e.Property(x => x.PasscodeHash).HasMaxLength(256);
            e.Property(x => x.ContactName).HasMaxLength(256);
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.Property(x => x.DecidedBy).HasMaxLength(256);
            e.Property(x => x.ReviewNote).HasMaxLength(1024);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.InviterCompanyId);   // the review queue is scoped to the inviting tenant (R15)
        });

        model.Entity<SubcontractorOnboardingConfigRecord>(e =>
        {
            e.ToTable("SubcontractorOnboardingConfigs");
            e.HasIndex(x => x.SubcontractorCompanyId);   // the Gate 1/5 lookup
            e.HasIndex(x => x.TradeInviteId);
            e.HasIndex(x => x.InviterCompanyId);         // scoped to the inviting tenant (R15)
        });

        model.Entity<OperativeDeviceRecord>(e =>
        {
            e.ToTable("OperativeDevices");
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.Property(x => x.DeviceName).HasMaxLength(256);
            e.Property(x => x.RefreshTokenHash).HasMaxLength(512);
            e.HasIndex(x => x.DeviceId).IsUnique();  // one binding per install (M2)
            e.HasIndex(x => x.PersonId);             // look up an operative's active device
        });

        model.Entity<OtpChallengeRecord>(e =>
        {
            e.ToTable("OtpChallenges");
            e.Property(x => x.PhoneNumber).HasMaxLength(32);
            e.Property(x => x.CodeHash).HasMaxLength(512);
            e.HasIndex(x => x.PhoneNumber);
        });

        model.Entity<UserRefreshTokenRecord>(e =>
        {
            e.ToTable("UserRefreshTokens");
            e.Property(x => x.TokenHash).HasMaxLength(512);
            e.HasIndex(x => x.UserId);  // renew/revoke a user's sessions (M8)
        });

        model.Entity<EvidenceItemRecord>(e =>
        {
            e.ToTable("EvidenceItems");
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.PhotoReference).HasMaxLength(256);
            e.HasIndex(x => x.CompanyId);  // a company's captures (review surface, M7)
            e.HasIndex(x => x.PersonId);   // an operative's own captures
        });

        model.Entity<StoredImageRecord>(e =>
        {
            e.ToTable("StoredImages");
            e.Property(x => x.ContentType).HasMaxLength(128);
        });

        model.Entity<PermitRecord>(e =>
        {
            e.ToTable("Permits");
            e.Property(x => x.PermitType).HasMaxLength(128);
            e.Property(x => x.SiteName).HasMaxLength(256);
            e.Property(x => x.ResponsiblePerson).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CreatedUtc });
        });

        model.Entity<AssetRecord>(e =>
        {
            e.ToTable("Assets");
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.AssetType).HasMaxLength(128);
            e.Property(x => x.SerialNumber).HasMaxLength(128);
            e.Property(x => x.Location).HasMaxLength(256);
            e.Property(x => x.OwnerName).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.CreatedUtc });
        });

        model.Entity<RamsSubmissionRecord>(e =>
        {
            e.ToTable("RamsSubmissions");
            e.Property(x => x.Reference).HasMaxLength(64);
            e.Property(x => x.ContractorName).HasMaxLength(256);
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.SiteName).HasMaxLength(256);
            e.Property(x => x.FileReference).HasMaxLength(256);
            e.Property(x => x.ReviewedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.SubmittedUtc });
            e.HasIndex(x => new { x.CompanyId, x.FamilyId });
        });

        model.Entity<RamsAcknowledgementRecord>(e =>
        {
            e.ToTable("RamsAcknowledgements");
            e.Property(x => x.SignatureName).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.PersonId, x.FamilyId });
        });

        model.Entity<DocumentDistributionRecord>(e =>
        {
            e.ToTable("DocumentDistributions");
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Audience).HasMaxLength(256);
            e.Property(x => x.FileReference).HasMaxLength(256);
            e.Property(x => x.SentBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.SentUtc });
        });

        model.Entity<DocumentAcknowledgementRecord>(e =>
        {
            e.ToTable("DocumentAcknowledgements");
            e.Property(x => x.RecipientName).HasMaxLength(256);
            e.HasIndex(x => x.DistributionId);
            e.HasIndex(x => x.CompanyId);
        });

        model.Entity<HazardReportRecord>(e =>
        {
            e.ToTable("HazardReports");
            e.Property(x => x.Reference).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Location).HasMaxLength(256);
            e.Property(x => x.PhotoReference).HasMaxLength(256);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.AssignedTo).HasMaxLength(256);
            e.Property(x => x.ReportedBy).HasMaxLength(256);
            e.Property(x => x.ClosureNote).HasMaxLength(2000);
            e.HasIndex(x => new { x.CompanyId, x.ReportedUtc });
        });

        model.Entity<IncidentReportRecord>(e =>
        {
            e.ToTable("IncidentReports");
            e.Property(x => x.Reference).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Location).HasMaxLength(256);
            e.Property(x => x.InjuredPersonName).HasMaxLength(256);
            e.Property(x => x.InjuryDetail).HasMaxLength(512);
            e.Property(x => x.ImmediateCause).HasMaxLength(2000);
            e.Property(x => x.RootCause).HasMaxLength(2000);
            e.Property(x => x.CorrectiveActions).HasMaxLength(2000);
            e.Property(x => x.RiddorCategory).HasMaxLength(128);
            e.Property(x => x.ReportedBy).HasMaxLength(256);
            e.Property(x => x.InvestigatedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.ReportedUtc });
        });

        model.Entity<HavsExposureRow>(e =>
        {
            e.ToTable("HavsExposureRecords");
            e.Property(x => x.PersonName).HasMaxLength(256);
            e.Property(x => x.RecordedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.RecordedUtc });
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
            e.Property(x => x.MediaUrl).HasMaxLength(1024);
            e.HasIndex(x => x.CompanyId);
        });

        model.Entity<InductionSessionRecord>(e =>
        {
            e.ToTable("InductionSessions");
            e.HasIndex(x => new { x.CompanyId, x.StartedUtc });
            e.HasIndex(x => new { x.CompanyId, x.TemplateId, x.PersonId, x.Status });
        });

        model.Entity<FormTemplateRecord>(e =>
        {
            e.ToTable("FormTemplates");
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Description).HasMaxLength(1024);
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => new { x.CompanyId, x.FamilyId });
        });

        model.Entity<FormSubmissionRecord>(e =>
        {
            e.ToTable("FormSubmissions");
            e.Property(x => x.FormName).HasMaxLength(256);
            e.Property(x => x.SubmittedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.CompanyId, x.SubmittedUtc });
            e.HasIndex(x => x.FormTemplateId);
        });

        model.Entity<FormSubmissionFileRecord>(e =>
        {
            e.ToTable("FormSubmissionFiles");
            e.Property(x => x.FieldId).HasMaxLength(64);
            e.Property(x => x.FileName).HasMaxLength(512);
            e.Property(x => x.ContentType).HasMaxLength(128);
            e.HasIndex(x => x.SubmissionId);
        });

        model.Entity<FormAssignmentRecord>(e =>
        {
            e.ToTable("FormAssignments");
            e.Property(x => x.FormName).HasMaxLength(256);
            e.Property(x => x.FailureAlertEmail).HasMaxLength(256);
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => new { x.CompanyId, x.FormTemplateFamilyId });
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
