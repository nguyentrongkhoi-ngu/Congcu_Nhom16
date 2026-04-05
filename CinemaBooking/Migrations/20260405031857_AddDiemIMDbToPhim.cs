using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddDiemIMDbToPhim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "diem_imdb",
                table: "phim",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "diem_imdb",
                table: "phim");
        }
    }
}
