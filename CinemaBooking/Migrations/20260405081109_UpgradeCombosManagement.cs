using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeCombosManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "so_luong_ton",
                table: "combo",
                type: "int",
                nullable: false,
                defaultValue: 999);

            migrationBuilder.AddColumn<bool>(
                name: "trang_thai",
                table: "combo",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "uu_tien",
                table: "combo",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "so_luong_ton",
                table: "combo");

            migrationBuilder.DropColumn(
                name: "trang_thai",
                table: "combo");

            migrationBuilder.DropColumn(
                name: "uu_tien",
                table: "combo");
        }
    }
}
