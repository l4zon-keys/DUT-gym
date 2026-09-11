using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginFormASPCore6.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentPurchaseInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Equipment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InsuranceExpiryDate",
                table: "Equipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicyNumber",
                table: "Equipment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "Equipment",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Equipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "Equipment",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "Equipment",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiryDate",
                table: "Equipment",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "InsuranceExpiryDate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "InsurancePolicyNumber",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiryDate",
                table: "Equipment");
        }
    }
}
