using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class RequireStaffApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrainerApprovalStatus",
                table: "Users",
                newName: "ApprovalStatus");

            // Staff didn't need approval before this migration - grandfather in every
            // existing Staff account as Approved so nobody already using the app gets
            // locked out. Only new Staff signups from here on start Pending.
            migrationBuilder.Sql("UPDATE [Users] SET [ApprovalStatus] = 'Approved' WHERE [Role] = 'Staff';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "Users",
                newName: "TrainerApprovalStatus");
        }
    }
}
