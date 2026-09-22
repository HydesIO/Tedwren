using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddRamsReviewCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRamsReviewReminderUtc",
                table: "SubcontractorOnboardingConfigs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RamsFamilyId",
                table: "SubcontractorOnboardingConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "RamsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRamsReviewReminderUtc",
                table: "SubcontractorOnboardingConfigs");

            migrationBuilder.DropColumn(
                name: "RamsFamilyId",
                table: "SubcontractorOnboardingConfigs");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "RamsSubmissions");
        }
    }
}
