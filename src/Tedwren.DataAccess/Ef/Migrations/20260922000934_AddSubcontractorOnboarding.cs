using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    RamsReviewCycleMonths = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorOnboardingConfigs", x => x.Id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubcontractorOnboardingConfigs");
        }
    }
}
