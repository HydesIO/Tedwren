using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAcknowledgements");

            migrationBuilder.DropTable(
                name: "DocumentDistributions");
        }
    }
}
