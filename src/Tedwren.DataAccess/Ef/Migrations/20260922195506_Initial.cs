using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CertificationExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    NextInspectionDue = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    WithinBoundary = table.Column<bool>(type: "bit", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectsRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrgType = table.Column<int>(type: "int", nullable: true),
                    Trade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SupersedesDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededByDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "CompliancePacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasscodeHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SupersededByPackId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompliancePacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Admitted = table.Column<bool>(type: "bit", nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChecksJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAcknowledgements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FileReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SentBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDistributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Engagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArchivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engagements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    PhotoReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CapturedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpiryNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpiryNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormTemplateFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InductionTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Schedule = table.Column<int>(type: "int", nullable: false),
                    FailureAlertEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReminderUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissionFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormTemplateVersion = table.Column<int>(type: "int", nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HavsExposureRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExposureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToolUsagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecordedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HavsExposureRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HazardReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    PhotoReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReportedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReportedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosureNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HazardReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InjuredPersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InjuryDetail = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    ImmediateCause = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RootCause = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectiveActions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiddorReportable = table.Column<bool>(type: "bit", nullable: false),
                    RiddorCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReportedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReportedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InvestigatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InductionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasscodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InductionLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InductionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastScore = table.Column<int>(type: "int", nullable: true),
                    CompletedStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ResetReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupersededBySessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InductionSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InductionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidityDays = table.Column<int>(type: "int", nullable: false),
                    PassMark = table.Column<int>(type: "int", nullable: false),
                    StepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptLimit = table.Column<int>(type: "int", nullable: false),
                    Mandatory = table.Column<bool>(type: "bit", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InductionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ItemsProcessed = table.Column<int>(type: "int", nullable: false),
                    NotificationsSent = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterListItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleEntitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasscodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Trade = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EngagementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperativeDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnrolledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperativeDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackAccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackAccessEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermitType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighRisk = table.Column<bool>(type: "bit", nullable: false),
                    RamsAttached = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualificationCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CaptureSource = table.Column<int>(type: "int", nullable: false),
                    ImageReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationState = table.Column<int>(type: "int", nullable: false),
                    NeedsReview = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SupersedesCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededByCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaptureClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualificationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValidityMonths = table.Column<int>(type: "int", nullable: false),
                    IsCscsVerifiable = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RamsAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SignatureName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RamsAcknowledgements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RamsSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContractorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FileReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RamsSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    BoundaryLatitude = table.Column<double>(type: "float", nullable: false),
                    BoundaryLongitude = table.Column<double>(type: "float", nullable: false),
                    BoundaryRadiusMetres = table.Column<double>(type: "float", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasCompound = table.Column<bool>(type: "bit", nullable: false),
                    IsDispersed = table.Column<bool>(type: "bit", nullable: false),
                    BoundaryLatitude = table.Column<double>(type: "float", nullable: true),
                    BoundaryLongitude = table.Column<double>(type: "float", nullable: true),
                    BoundaryRadiusMetres = table.Column<double>(type: "float", nullable: true),
                    LocationPolicy = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Bytes = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorOnboardingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviterCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubcontractorCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeInviteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    RequiredDocumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SsstsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SmstsRequired = table.Column<bool>(type: "bit", nullable: false),
                    InductionValidityDays = table.Column<int>(type: "int", nullable: false),
                    InductionPassMark = table.Column<int>(type: "int", nullable: false),
                    InductionAttemptLimit = table.Column<int>(type: "int", nullable: false),
                    InductionTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RamsReviewCycleMonths = table.Column<int>(type: "int", nullable: true),
                    RamsFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastRamsReviewReminderUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorOnboardingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CorrectsEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperativeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionScope = table.Column<int>(type: "int", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasscodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviterCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeInvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeQualificationRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QualificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalMandatory = table.Column<bool>(type: "bit", nullable: false),
                    ClientRequired = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeQualificationRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AvatarImageReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastActiveUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PasswordSetUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InviteToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InviteTokenExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_CreatedUtc",
                table: "Assets",
                columns: new[] { "CompanyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_PersonId_OccurredUtc",
                table: "Attendance",
                columns: new[] { "PersonId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_SiteId_OccurredUtc",
                table: "Attendance",
                columns: new[] { "SiteId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_CompanyId_OccurredUtc",
                table: "AuditEntries",
                columns: new[] { "CompanyId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocuments_CompanyId",
                table: "CompanyDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePacks_CompanyId_CreatedUtc",
                table: "CompliancePacks",
                columns: new[] { "CompanyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePacks_Token",
                table: "CompliancePacks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_PersonId_OccurredUtc",
                table: "Decisions",
                columns: new[] { "PersonId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_SiteId_OccurredUtc",
                table: "Decisions",
                columns: new[] { "SiteId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAcknowledgements_CompanyId",
                table: "DocumentAcknowledgements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAcknowledgements_DistributionId",
                table: "DocumentAcknowledgements",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDistributions_CompanyId_SentUtc",
                table: "DocumentDistributions",
                columns: new[] { "CompanyId", "SentUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_CompanyId_PersonId",
                table: "Engagements",
                columns: new[] { "CompanyId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_CompanyId",
                table: "EvidenceItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_PersonId",
                table: "EvidenceItems",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiryNotifications_Source_SubjectId_Stage_Channel_Recipient",
                table: "ExpiryNotifications",
                columns: new[] { "Source", "SubjectId", "Stage", "Channel", "Recipient" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignments_CompanyId",
                table: "FormAssignments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignments_CompanyId_FormTemplateFamilyId",
                table: "FormAssignments",
                columns: new[] { "CompanyId", "FormTemplateFamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionFiles_SubmissionId",
                table: "FormSubmissionFiles",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_CompanyId_SubmittedUtc",
                table: "FormSubmissions",
                columns: new[] { "CompanyId", "SubmittedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormTemplateId",
                table: "FormSubmissions",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CompanyId",
                table: "FormTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CompanyId_FamilyId",
                table: "FormTemplates",
                columns: new[] { "CompanyId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_HavsExposureRecords_CompanyId_RecordedUtc",
                table: "HavsExposureRecords",
                columns: new[] { "CompanyId", "RecordedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HazardReports_CompanyId_ReportedUtc",
                table: "HazardReports",
                columns: new[] { "CompanyId", "ReportedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_CompanyId_ReportedUtc",
                table: "IncidentReports",
                columns: new[] { "CompanyId", "ReportedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InductionLinks_Token",
                table: "InductionLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InductionSessions_CompanyId_StartedUtc",
                table: "InductionSessions",
                columns: new[] { "CompanyId", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InductionSessions_CompanyId_TemplateId_PersonId_Status",
                table: "InductionSessions",
                columns: new[] { "CompanyId", "TemplateId", "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InductionTemplates_CompanyId",
                table: "InductionTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_JobName_StartedUtc",
                table: "JobRuns",
                columns: new[] { "JobName", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterListItems_ListKey_CompanyId",
                table: "MasterListItems",
                columns: new[] { "ListKey", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleEntitlements_CompanyId_ModuleKey",
                table: "ModuleEntitlements",
                columns: new[] { "CompanyId", "ModuleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingLinks_Token",
                table: "OnboardingLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperativeDevices_DeviceId",
                table: "OperativeDevices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperativeDevices_PersonId",
                table: "OperativeDevices",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_PhoneNumber",
                table: "OtpChallenges",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PackAccessEvents_PackId_Kind",
                table: "PackAccessEvents",
                columns: new[] { "PackId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_Permits_CompanyId_CreatedUtc",
                table: "Permits",
                columns: new[] { "CompanyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_PhoneNumber",
                table: "Persons",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualificationCards_PersonId",
                table: "QualificationCards",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_RamsAcknowledgements_CompanyId_PersonId_FamilyId",
                table: "RamsAcknowledgements",
                columns: new[] { "CompanyId", "PersonId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_RamsSubmissions_CompanyId_FamilyId",
                table: "RamsSubmissions",
                columns: new[] { "CompanyId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_RamsSubmissions_CompanyId_SubmittedUtc",
                table: "RamsSubmissions",
                columns: new[] { "CompanyId", "SubmittedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceValues_ListKey_Value",
                table: "ReferenceValues",
                columns: new[] { "ListKey", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteAssignments_UserId",
                table: "SiteAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAssignments_UserId_SiteId",
                table: "SiteAssignments",
                columns: new[] { "UserId", "SiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteProperties_SiteId",
                table: "SiteProperties",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CompanyId",
                table: "Sites",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorOnboardingConfigs_InviterCompanyId",
                table: "SubcontractorOnboardingConfigs",
                column: "InviterCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorOnboardingConfigs_SubcontractorCompanyId",
                table: "SubcontractorOnboardingConfigs",
                column: "SubcontractorCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorOnboardingConfigs_TradeInviteId",
                table: "SubcontractorOnboardingConfigs",
                column: "TradeInviteId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_CreatedUtc",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_CompanyId_PersonId_WeekStart",
                table: "Timesheets",
                columns: new[] { "CompanyId", "PersonId", "WeekStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_CompanyId_WeekStart",
                table: "Timesheets",
                columns: new[] { "CompanyId", "WeekStart" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeInvites_InviterCompanyId",
                table: "TradeInvites",
                column: "InviterCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeInvites_Token",
                table: "TradeInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeQualificationRequirements_Trade_QualificationTypeId",
                table: "TradeQualificationRequirements",
                columns: new[] { "Trade", "QualificationTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_InviteToken",
                table: "Users",
                column: "InviteToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "CompanyDocuments");

            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropTable(
                name: "CompliancePacks");

            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropTable(
                name: "DocumentAcknowledgements");

            migrationBuilder.DropTable(
                name: "DocumentDistributions");

            migrationBuilder.DropTable(
                name: "Engagements");

            migrationBuilder.DropTable(
                name: "EvidenceItems");

            migrationBuilder.DropTable(
                name: "ExpiryNotifications");

            migrationBuilder.DropTable(
                name: "FormAssignments");

            migrationBuilder.DropTable(
                name: "FormSubmissionFiles");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "FormTemplates");

            migrationBuilder.DropTable(
                name: "HavsExposureRecords");

            migrationBuilder.DropTable(
                name: "HazardReports");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "InductionLinks");

            migrationBuilder.DropTable(
                name: "InductionSessions");

            migrationBuilder.DropTable(
                name: "InductionTemplates");

            migrationBuilder.DropTable(
                name: "JobRuns");

            migrationBuilder.DropTable(
                name: "MasterListItems");

            migrationBuilder.DropTable(
                name: "ModuleEntitlements");

            migrationBuilder.DropTable(
                name: "OnboardingLinks");

            migrationBuilder.DropTable(
                name: "OperativeDevices");

            migrationBuilder.DropTable(
                name: "OtpChallenges");

            migrationBuilder.DropTable(
                name: "PackAccessEvents");

            migrationBuilder.DropTable(
                name: "Permits");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "QualificationCards");

            migrationBuilder.DropTable(
                name: "QualificationTypes");

            migrationBuilder.DropTable(
                name: "RamsAcknowledgements");

            migrationBuilder.DropTable(
                name: "RamsSubmissions");

            migrationBuilder.DropTable(
                name: "ReferenceValues");

            migrationBuilder.DropTable(
                name: "SiteAssignments");

            migrationBuilder.DropTable(
                name: "SiteProperties");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "StoredImages");

            migrationBuilder.DropTable(
                name: "SubcontractorOnboardingConfigs");

            migrationBuilder.DropTable(
                name: "TimesheetEntries");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "TradeInvites");

            migrationBuilder.DropTable(
                name: "TradeQualificationRequirements");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
