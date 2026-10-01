using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackPrice.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTriggered",
                table: "PriceAlerts",
                newName: "IsActive");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "PriceAlerts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TriggeredAt",
                table: "PriceAlerts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "PriceAlerts");

            migrationBuilder.DropColumn(
                name: "TriggeredAt",
                table: "PriceAlerts");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "PriceAlerts",
                newName: "IsTriggered");
        }
    }
}
