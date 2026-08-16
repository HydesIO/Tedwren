using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "ExpiryNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationTypes", x => x.Id);
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
                name: "TradeQualificationRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QualificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeQualificationRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
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
                name: "IX_Engagements_CompanyId_PersonId",
                table: "Engagements",
                columns: new[] { "CompanyId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpiryNotifications_CardId_Stage_Channel_Recipient",
                table: "ExpiryNotifications",
                columns: new[] { "CardId", "Stage", "Channel", "Recipient" },
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
                name: "IX_ReferenceValues_ListKey_Value",
                table: "ReferenceValues",
                columns: new[] { "ListKey", "Value" },
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
                name: "IX_TradeQualificationRequirements_Trade_QualificationTypeId",
                table: "TradeQualificationRequirements",
                columns: new[] { "Trade", "QualificationTypeId" },
                unique: true);

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
                name: "Engagements");

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
                name: "InductionSessions");

            migrationBuilder.DropTable(
                name: "InductionTemplates");

            migrationBuilder.DropTable(
                name: "JobRuns");

            migrationBuilder.DropTable(
                name: "ModuleEntitlements");

            migrationBuilder.DropTable(
                name: "OnboardingLinks");

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
                name: "ReferenceValues");

            migrationBuilder.DropTable(
                name: "SiteProperties");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "StoredImages");

            migrationBuilder.DropTable(
                name: "TimesheetEntries");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "TradeQualificationRequirements");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
