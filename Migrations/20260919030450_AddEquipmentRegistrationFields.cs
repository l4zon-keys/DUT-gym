using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Equipment");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Equipment",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Equipment",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Equipment",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VenueId",
                table: "Equipment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_QrCode",
                table: "Equipment",
                column: "QrCode",
                unique: true,
                filter: "[QrCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_SerialNumber",
                table: "Equipment",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_VenueId",
                table: "Equipment",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Venues_VenueId",
                table: "Equipment",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Venues_VenueId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_QrCode",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_SerialNumber",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_VenueId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "Equipment");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Equipment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
