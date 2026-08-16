using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Commercial.Ef.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommercialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffiliateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TermsHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountersignatoryName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CountersignatoryTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SignatureDataUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedByName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SignedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PdfBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AffiliatePayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffiliateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "GBP"),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliatePayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Affiliates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AccountManager = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    PayeeReference = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    AffiliateRatePct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0m),
                    ProfitMarginPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0m),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affiliates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MandateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MeterKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BandKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaunchSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UtmSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UtmMedium = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UtmCampaign = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NotifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailLower = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, computedColumnSql: "LOWER(Email)", stored: true),
                    Unsubscribed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UnsubscribeToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchSignups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusChange = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Model = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NumberOfSites = table.Column<int>(type: "int", nullable: true),
                    EstimatedRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AccountManager = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    CompanyNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AffiliateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mandates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoCardlessBillingRequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    GoCardlessMandateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    GoCardlessCustomerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    AccountNumberEnding = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mandates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MandateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoCardlessPaymentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AmountPence = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChargeDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoCardlessPayoutId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AmountPence = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    ArrivalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoCardlessEventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AffiliateAgreements_Token",
                table: "AffiliateAgreements",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliatePayouts_Affiliate",
                table: "AffiliatePayouts",
                columns: new[] { "AffiliateId", "CreatedUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Affiliates_UpdatedUtc",
                table: "Affiliates",
                column: "UpdatedUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_BillingSubscriptions_Company",
                table: "BillingSubscriptions",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LaunchSignups_Email",
                table: "LaunchSignups",
                column: "EmailLower",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LaunchSignups_Unsub",
                table: "LaunchSignups",
                column: "UnsubscribeToken",
                unique: true,
                filter: "[UnsubscribeToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeadNotes_Lead",
                table: "LeadNotes",
                columns: new[] { "LeadId", "CreatedUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_UpdatedUtc",
                table: "Leads",
                column: "UpdatedUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Mandates_Company",
                table: "Mandates",
                columns: new[] { "CompanyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Mandates_GcMandate",
                table: "Mandates",
                column: "GoCardlessMandateId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Company",
                table: "Payments",
                columns: new[] { "CompanyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GcPayment",
                table: "Payments",
                column: "GoCardlessPaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_Payouts_GcPayout",
                table: "Payouts",
                column: "GoCardlessPayoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Received",
                table: "WebhookEvents",
                column: "ReceivedUtc");

            migrationBuilder.CreateIndex(
                name: "UX_WebhookEvents_EventId",
                table: "WebhookEvents",
                column: "GoCardlessEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliateAgreements");

            migrationBuilder.DropTable(
                name: "AffiliatePayouts");

            migrationBuilder.DropTable(
                name: "Affiliates");

            migrationBuilder.DropTable(
                name: "BillingSubscriptions");

            migrationBuilder.DropTable(
                name: "LaunchSignups");

            migrationBuilder.DropTable(
                name: "LeadNotes");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Mandates");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}
