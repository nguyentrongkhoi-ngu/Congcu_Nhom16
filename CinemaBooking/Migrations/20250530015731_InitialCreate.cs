using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "khuyen_mai",
                columns: table => new
                {
                    ma_khuyen_mai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    phan_tram_giam = table.Column<int>(type: "int", nullable: false),
                    ngay_bat_dau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ngay_ket_thuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_khuyen_mai", x => x.ma_khuyen_mai);
                });

            migrationBuilder.CreateTable(
                name: "phim",
                columns: table => new
                {
                    ma_phim = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_phim = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: false),
                    thoi_luong = table.Column<int>(type: "int", nullable: false),
                    the_loai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ngay_phat_hanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    url_poster = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    dinh_dang = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    trailer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phim", x => x.ma_phim);
                });

            migrationBuilder.CreateTable(
                name: "rap_phim",
                columns: table => new
                {
                    ma_rap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_rap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dia_chi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    thanh_pho = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rap_phim", x => x.ma_rap);
                });

            migrationBuilder.CreateTable(
                name: "vai_tro",
                columns: table => new
                {
                    ma_vai_tro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_vai_tro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    mo_ta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vai_tro", x => x.ma_vai_tro);
                });

            migrationBuilder.CreateTable(
                name: "ngon_ngu_phim",
                columns: table => new
                {
                    ma_ngon_ngu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phim = table.Column<int>(type: "int", nullable: true),
                    ngon_ngu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    phu_de = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ngon_ngu_phim", x => x.ma_ngon_ngu);
                    table.ForeignKey(
                        name: "FK_ngon_ngu_phim_phim_ma_phim",
                        column: x => x.ma_phim,
                        principalTable: "phim",
                        principalColumn: "ma_phim",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phong_chieu",
                columns: table => new
                {
                    ma_phong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_rap = table.Column<int>(type: "int", nullable: false),
                    so_phong = table.Column<int>(type: "int", nullable: false),
                    suc_chua = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phong_chieu", x => x.ma_phong);
                    table.ForeignKey(
                        name: "FK_phong_chieu_rap_phim_ma_rap",
                        column: x => x.ma_rap,
                        principalTable: "rap_phim",
                        principalColumn: "ma_rap",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nguoi_dung",
                columns: table => new
                {
                    ma_nguoi_dung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ten_dang_nhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    mat_khau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ho_ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    so_dien_thoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ma_vai_tro = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nguoi_dung", x => x.ma_nguoi_dung);
                    table.ForeignKey(
                        name: "FK_nguoi_dung_vai_tro_ma_vai_tro",
                        column: x => x.ma_vai_tro,
                        principalTable: "vai_tro",
                        principalColumn: "ma_vai_tro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ghe",
                columns: table => new
                {
                    ma_ghe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong = table.Column<int>(type: "int", nullable: false),
                    so_ghe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    loai_ghe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Thường")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ghe", x => x.ma_ghe);
                    table.ForeignKey(
                        name: "FK_ghe_phong_chieu_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "phong_chieu",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lich_chieu",
                columns: table => new
                {
                    ma_lich_chieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phim = table.Column<int>(type: "int", nullable: false),
                    ma_phong = table.Column<int>(type: "int", nullable: false),
                    ngay_chieu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gio_chieu = table.Column<TimeSpan>(type: "time", nullable: false),
                    gia_ve = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ma_ngon_ngu = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_chieu", x => x.ma_lich_chieu);
                    table.ForeignKey(
                        name: "FK_lich_chieu_ngon_ngu_phim_ma_ngon_ngu",
                        column: x => x.ma_ngon_ngu,
                        principalTable: "ngon_ngu_phim",
                        principalColumn: "ma_ngon_ngu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lich_chieu_phim_ma_phim",
                        column: x => x.ma_phim,
                        principalTable: "phim",
                        principalColumn: "ma_phim",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lich_chieu_phong_chieu_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "phong_chieu",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "danh_gia",
                columns: table => new
                {
                    ma_danh_gia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_nguoi_dung = table.Column<int>(type: "int", nullable: false),
                    ma_phim = table.Column<int>(type: "int", nullable: false),
                    diem_so = table.Column<int>(type: "int", nullable: true),
                    binh_luan = table.Column<string>(type: "ntext", nullable: false),
                    ngay_danh_gia = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_danh_gia", x => x.ma_danh_gia);
                    table.ForeignKey(
                        name: "FK_danh_gia_nguoi_dung_ma_nguoi_dung",
                        column: x => x.ma_nguoi_dung,
                        principalTable: "nguoi_dung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_danh_gia_phim_ma_phim",
                        column: x => x.ma_phim,
                        principalTable: "phim",
                        principalColumn: "ma_phim",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_info",
                columns: table => new
                {
                    ma_otp = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ma_xac_thuc = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    thoi_gian_tao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    thoi_gian_het_han = table.Column<DateTime>(type: "datetime2", nullable: false),
                    loai_otp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    da_su_dung = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ma_nguoi_dung = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_info", x => x.ma_otp);
                    table.ForeignKey(
                        name: "FK_otp_info_nguoi_dung_ma_nguoi_dung",
                        column: x => x.ma_nguoi_dung,
                        principalTable: "nguoi_dung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dat_ve",
                columns: table => new
                {
                    ma_dat_ve = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_nguoi_dung = table.Column<int>(type: "int", nullable: false),
                    ma_lich_chieu = table.Column<int>(type: "int", nullable: false),
                    ngay_dat = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    tong_tien = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ma_khuyen_mai = table.Column<int>(type: "int", nullable: true),
                    ghi_chu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dat_ve", x => x.ma_dat_ve);
                    table.ForeignKey(
                        name: "FK_dat_ve_khuyen_mai_ma_khuyen_mai",
                        column: x => x.ma_khuyen_mai,
                        principalTable: "khuyen_mai",
                        principalColumn: "ma_khuyen_mai",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dat_ve_lich_chieu_ma_lich_chieu",
                        column: x => x.ma_lich_chieu,
                        principalTable: "lich_chieu",
                        principalColumn: "ma_lich_chieu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dat_ve_nguoi_dung_ma_nguoi_dung",
                        column: x => x.ma_nguoi_dung,
                        principalTable: "nguoi_dung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dat_ve_ghe",
                columns: table => new
                {
                    ma_dat_ve = table.Column<int>(type: "int", nullable: false),
                    ma_ghe = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dat_ve_ghe", x => new { x.ma_dat_ve, x.ma_ghe });
                    table.ForeignKey(
                        name: "FK_dat_ve_ghe_dat_ve_ma_dat_ve",
                        column: x => x.ma_dat_ve,
                        principalTable: "dat_ve",
                        principalColumn: "ma_dat_ve",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dat_ve_ghe_ghe_ma_ghe",
                        column: x => x.ma_ghe,
                        principalTable: "ghe",
                        principalColumn: "ma_ghe",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "thanh_toan",
                columns: table => new
                {
                    ma_thanh_toan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_dat_ve = table.Column<int>(type: "int", nullable: false),
                    so_tien = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    phuong_thuc_thanh_toan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ma_giao_dich = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ma_giao_dich_ngan_hang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ngay_thanh_toan = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ghi_chu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thanh_toan", x => x.ma_thanh_toan);
                    table.ForeignKey(
                        name: "FK_thanh_toan_dat_ve_ma_dat_ve",
                        column: x => x.ma_dat_ve,
                        principalTable: "dat_ve",
                        principalColumn: "ma_dat_ve",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lich_su_giao_dich",
                columns: table => new
                {
                    ma_giao_dich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_nguoi_dung = table.Column<int>(type: "int", nullable: true),
                    ma_thanh_toan = table.Column<int>(type: "int", nullable: true),
                    loai_giao_dich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    noi_dung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ngay_giao_dich = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_su_giao_dich", x => x.ma_giao_dich);
                    table.ForeignKey(
                        name: "FK_lich_su_giao_dich_nguoi_dung_ma_nguoi_dung",
                        column: x => x.ma_nguoi_dung,
                        principalTable: "nguoi_dung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lich_su_giao_dich_thanh_toan_ma_thanh_toan",
                        column: x => x.ma_thanh_toan,
                        principalTable: "thanh_toan",
                        principalColumn: "ma_thanh_toan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_ma_nguoi_dung",
                table: "danh_gia",
                column: "ma_nguoi_dung");

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_ma_phim",
                table: "danh_gia",
                column: "ma_phim");

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_ma_khuyen_mai",
                table: "dat_ve",
                column: "ma_khuyen_mai");

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_ma_lich_chieu",
                table: "dat_ve",
                column: "ma_lich_chieu");

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_ma_nguoi_dung",
                table: "dat_ve",
                column: "ma_nguoi_dung");

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_ghe_ma_ghe",
                table: "dat_ve_ghe",
                column: "ma_ghe");

            migrationBuilder.CreateIndex(
                name: "IX_ghe_ma_phong",
                table: "ghe",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_lich_chieu_ma_ngon_ngu",
                table: "lich_chieu",
                column: "ma_ngon_ngu");

            migrationBuilder.CreateIndex(
                name: "IX_lich_chieu_ma_phim",
                table: "lich_chieu",
                column: "ma_phim");

            migrationBuilder.CreateIndex(
                name: "IX_lich_chieu_ma_phong",
                table: "lich_chieu",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_giao_dich_ma_nguoi_dung",
                table: "lich_su_giao_dich",
                column: "ma_nguoi_dung");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_giao_dich_ma_thanh_toan",
                table: "lich_su_giao_dich",
                column: "ma_thanh_toan");

            migrationBuilder.CreateIndex(
                name: "IX_ngon_ngu_phim_ma_phim",
                table: "ngon_ngu_phim",
                column: "ma_phim");

            migrationBuilder.CreateIndex(
                name: "IX_nguoi_dung_ma_vai_tro",
                table: "nguoi_dung",
                column: "ma_vai_tro");

            migrationBuilder.CreateIndex(
                name: "IX_otp_info_ma_nguoi_dung",
                table: "otp_info",
                column: "ma_nguoi_dung");

            migrationBuilder.CreateIndex(
                name: "IX_phong_chieu_ma_rap",
                table: "phong_chieu",
                column: "ma_rap");

            migrationBuilder.CreateIndex(
                name: "IX_thanh_toan_ma_dat_ve",
                table: "thanh_toan",
                column: "ma_dat_ve");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "danh_gia");

            migrationBuilder.DropTable(
                name: "dat_ve_ghe");

            migrationBuilder.DropTable(
                name: "lich_su_giao_dich");

            migrationBuilder.DropTable(
                name: "otp_info");

            migrationBuilder.DropTable(
                name: "ghe");

            migrationBuilder.DropTable(
                name: "thanh_toan");

            migrationBuilder.DropTable(
                name: "dat_ve");

            migrationBuilder.DropTable(
                name: "khuyen_mai");

            migrationBuilder.DropTable(
                name: "lich_chieu");

            migrationBuilder.DropTable(
                name: "nguoi_dung");

            migrationBuilder.DropTable(
                name: "ngon_ngu_phim");

            migrationBuilder.DropTable(
                name: "phong_chieu");

            migrationBuilder.DropTable(
                name: "vai_tro");

            migrationBuilder.DropTable(
                name: "phim");

            migrationBuilder.DropTable(
                name: "rap_phim");
        }
    }
}
