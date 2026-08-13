using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddFormAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignments_CompanyId",
                table: "FormAssignments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignments_CompanyId_FormTemplateFamilyId",
                table: "FormAssignments",
                columns: new[] { "CompanyId", "FormTemplateFamilyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormAssignments");
        }
    }
}
