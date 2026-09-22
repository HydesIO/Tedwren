using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tedwren.DataAccess.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiryNotificationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpiryNotifications_CardId_Stage_Channel_Recipient",
                table: "ExpiryNotifications");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "ExpiryNotifications",
                newName: "SubjectId");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ExpiryNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExpiryNotifications_Source_SubjectId_Stage_Channel_Recipient",
                table: "ExpiryNotifications",
                columns: new[] { "Source", "SubjectId", "Stage", "Channel", "Recipient" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpiryNotifications_Source_SubjectId_Stage_Channel_Recipient",
                table: "ExpiryNotifications");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ExpiryNotifications");

            migrationBuilder.RenameColumn(
                name: "SubjectId",
                table: "ExpiryNotifications",
                newName: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiryNotifications_CardId_Stage_Channel_Recipient",
                table: "ExpiryNotifications",
                columns: new[] { "CardId", "Stage", "Channel", "Recipient" },
                unique: true);
        }
    }
}
