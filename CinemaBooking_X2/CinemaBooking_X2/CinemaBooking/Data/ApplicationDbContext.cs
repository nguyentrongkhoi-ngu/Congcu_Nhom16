using CinemaBooking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CinemaBooking.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Phim> Phims { get; set; }
        public DbSet<NgonNguPhim> NgonNguPhims { get; set; }
        public DbSet<LichChieu> LichChieus { get; set; }
        public DbSet<PhongChieu> PhongChieus { get; set; }
        public DbSet<RapPhim> RapPhims { get; set; }
        public DbSet<Ghe> Ghes { get; set; }
        public DbSet<DatVe> DatVes { get; set; }
        public DbSet<DatVeGhe> DatVeGhes { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<DatVeCombo> DatVeCombos { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }
        public DbSet<LichSuGiaoDich> LichSuGiaoDiches { get; set; }
        public DbSet<OtpInfo> OtpInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables to use custom table names
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers");
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.SoDienThoai).HasMaxLength(15);
                entity.Property(e => e.NgayTao);
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("AspNetRoles");
                // MoTa property configuration will be added in future migration
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("AspNetUserRoles");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("AspNetUserClaims");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("AspNetUserLogins");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("AspNetRoleClaims");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("AspNetUserTokens");
            });

            // Mapping cho VaiTro
            modelBuilder.Entity<VaiTro>(entity =>
            {
                entity.ToTable("vai_tro");
                entity.HasKey(e => e.MaVaiTro);
                entity.Property(e => e.MaVaiTro).HasColumnName("ma_vai_tro").ValueGeneratedOnAdd();
                entity.Property(e => e.TenVaiTro).HasColumnName("ten_vai_tro").HasMaxLength(50).IsRequired();
                entity.Property(e => e.MoTa).HasColumnName("mo_ta").HasMaxLength(255);
            });

            // Mapping cho NguoiDung
            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.ToTable("nguoi_dung");
                entity.HasKey(e => e.MaNguoiDung);
                entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung").ValueGeneratedOnAdd();
                entity.Property(e => e.TenDangNhap).HasColumnName("ten_dang_nhap").HasMaxLength(50).IsRequired();
                entity.Property(e => e.MatKhau).HasColumnName("mat_khau").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
                entity.Property(e => e.HoTen).HasColumnName("ho_ten").HasMaxLength(100);
                entity.Property(e => e.SoDienThoai).HasColumnName("so_dien_thoai").HasMaxLength(15);
                entity.Property(e => e.NgayTao).HasColumnName("ngay_tao").HasDefaultValueSql("getdate()");
                entity.Property(e => e.MaVaiTro).HasColumnName("ma_vai_tro");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(255);
                entity.Property(e => e.DiemTichLuy).HasColumnName("diem_tich_luy").HasDefaultValue(0);

                // Thiết lập quan hệ với VaiTro
                entity.HasOne(e => e.VaiTro)
                    .WithMany(v => v.NguoiDungs)
                    .HasForeignKey(e => e.MaVaiTro)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mapping cho OtpInfo
            modelBuilder.Entity<OtpInfo>(entity =>
            {
                entity.ToTable("otp_info");
                entity.HasKey(e => e.MaOtp);
                entity.Property(e => e.MaOtp).HasColumnName("ma_otp").ValueGeneratedOnAdd();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
                entity.Property(e => e.MaXacThuc).HasColumnName("ma_xac_thuc").HasMaxLength(6).IsRequired();
                entity.Property(e => e.ThoiGianTao).HasColumnName("thoi_gian_tao").IsRequired();
                entity.Property(e => e.ThoiGianHetHan).HasColumnName("thoi_gian_het_han").IsRequired();
                entity.Property(e => e.LoaiOtp).HasColumnName("loai_otp").HasMaxLength(20).IsRequired();
                entity.Property(e => e.DaSuDung).HasColumnName("da_su_dung").HasDefaultValue(false);
                entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung");
                entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450);

                // Cấu hình quan hệ với NguoiDung
                entity.HasOne(e => e.NguoiDung)
                    .WithMany(n => n.OtpInfos)
                    .HasForeignKey(e => e.MaNguoiDung)
                    .OnDelete(DeleteBehavior.Cascade);

                // Cấu hình quan hệ với ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithMany(u => u.OtpInfos)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Mapping cho Phim
            modelBuilder.Entity<Phim>(entity =>
            {
                entity.ToTable("phim");
                entity.HasKey(e => e.MaPhim);
                entity.Property(e => e.TenPhim).HasColumnName("ten_phim").HasMaxLength(100).IsRequired();
                entity.Property(e => e.MoTa).HasColumnName("mo_ta").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ThoiLuong).HasColumnName("thoi_luong").IsRequired();
                entity.Property(e => e.TheLoai).HasColumnName("the_loai").HasMaxLength(50);
                entity.Property(e => e.NgayPhatHanh).HasColumnName("ngay_phat_hanh");
                entity.Property(e => e.UrlPoster).HasColumnName("url_poster").HasMaxLength(255);
                entity.Property(e => e.DinhDang).HasColumnName("dinh_dang").HasMaxLength(20);
                entity.Property(e => e.Trailer).HasColumnName("trailer").HasMaxLength(255);
                entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(50);
            });

            // Mapping cho NgonNguPhim
            modelBuilder.Entity<NgonNguPhim>(entity =>
            {
                entity.ToTable("ngon_ngu_phim");
                entity.HasKey(e => e.MaNgonNgu);
                entity.Property(e => e.MaNgonNgu).HasColumnName("ma_ngon_ngu").ValueGeneratedOnAdd();
                entity.Property(e => e.NgonNgu).HasColumnName("ngon_ngu").HasMaxLength(50);
                entity.Property(e => e.PhuDe).HasColumnName("phu_de").HasMaxLength(50);
                entity.Property(e => e.MaPhim).HasColumnName("ma_phim");

                // Quan hệ với Phim
                entity.HasOne(e => e.Phim)
                    .WithMany(p => p.NgonNguPhims)
                    .HasForeignKey(e => e.MaPhim)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapping cho RapPhim
            modelBuilder.Entity<RapPhim>(entity =>
            {
                entity.ToTable("rap_phim");
                entity.HasKey(e => e.MaRap);
                entity.Property(e => e.MaRap).HasColumnName("ma_rap").ValueGeneratedOnAdd();
                entity.Property(e => e.TenRap).HasColumnName("ten_rap").HasMaxLength(100);
                entity.Property(e => e.DiaChi).HasColumnName("dia_chi").HasMaxLength(200);
                entity.Property(e => e.ThanhPho).HasColumnName("thanh_pho").HasMaxLength(50);
            });

            // Mapping cho PhongChieu
            modelBuilder.Entity<PhongChieu>(entity =>
            {
                entity.ToTable("phong_chieu");
                entity.HasKey(e => e.MaPhong);
                entity.Property(e => e.MaPhong).HasColumnName("ma_phong").ValueGeneratedOnAdd();
                entity.Property(e => e.MaRap).HasColumnName("ma_rap").IsRequired();
                entity.Property(e => e.SoPhong).HasColumnName("so_phong").IsRequired();
                entity.Property(e => e.SucChua).HasColumnName("suc_chua").IsRequired();

                // Quan hệ với RapPhim
                entity.HasOne(e => e.RapPhim)
                    .WithMany(r => r.PhongChieus)
                    .HasForeignKey(e => e.MaRap)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapping cho Ghe
            modelBuilder.Entity<Ghe>(entity =>
            {
                entity.ToTable("ghe");
                entity.HasKey(e => e.MaGhe);
                entity.Property(e => e.MaGhe).HasColumnName("ma_ghe").ValueGeneratedOnAdd();
                entity.Property(e => e.MaPhong).HasColumnName("ma_phong").IsRequired();
                entity.Property(e => e.SoGhe).HasColumnName("so_ghe").HasMaxLength(10).IsRequired();
                entity.Property(e => e.LoaiGhe).HasColumnName("loai_ghe").HasMaxLength(20).IsRequired().HasDefaultValue("Thường");

                // Quan hệ với PhongChieu
                entity.HasOne(e => e.PhongChieu)
                    .WithMany(p => p.Ghes)
                    .HasForeignKey(e => e.MaPhong)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapping cho LichChieu
            modelBuilder.Entity<LichChieu>(entity =>
            {
                entity.ToTable("lich_chieu");
                entity.HasKey(e => e.MaLichChieu);
                entity.Property(e => e.MaLichChieu).HasColumnName("ma_lich_chieu").ValueGeneratedOnAdd();
                entity.Property(e => e.MaPhim).HasColumnName("ma_phim").IsRequired();
                entity.Property(e => e.MaPhong).HasColumnName("ma_phong").IsRequired();
                entity.Property(e => e.NgayChieu).HasColumnName("ngay_chieu").IsRequired();
                entity.Property(e => e.GioChieu).HasColumnName("gio_chieu").IsRequired();
                entity.Property(e => e.GiaVe).HasColumnName("gia_ve").HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(e => e.MaNgonNgu).HasColumnName("ma_ngon_ngu");

                // Quan hệ với Phim
                entity.HasOne(e => e.Phim)
                    .WithMany(p => p.LichChieus)
                    .HasForeignKey(e => e.MaPhim)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với PhongChieu
                entity.HasOne(e => e.PhongChieu)
                    .WithMany(p => p.LichChieus)
                    .HasForeignKey(e => e.MaPhong)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với NgonNguPhim
                entity.HasOne(e => e.NgonNguPhim)
                    .WithMany(n => n.LichChieus)
                    .HasForeignKey(e => e.MaNgonNgu)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mapping cho KhuyenMai
            modelBuilder.Entity<KhuyenMai>(entity =>
            {
                entity.ToTable("khuyen_mai");
                entity.HasKey(e => e.MaKhuyenMai);
                entity.Property(e => e.MaKhuyenMai).HasColumnName("ma_khuyen_mai").ValueGeneratedOnAdd();
                entity.Property(e => e.MaCode).HasColumnName("ma_code").HasMaxLength(20).IsRequired();
                entity.Property(e => e.PhanTramGiam).HasColumnName("phan_tram_giam").IsRequired();
                entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau").IsRequired();
                entity.Property(e => e.NgayKetThuc).HasColumnName("ngay_ket_thuc").IsRequired();
                entity.Property(e => e.GiaTriToiThieu).HasColumnName("gia_tri_toi_thieu").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.MoTa).HasColumnName("mo_ta").HasMaxLength(255);
            });

            // Mapping cho DatVe
            modelBuilder.Entity<DatVe>(entity =>
            {
                entity.ToTable("dat_ve");
                entity.HasKey(e => e.MaDatVe);
                entity.Property(e => e.MaDatVe).HasColumnName("ma_dat_ve").ValueGeneratedOnAdd();
                entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450);
                entity.Property(e => e.MaLichChieu).HasColumnName("ma_lich_chieu").IsRequired();
                entity.Property(e => e.NgayDat).HasColumnName("ngay_dat").HasDefaultValueSql("getdate()");
                entity.Property(e => e.TongTien).HasColumnName("tong_tien").HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(50);
                entity.Property(e => e.MaKhuyenMai).HasColumnName("ma_khuyen_mai");
                entity.Property(e => e.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);

                // Quan hệ với NguoiDung
                entity.HasOne(e => e.NguoiDung)
                    .WithMany(n => n.DatVes)
                    .HasForeignKey(e => e.MaNguoiDung)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithMany(u => u.DatVes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ với LichChieu
                entity.HasOne(e => e.LichChieu)
                    .WithMany(l => l.DatVes)
                    .HasForeignKey(e => e.MaLichChieu)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với KhuyenMai
                entity.HasOne(e => e.KhuyenMai)
                    .WithMany(k => k.DatVes)
                    .HasForeignKey(e => e.MaKhuyenMai)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Mapping cho DatVeGhe
            modelBuilder.Entity<DatVeGhe>(entity =>
            {
                entity.ToTable("dat_ve_ghe");
                entity.HasKey(e => new { e.MaDatVe, e.MaGhe });
                entity.Property(e => e.MaDatVe).HasColumnName("ma_dat_ve");
                entity.Property(e => e.MaGhe).HasColumnName("ma_ghe");

                // Quan hệ với DatVe
                entity.HasOne(e => e.DatVe)
                    .WithMany(d => d.DatVeGhes)
                    .HasForeignKey(e => e.MaDatVe)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với Ghe - sử dụng Restrict để tránh multiple cascade paths
                entity.HasOne(e => e.Ghe)
                    .WithMany(g => g.DatVeGhes)
                    .HasForeignKey(e => e.MaGhe)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mapping cho ThanhToan
            modelBuilder.Entity<ThanhToan>(entity =>
            {
                entity.ToTable("thanh_toan");
                entity.HasKey(e => e.MaThanhToan);
                entity.Property(e => e.MaThanhToan).HasColumnName("ma_thanh_toan").ValueGeneratedOnAdd();
                entity.Property(e => e.MaDatVe).HasColumnName("ma_dat_ve").IsRequired();
                entity.Property(e => e.SoTien).HasColumnName("so_tien").HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(e => e.PhuongThucThanhToan).HasColumnName("phuong_thuc_thanh_toan").HasMaxLength(50).IsRequired();
                entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(50);
                entity.Property(e => e.NgayThanhToan).HasColumnName("ngay_thanh_toan").HasDefaultValueSql("getdate()");
                entity.Property(e => e.MaGiaoDich).HasColumnName("ma_giao_dich").HasMaxLength(100);
                entity.Property(e => e.MaGiaoDichNganHang).HasColumnName("ma_giao_dich_ngan_hang").HasMaxLength(100);
                entity.Property(e => e.GhiChu).HasColumnName("ghi_chu").HasMaxLength(500);

                // Quan hệ với DatVe
                entity.HasOne(e => e.DatVe)
                    .WithMany(d => d.ThanhToans)
                    .HasForeignKey(e => e.MaDatVe)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapping cho Combo
            modelBuilder.Entity<Combo>(entity =>
            {
                entity.ToTable("combo");
                entity.HasKey(e => e.MaCombo);
                entity.Property(e => e.MaCombo).HasColumnName("ma_combo").ValueGeneratedOnAdd();
                entity.Property(e => e.TenCombo).HasColumnName("ten_combo").HasMaxLength(100).IsRequired();
                entity.Property(e => e.MoTa).HasColumnName("mo_ta");
                entity.Property(e => e.Gia).HasColumnName("gia").HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(e => e.HinhAnh).HasColumnName("hinh_anh").HasMaxLength(255);
                entity.Property(e => e.Loai).HasColumnName("loai").HasMaxLength(50);
                entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasDefaultValue(true);
                entity.Property(e => e.SoLuongTon).HasColumnName("so_luong_ton").HasDefaultValue(999);
                entity.Property(e => e.UuTien).HasColumnName("uu_tien").HasDefaultValue(0);
                entity.Property(e => e.Nhan).HasColumnName("nhan").HasMaxLength(50);
            });

            // Mapping cho DatVeCombo
            modelBuilder.Entity<DatVeCombo>(entity =>
            {
                entity.ToTable("dat_ve_combo");
                entity.HasKey(e => new { e.MaDatVe, e.MaCombo });
                entity.Property(e => e.MaDatVe).HasColumnName("ma_dat_ve");
                entity.Property(e => e.MaCombo).HasColumnName("ma_combo");
                entity.Property(e => e.SoLuong).HasColumnName("so_luong").IsRequired();

                // Quan hệ với DatVe
                entity.HasOne(e => e.DatVe)
                    .WithMany(d => d.DatVeCombos)
                    .HasForeignKey(e => e.MaDatVe)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với Combo
                entity.HasOne(e => e.Combo)
                    .WithMany(c => c.DatVeCombos)
                    .HasForeignKey(e => e.MaCombo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mapping cho DanhGia
            modelBuilder.Entity<DanhGia>(entity =>
            {
                entity.ToTable("danh_gia");
                entity.HasKey(e => e.MaDanhGia);
                entity.Property(e => e.MaDanhGia).HasColumnName("ma_danh_gia").ValueGeneratedOnAdd();
                entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450);
                entity.Property(e => e.MaPhim).HasColumnName("ma_phim").IsRequired();
                entity.Property(e => e.DiemSo).HasColumnName("diem_so");
                entity.Property(e => e.NgayDanhGia).HasColumnName("ngay_danh_gia").HasDefaultValueSql("getdate()");
                entity.Property(e => e.BinhLuan).HasColumnName("binh_luan").HasColumnType("ntext");

                // Quan hệ với NguoiDung
                entity.HasOne(e => e.NguoiDung)
                    .WithMany(n => n.DanhGias)
                    .HasForeignKey(e => e.MaNguoiDung)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithMany(u => u.DanhGias)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ với Phim
                entity.HasOne(e => e.Phim)
                    .WithMany(p => p.DanhGias)
                    .HasForeignKey(e => e.MaPhim)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapping cho LichSuGiaoDich
            modelBuilder.Entity<LichSuGiaoDich>(entity =>
            {
                entity.ToTable("lich_su_giao_dich");
                entity.HasKey(e => e.MaGiaoDich);
                entity.Property(e => e.MaGiaoDich).HasColumnName("ma_giao_dich").ValueGeneratedOnAdd();
                entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung");
                entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(450);
                entity.Property(e => e.LoaiGiaoDich).HasColumnName("loai_giao_dich").HasMaxLength(50).IsRequired();
                entity.Property(e => e.NoiDung).HasColumnName("noi_dung").HasMaxLength(255);
                entity.Property(e => e.NgayGiaoDich).HasColumnName("ngay_giao_dich").HasDefaultValueSql("getdate()");
                entity.Property(e => e.MaThanhToan).HasColumnName("ma_thanh_toan");
                entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(50);

                // Quan hệ với NguoiDung
                entity.HasOne(e => e.NguoiDung)
                    .WithMany(n => n.LichSuGiaoDichs)
                    .HasForeignKey(e => e.MaNguoiDung)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ với ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithMany(u => u.LichSuGiaoDichs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ với ThanhToan - sử dụng Restrict để tránh multiple cascade paths
                entity.HasOne(e => e.ThanhToan)
                    .WithMany()
                    .HasForeignKey(e => e.MaThanhToan)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}