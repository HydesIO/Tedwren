namespace Tedwren.DataAccess.Ef;

// These flat entity classes exist ONLY to describe the database schema to EF Core so it can author and apply
// migrations (DDL). They mirror the tables 1:1 — same names, columns and types as the hand-written scripts —
// and are never used for runtime reads/writes, which stay on Dapper (DML). Keeping them separate from the
// domain entities keeps the domain free of persistence concerns. See docs/ef-migrations.md.

/// <summary>Schema row for the <c>Companies</c> table (SF-1).</summary>
public sealed class CompanyRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Trade { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>CompanyDocuments</c> table (SUB-4: insurances, accreditations, policies).</summary>
public sealed class CompanyDocumentRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateOnly? ExpiresOn { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Persons</c> table (SF-1: one person per mobile number).</summary>
public sealed class PersonRecord
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Engagements</c> table (SF-2/SF-3).</summary>
public sealed class EngagementRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Trade { get; set; }
    public string? InternalReference { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ArchivedUtc { get; set; }
}

/// <summary>Schema row for the <c>QualificationTypes</c> table (SF-10/SF-12).</summary>
public sealed class QualificationTypeRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Issuer { get; set; }
    public int DefaultValidityMonths { get; set; }
    public bool IsCscsVerifiable { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>QualificationCards</c> table (SF-5–SF-8).</summary>
public sealed class QualificationCardRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid QualificationTypeId { get; set; }
    public string? CardNumber { get; set; }
    public string? HolderName { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int CaptureSource { get; set; }
    public string? ImageReference { get; set; }
    public int VerificationState { get; set; }
    public bool NeedsReview { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedUtc { get; set; }
    public Guid? SupersedesCardId { get; set; }
    public Guid? SupersededByCardId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>TradeQualificationRequirements</c> table (SF-11).</summary>
public sealed class TradeQualificationRequirementRecord
{
    public Guid Id { get; set; }
    public string Trade { get; set; } = string.Empty;
    public Guid QualificationTypeId { get; set; }
}

/// <summary>Schema row for the <c>ExpiryNotifications</c> table (SF-9).</summary>
public sealed class ExpiryNotificationRecord
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public int Stage { get; set; }
    public int Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public DateTimeOffset SentUtc { get; set; }
}

/// <summary>Schema row for the <c>JobRuns</c> table (SF-21, R12).</summary>
public sealed class JobRunRecord
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public DateTimeOffset StartedUtc { get; set; }
    public DateTimeOffset? FinishedUtc { get; set; }
    public int Status { get; set; }
    public int ItemsProcessed { get; set; }
    public int NotificationsSent { get; set; }
    public string? Error { get; set; }
}

/// <summary>Schema row for the <c>Sites</c> table (SF-6/SF-14/SF-25/SF-26); boundary is three nullable columns.</summary>
public sealed class SiteRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Client { get; set; }
    public string? Region { get; set; }
    public string? Address { get; set; }
    public bool HasCompound { get; set; }
    public bool IsDispersed { get; set; }
    public double? BoundaryLatitude { get; set; }
    public double? BoundaryLongitude { get; set; }
    public double? BoundaryRadiusMetres { get; set; }
    public int LocationPolicy { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>SiteProperties</c> table (SF-26).</summary>
public sealed class SitePropertyRecord
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Units { get; set; }
    public double BoundaryLatitude { get; set; }
    public double BoundaryLongitude { get; set; }
    public double BoundaryRadiusMetres { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Users</c> table (SF-20/SF-23).</summary>
public sealed class UserRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? LastActiveUtc { get; set; }
    public string? PasswordHash { get; set; }
    public DateTimeOffset? PasswordSetUtc { get; set; }
    public string? InviteToken { get; set; }
    public DateTimeOffset? InviteTokenExpiresUtc { get; set; }
}

/// <summary>Schema row for the <c>Attendance</c> table (SF-13–SF-19).</summary>
public sealed class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid SiteId { get; set; }
    public Guid? PropertyId { get; set; }
    public int Type { get; set; }
    public int Outcome { get; set; }
    public int Method { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool? WithinBoundary { get; set; }
    public string? Reason { get; set; }
    public Guid? CorrectsRecordId { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>ModuleEntitlements</c> table (SF-22, Q2).</summary>
public sealed class ModuleEntitlementRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>ReferenceValues</c> table (form option lists).</summary>
public sealed class ReferenceValueRecord
{
    public Guid Id { get; set; }
    public string ListKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>Schema row for the <c>CompanySettings</c> table (per-company general settings, System Configuration).</summary>
public sealed class CompanySettingsRecord
{
    public Guid CompanyId { get; set; }
    public string SettingsJson { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>OnboardingLinks</c> table (self-service onboarding, SF-4/SUB-2).</summary>
public sealed class OnboardingLinkRecord
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? PasscodeHash { get; set; }
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Trade { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
    public int Status { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? EngagementId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>StoredImages</c> table (private card photos, R9).</summary>
public sealed class StoredImageRecord
{
    public Guid Id { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Permits</c> table (permits to work).</summary>
public sealed class PermitRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string PermitType { get; set; } = string.Empty;
    public string? SiteName { get; set; }
    public string? ResponsiblePerson { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? Description { get; set; }
    public bool HighRisk { get; set; }
    public bool RamsAttached { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>AuditEntries</c> table (SF-20).</summary>
public sealed class AuditEntryRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Category { get; set; }
}

/// <summary>Schema row for the <c>Decisions</c> table (R10); checks are stored as JSON.</summary>
public sealed class DecisionRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid SiteId { get; set; }
    public bool Admitted { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public string? ChecksJson { get; set; }
}

/// <summary>Schema row for the <c>Timesheets</c> table (SUB-7–SUB-12).</summary>
public sealed class TimesheetRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PersonId { get; set; }
    public string OperativeName { get; set; } = string.Empty;
    public DateOnly WeekStart { get; set; }
    public int Status { get; set; }
    public DateTimeOffset? SubmittedUtc { get; set; }
    public string? DecidedBy { get; set; }
    public DateTimeOffset? DecidedUtc { get; set; }
    public int? DecisionScope { get; set; }
    public string? ReturnReason { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>TimesheetEntries</c> table (append-only lines, SUB-9).</summary>
public sealed class TimesheetEntryRecord
{
    public Guid Id { get; set; }
    public Guid TimesheetId { get; set; }
    public DateOnly WorkDate { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public Guid? CorrectsEntryId { get; set; }
    public string? Author { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>CompliancePacks</c> table (SUB-13–SUB-26); subjects are stored as JSON.</summary>
public sealed class CompliancePackRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? SiteId { get; set; }
    public string? SiteName { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PasscodeHash { get; set; } = string.Empty;
    public int Status { get; set; }
    public Guid? SupersededByPackId { get; set; }
    public string? SubjectsJson { get; set; }
}

/// <summary>Schema row for the <c>PackAccessEvents</c> table (SUB-24).</summary>
public sealed class PackAccessEventRecord
{
    public Guid Id { get; set; }
    public Guid PackId { get; set; }
    public int Kind { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
}

/// <summary>Schema row for the <c>InductionTemplates</c> table (MC-1–MC-7); steps/questions stored as JSON (R5).</summary>
public sealed class InductionTemplateRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ValidityDays { get; set; }
    public int PassMark { get; set; }
    public string StepsJson { get; set; } = string.Empty;
    public string QuestionsJson { get; set; } = string.Empty;
    public int AttemptLimit { get; set; }
    public bool Mandatory { get; set; }
    public string? MediaUrl { get; set; }
    public Guid? SiteId { get; set; }
}

/// <summary>Schema row for the <c>InductionSessions</c> table (MC-1–MC-7, MC-20).</summary>
public sealed class InductionSessionRecord
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTimeOffset StartedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public string? CompletionReference { get; set; }
    public DateTimeOffset? ExpiresUtc { get; set; }
    public int AttemptCount { get; set; }
    public int? LastScore { get; set; }
    public string? CompletedStepsJson { get; set; }
    public string? SignatureName { get; set; }
    public bool ConsentGiven { get; set; }
    public string? ResetReason { get; set; }
    public Guid? SupersededBySessionId { get; set; }
}

/// <summary>Schema row for the <c>FormTemplates</c> table (PRD-Phase 2 Forms Library); sections/fields stored as JSON.</summary>
public sealed class FormTemplateRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public int Status { get; set; }
    public string SectionsJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>FormSubmissions</c> table (PRD-Phase 2 Forms Library); answers stored as JSON, append-only (R4/R10).</summary>
public sealed class FormSubmissionRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FormTemplateId { get; set; }
    public int FormTemplateVersion { get; set; }
    public string FormName { get; set; } = string.Empty;
    public int Scope { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? PersonId { get; set; }
    public string AnswersJson { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTimeOffset SubmittedUtc { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
}

/// <summary>Schema row for the <c>FormSubmissionFiles</c> table (PRD-Phase 2 Forms Library); captured photos/attachments as blobs.</summary>
public sealed class FormSubmissionFileRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SubmissionId { get; set; }
    public string FieldId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTimeOffset UploadedUtc { get; set; }
}

/// <summary>Schema row for the <c>FormAssignments</c> table (PRD-Phase 2 Forms Library); assigns a form family to a scope.</summary>
public sealed class FormAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FormTemplateFamilyId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public int Scope { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? InductionTemplateId { get; set; }
    public int Schedule { get; set; }
    public string? FailureAlertEmail { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? LastReminderUtc { get; set; }
}
