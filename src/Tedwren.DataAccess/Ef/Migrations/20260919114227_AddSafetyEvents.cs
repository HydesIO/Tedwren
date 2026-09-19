using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_HazardReports_CompanyId_ReportedUtc",
                table: "HazardReports",
                columns: new[] { "CompanyId", "ReportedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_CompanyId_ReportedUtc",
                table: "IncidentReports",
                columns: new[] { "CompanyId", "ReportedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HazardReports");

            migrationBuilder.DropTable(
                name: "IncidentReports");
        }
    }
}
