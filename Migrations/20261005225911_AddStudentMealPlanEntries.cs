using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentMealPlanEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentMealPlanEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Day = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Slot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MealName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CaloriesKcal = table.Column<int>(type: "int", nullable: false),
                    ProteinG = table.Column<int>(type: "int", nullable: false),
                    CarbsG = table.Column<int>(type: "int", nullable: false),
                    FatG = table.Column<int>(type: "int", nullable: false),
                    EstimatedCostRand = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMealPlanEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMealPlanEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentMealPlanEntries_UserId_WeekStartDate_Day_Slot",
                table: "StudentMealPlanEntries",
                columns: new[] { "UserId", "WeekStartDate", "Day", "Slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentMealPlanEntries");
        }
    }
}
