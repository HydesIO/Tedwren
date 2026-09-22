using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddAccreditationMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClientRequired",
                table: "TradeQualificationRequirements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "TradeQualificationRequirements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalMandatory",
                table: "TradeQualificationRequirements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "QualificationTypes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CaptureClientId",
                table: "QualificationCards",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientRequired",
                table: "TradeQualificationRequirements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TradeQualificationRequirements");

            migrationBuilder.DropColumn(
                name: "LegalMandatory",
                table: "TradeQualificationRequirements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "QualificationTypes");

            migrationBuilder.DropColumn(
                name: "CaptureClientId",
                table: "QualificationCards");
        }
    }
}
