using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackPrice.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalProductInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProductId",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Products");
        }
    }
}
