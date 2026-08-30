using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalTrainerAndRegistrationProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonalTrainerOption",
                table: "Memberships",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProofOfRegistrationFilePath",
                table: "Memberships",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalTrainerOption",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "ProofOfRegistrationFilePath",
                table: "Memberships");
        }
    }
}
