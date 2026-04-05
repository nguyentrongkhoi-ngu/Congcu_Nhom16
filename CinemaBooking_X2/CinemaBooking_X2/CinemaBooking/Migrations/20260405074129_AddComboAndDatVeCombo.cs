using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddComboAndDatVeCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "combo",
                columns: table => new
                {
                    ma_combo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_combo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gia = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    hinh_anh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combo", x => x.ma_combo);
                });

            migrationBuilder.CreateTable(
                name: "dat_ve_combo",
                columns: table => new
                {
                    ma_dat_ve = table.Column<int>(type: "int", nullable: false),
                    ma_combo = table.Column<int>(type: "int", nullable: false),
                    so_luong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dat_ve_combo", x => new { x.ma_dat_ve, x.ma_combo });
                    table.ForeignKey(
                        name: "FK_dat_ve_combo_combo_ma_combo",
                        column: x => x.ma_combo,
                        principalTable: "combo",
                        principalColumn: "ma_combo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dat_ve_combo_dat_ve_ma_dat_ve",
                        column: x => x.ma_dat_ve,
                        principalTable: "dat_ve",
                        principalColumn: "ma_dat_ve",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_combo_ma_combo",
                table: "dat_ve_combo",
                column: "ma_combo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dat_ve_combo");

            migrationBuilder.DropTable(
                name: "combo");
        }
    }
}
