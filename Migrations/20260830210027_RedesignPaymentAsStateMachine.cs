using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class RedesignPaymentAsStateMachine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_VerifiedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayProvider",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProofFilePath",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "VerifiedByUserId",
                table: "Payments",
                newName: "ConfirmedByUserId");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "Payments",
                newName: "PaidAt");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_VerifiedByUserId",
                table: "Payments",
                newName: "IX_Payments_ConfirmedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ConfirmedByUserId",
                table: "Payments",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Map old PaymentMethod/PaymentStatus values (from the removed gateway design)
            // onto the new ones so existing payment history (including real, already-paid
            // memberships) keeps working instead of throwing on enum parse.
            migrationBuilder.Sql("UPDATE [Payments] SET [Method] = 'Card' WHERE [Method] = 'Gateway';");
            migrationBuilder.Sql("UPDATE [Payments] SET [Method] = 'Cash' WHERE [Method] = 'ManualProof';");
            migrationBuilder.Sql("UPDATE [Payments] SET [Status] = 'Paid' WHERE [Status] = 'Verified';");
            migrationBuilder.Sql("UPDATE [Payments] SET [Status] = 'Failed' WHERE [Status] = 'Rejected';");
            // A Failed payment was never actually paid - the old VerifiedAt (now PaidAt)
            // only meant "when it was reviewed" for a rejection, not "when it settled".
            migrationBuilder.Sql("UPDATE [Payments] SET [PaidAt] = NULL WHERE [Status] = 'Failed';");
            // Reference is now required - give existing rows a legacy-looking one.
            migrationBuilder.Sql("UPDATE [Payments] SET [Reference] = 'PAYLEGACY' + RIGHT('000000' + CAST([Id] AS VARCHAR(6)), 6) WHERE [Reference] = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ConfirmedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "Payments",
                newName: "VerifiedAt");

            migrationBuilder.RenameColumn(
                name: "ConfirmedByUserId",
                table: "Payments",
                newName: "VerifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ConfirmedByUserId",
                table: "Payments",
                newName: "IX_Payments_VerifiedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "GatewayProvider",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayReference",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofFilePath",
                table: "Payments",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_VerifiedByUserId",
                table: "Payments",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
