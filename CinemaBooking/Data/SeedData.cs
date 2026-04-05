using CinemaBooking.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CinemaBooking.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context)
        {
            Console.WriteLine("--- Starting SeedData.Initialize ---");

            // Đảm bảo Database đã được cập nhật Migration mới nhất
            await context.Database.MigrateAsync();

            // 1. Seed Vai Trò (Roles)
            if (!context.VaiTros.Any())
            {
                var vaiTros = new List<VaiTro>
                {
                    new VaiTro { TenVaiTro = "Admin", MoTa = "Quản trị viên hệ thống" },
                    new VaiTro { TenVaiTro = "User", MoTa = "Người dùng thông thường" }
                };
                context.VaiTros.AddRange(vaiTros);
                await context.SaveChangesAsync();
                Console.WriteLine("Seeded Roles.");
            }

            // 2. Seed Admin User
            if (!context.NguoiDungs.Any(u => u.TenDangNhap == "admin"))
            {
                var adminRole = await context.VaiTros.FirstOrDefaultAsync(r => r.TenVaiTro == "Admin");
                var adminUser = new NguoiDung
                {
                    TenDangNhap = "admin",
                    MatKhau = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Email = "admin@cinezore.com",
                    HoTen = "Quản trị viên",
                    SoDienThoai = "0123456789",
                    NgayTao = DateTime.Now,
                    MaVaiTro = adminRole?.MaVaiTro ?? 1
                };
                context.NguoiDungs.Add(adminUser);
                await context.SaveChangesAsync();
                Console.WriteLine("Seeded Admin User.");
            }

            // 3. Seed Rạp Phim, Phòng và Ghế
            if (!context.RapPhims.Any())
            {
                var rapPhims = new List<RapPhim>
                {
                    new RapPhim { TenRap = "CineZore Hà Nội", DiaChi = "123 Đường ABC, Quận 1", ThanhPho = "Hà Nội" },
                    new RapPhim { TenRap = "CineZore TP.HCM", DiaChi = "456 Đường XYZ, Quận 3", ThanhPho = "TP.HCM" },
                    new RapPhim { TenRap = "CineZore Đà Nẵng", DiaChi = "789 Đường DEF, Quận Hải Châu", ThanhPho = "Đà Nẵng" }
                };
                context.RapPhims.AddRange(rapPhims);
                await context.SaveChangesAsync();

                await SeedRoomsAndSeats(context);
                Console.WriteLine("Seeded Theaters, Rooms, and Seats.");
            }

            // 4. Seed Phim và Ngôn ngữ
            await SeedMovies(context);

            // 5. Seed Khuyến Mãi
            if (!context.KhuyenMais.Any())
            {
                context.KhuyenMais.AddRange(
                    new KhuyenMai { MaCode = "WELCOME10", PhanTramGiam = 10, NgayBatDau = DateTime.Now.AddDays(-30), NgayKetThuc = DateTime.Now.AddDays(30), MoTa = "Giảm 10% cho khách hàng mới" },
                    new KhuyenMai { MaCode = "SUMMER20", PhanTramGiam = 20, NgayBatDau = DateTime.Now.AddDays(-15), NgayKetThuc = DateTime.Now.AddDays(45), MoTa = "Giảm 20% mùa hè" }
                );
                await context.SaveChangesAsync();
                Console.WriteLine("Seeded Promotions.");
            }

            // 6. Seed Lịch Chiếu
            await SeedShowtimes(context);

            // 7. Seed Combos (F&B)
            try
            {
                await SeedCombos(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding combos: {ex.Message}");
            }

            Console.WriteLine("--- SeedData completed successfully ---");
        }

        private static async Task SeedRoomsAndSeats(ApplicationDbContext context)
        {
            var raps = await context.RapPhims.ToListAsync();
            foreach (var rap in raps)
            {
                var phong1 = new PhongChieu { MaRap = rap.MaRap, SoPhong = "1", SucChua = 100 };
                var phong2 = new PhongChieu { MaRap = rap.MaRap, SoPhong = "2", SucChua = 120 };
                context.PhongChieus.AddRange(phong1, phong2);
            }
            await context.SaveChangesAsync();

            var phongs = await context.PhongChieus.ToListAsync();
            foreach (var phong in phongs)
            {
                int soHang = phong.SucChua / 10;
                for (int h = 0; h < soHang; h++)
                {
                    char tenHang = (char)('A' + h);
                    for (int g = 1; g <= 10; g++)
                    {
                        context.Ghes.Add(new Ghe {
                            MaPhong = phong.MaPhong,
                            SoGhe = $"{tenHang}{g}",
                            LoaiGhe = (g >= 4 && g <= 7) ? "VIP" : "Thường"
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedMovies(ApplicationDbContext context)
        {
            if (await context.Phims.AnyAsync()) return;

            var movies = new List<Phim>
            {
                new Phim { TenPhim = "Avengers: Endgame", ThoiLuong = 181, TheLoai = "Hành động, Sci-Fi", NgayPhatHanh = new DateTime(2019, 4, 26), UrlPoster = "https://image.tmdb.org/t/p/original/or06vSfv0uY7o98ToolkitW0v3K7.jpg", DiemIMDb = 8.4, DinhDang = "2D, 3D, IMAX", Trailer = "https://www.youtube.com/watch?v=TcMBFSGVi1c" },
                new Phim { TenPhim = "Inside Out 2", ThoiLuong = 96, TheLoai = "Hoạt hình, Hài", NgayPhatHanh = new DateTime(2024, 6, 14), UrlPoster = "https://image.tmdb.org/t/p/original/vpn9sy7kR40O1ZJ3A6Gv09yH6Vj.jpg", DiemIMDb = 7.6, DinhDang = "2D, 3D", Trailer = "https://www.youtube.com/watch?v=L4DrolmDxmw" },
                new Phim { TenPhim = "Deadpool & Wolverine", ThoiLuong = 127, TheLoai = "Hành động, Hài", NgayPhatHanh = new DateTime(2024, 7, 26), UrlPoster = "https://image.tmdb.org/t/p/original/8cd96f2pUAp9GmBy5C2OSWSJhNm.jpg", DiemIMDb = 7.7, DinhDang = "2D, IMAX", Trailer = "https://www.youtube.com/watch?v=73_1biulkYk" },
                new Phim { TenPhim = "Robot Hoang Dã", ThoiLuong = 102, TheLoai = "Hoạt hình, Phiêu lưu", NgayPhatHanh = new DateTime(2024, 9, 27), UrlPoster = "https://image.tmdb.org/t/p/original/hr7I1tLIs090dK47K0P70wInS0G.jpg", DiemIMDb = 8.3, DinhDang = "2D", Trailer = "https://www.youtube.com/watch?v=67vbA5ZJdUs" },
                new Phim { TenPhim = "Spider-Man: No Way Home", ThoiLuong = 148, TheLoai = "Hành động", NgayPhatHanh = new DateTime(2021, 12, 17), UrlPoster = "https://image.tmdb.org/t/p/original/1g0dhvRzfwvqp1Z6BLpvmUfAdpI.jpg", DiemIMDb = 8.2, DinhDang = "2D, 3D", Trailer = "https://www.youtube.com/watch?v=JfVOs4VSpmA" }
            };

            context.Phims.AddRange(movies);
            await context.SaveChangesAsync();

            var allPhims = await context.Phims.ToListAsync();
            foreach (var p in allPhims)
            {
                context.NgonNguPhims.Add(new NgonNguPhim { MaPhim = p.MaPhim, NgonNgu = "Tiếng Anh", PhuDe = "Tiếng Việt" });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedShowtimes(ApplicationDbContext context)
        {
            if (await context.LichChieus.AnyAsync()) return;

            var phims = await context.Phims.ToListAsync();
            var phongs = await context.PhongChieus.ToListAsync();
            var ngonNgu = await context.NgonNguPhims.ToListAsync();

            foreach (var phim in phims.Take(3)) // Seed cho 3 phim đầu
            {
                foreach (var phong in phongs.Take(2)) // Mỗi phim ở 2 phòng
                {
                    for (int d = 0; d < 2; d++) // Trong 2 ngày tới
                    {
                        context.LichChieus.Add(new LichChieu {
                            MaPhim = phim.MaPhim,
                            MaPhong = phong.MaPhong,
                            NgayChieu = DateTime.Today.AddDays(d),
                            GioChieu = new TimeSpan(19, 30, 0),
                            GiaVe = 95000,
                            MaNgonNgu = ngonNgu.FirstOrDefault(n => n.MaPhim == phim.MaPhim)?.MaNgonNgu
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedCombos(ApplicationDbContext context)
        {
            // Kiểm tra nếu đã có đơn hàng thì không xóa để tránh lỗi ràng buộc
            if (context.Combos.Any())
            {
                if (await context.DatVeCombos.AnyAsync()) return;
                context.Combos.RemoveRange(context.Combos);
                await context.SaveChangesAsync();
            }

            var combos = new List<Combo>
            {
                new Combo { TenCombo = "Solo Combo", MoTa = "1 Bắp + 1 Nước ngọt vừa. Tiết kiệm 15%.", Gia = 75000, Loai = "Combo", KichThuoc = "Standard", HinhAnh = "https://images.unsplash.com/photo-1572177191856-3cde618dee1f?w=800&q=80" },
                new Combo { TenCombo = "Couple Combo", MoTa = "1 Bắp lớn + 2 Nước ngọt lớn.", Gia = 115000, Loai = "Combo", KichThuoc = "L", HinhAnh = "https://images.unsplash.com/photo-1594465909740-9821d81995ba?w=800&q=80" },
                new Combo { TenCombo = "Bắp Phô Mai (Lớn)", MoTa = "Bắp rang phủ bột phô mai béo ngậy.", Gia = 75000, Loai = "Bắp", KichThuoc = "L", HinhAnh = "https://images.unsplash.com/photo-1585647347384-2593bc3571d4?w=800&q=80" },
                new Combo { TenCombo = "Cocacola (Lớn)", MoTa = "Thức uống giải khát mát lạnh.", Gia = 35000, Loai = "Nước", KichThuoc = "L", HinhAnh = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=800&q=80" }
            };

            context.Combos.AddRange(combos);
            await context.SaveChangesAsync();
        }
    }
}