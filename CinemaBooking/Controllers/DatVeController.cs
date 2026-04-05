using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;
using CinemaBooking.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CinemaBooking.Data;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "User")]
    public class DatVeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatVeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách lịch chiếu của phim
        public async Task<IActionResult> Index(int maPhim, int? maRap = null, string ngayChieu = null)
        {
            var phim = await _context.Phims
                .Include(p => p.LichChieus)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim);

            if (phim == null)
            {
                return NotFound();
            }

            // Lấy thời gian hiện tại
            var now = DateTime.Now;

            // Lấy các lịch chiếu từ thời điểm hiện tại trở đi (loại bỏ các lịch chiếu đã kết thúc)
            IEnumerable<LichChieu> lichChieusQuery = phim.LichChieus
                .Where(l => l.NgayChieu.Date > now.Date ||
                      (l.NgayChieu.Date == now.Date &&
                       l.GioChieu > now.TimeOfDay));

            if (maRap.HasValue)
            {
                lichChieusQuery = lichChieusQuery.Where(l => l.PhongChieu.MaRap == maRap.Value);
            }

            var lichChieus = lichChieusQuery
                .OrderBy(l => l.NgayChieu)
                .ThenBy(l => l.GioChieu)
                .ToList();

            // Kiểm tra nếu không có lịch chiếu nào
            if (lichChieus.Count == 0)
            {
                TempData["InfoMessage"] = "Hiện tại chưa có lịch chiếu nào cho phim này" + (maRap.HasValue ? " tại rạp đã chọn." : ".");
            }

            ViewBag.Phim = phim;
            ViewBag.SelectedDate = ngayChieu;
            return View(lichChieus);
        }

        // Hiển thị form đặt vé
        public async Task<IActionResult> Create(int maLichChieu)
        {
            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra xem lịch chiếu đã kết thúc chưa
            var now = DateTime.Now;
            if (lichChieu.NgayChieu.Date < now.Date ||
                (lichChieu.NgayChieu.Date == now.Date && lichChieu.GioChieu < now.TimeOfDay))
            {
                TempData["ErrorMessage"] = "Lịch chiếu này đã kết thúc, không thể đặt vé";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách ghế đã đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync() ?? new List<string>();

            ViewBag.GheDaDat = gheDaDat;
            return View(lichChieu);
        }

        // Hiển thị giao diện chọn ghế
        public async Task<IActionResult> ChonGhe(int maLichChieu)
        {
            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra xem lịch chiếu đã kết thúc chưa
            var now = DateTime.Now;
            if (lichChieu.NgayChieu.Date < now.Date ||
                (lichChieu.NgayChieu.Date == now.Date && lichChieu.GioChieu < now.TimeOfDay))
            {
                TempData["ErrorMessage"] = "Lịch chiếu này đã kết thúc, không thể đặt vé";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách ghế đã đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync() ?? new List<string>();

            // Tính toán số hàng và số cột dựa vào sức chứa của phòng
            var sucChua = lichChieu.PhongChieu.SucChua;
            int soCot = 0;
            char[] hangGhe = Array.Empty<char>();

            // Tính toán số cột dựa trên sức chứa
            if (sucChua <= 100)
            {
                soCot = 10; // Phòng nhỏ: 10 cột
            }
            else if (sucChua <= 150)
            {
                soCot = 15; // Phòng trung bình: 15 cột
            }
            else
            {
                soCot = 20; // Phòng lớn: 20 cột
            }

            // Tính số hàng cần thiết để đạt được sức chứa
            int soHang = (int)Math.Ceiling((double)sucChua / soCot);

            // Tạo mảng các ký tự hàng từ A đến Z
            hangGhe = Enumerable.Range(0, soHang)
                .Select(i => (char)('A' + i))
                .ToArray();

            ViewBag.GheDaDat = gheDaDat;
            ViewBag.HangGhe = hangGhe;
            ViewBag.SoCot = soCot;

            // Tính toán xếp hạng và giảm giá thành viên
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            string userRank = "Đồng";
            decimal discountPercent = 0;
            decimal totalSpent = 0;

            if (currentUserId.HasValue)
            {
                totalSpent = await _context.DatVes
                    .Where(d => d.MaNguoiDung == currentUserId.Value && d.TrangThai == "Đã thanh toán")
                    .SumAsync(d => d.TongTien);

                if (totalSpent >= 10000000) { userRank = "Kim Cương"; discountPercent = 15; }
                else if (totalSpent >= 5000000) { userRank = "Vàng"; discountPercent = 10; }
                else if (totalSpent >= 1000000) { userRank = "Bạc"; discountPercent = 5; }
            }

            ViewBag.UserRank = userRank;
            ViewBag.DiscountPercent = discountPercent;
            ViewBag.TotalSpent = totalSpent;

            return View(lichChieu);
        }

        // Chọn Combo (F&B)
        public async Task<IActionResult> ChonCombo(int maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(selectedSeats))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ghế trước khi chọn Combo";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null) return NotFound();

            var combos = await _context.Combos
                .Where(c => c.TrangThai == true)
                .OrderByDescending(c => c.UuTien)
                .ThenBy(c => c.Gia)
                .ToListAsync();

            ViewBag.SelectedSeats = selectedSeats;
            ViewBag.Combos = combos;
            
            // Tính tiền ghế sơ bộ để hiển thị real-time subtotal
            var selectedSeatsList = selectedSeats.Split(',');
            decimal seatTotal = 0;
            foreach (var soGhe in selectedSeatsList)
            {
                var row = soGhe[0];
                var col = int.Parse(soGhe.Substring(1));
                decimal giaGhe = lichChieu.GiaVe;
                
                // VIP/Sweetbox logic (matching ChonGhe)
                // Note: Simplified logic here, should use same as ChonGhe
                int soHang = (int)Math.Ceiling((double)lichChieu.PhongChieu.SucChua / 10); // Simplified
                if (row >= 'J') giaGhe = lichChieu.GiaVe * 1.2m;
                if (row == 'P' && col <= 4) giaGhe = lichChieu.GiaVe * 1.5m;
                
                seatTotal += giaGhe;
            }

            // Membership discount calculation
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            decimal discountPercent = 0;
            if (currentUserId.HasValue)
            {
                var totalSpent = await _context.DatVes
                    .Where(d => d.MaNguoiDung == currentUserId.Value && d.TrangThai == "Đã thanh toán")
                    .SumAsync(d => d.TongTien);

                if (totalSpent >= 10000000) discountPercent = 15;
                else if (totalSpent >= 5000000) discountPercent = 10;
                else if (totalSpent >= 1000000) discountPercent = 5;
            }

            ViewBag.SeatTotal = seatTotal;
            ViewBag.DiscountPercent = discountPercent;

            return View(lichChieu);
        }

        // API kiểm tra mã khuyến mãi
        [HttpPost]
        public async Task<IActionResult> KiemTraKhuyenMai(string maKhuyenMai, decimal tongTien = 0)
        {
            if (string.IsNullOrEmpty(maKhuyenMai))
            {
                return Json(new { isValid = false, message = "Vui lòng nhập mã." });
            }

            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (!currentUserId.HasValue)
            {
                return Json(new { isValid = false, message = "Vui lòng đăng nhập." });
            }

            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai);

            if (khuyenMai == null)
            {
                return Json(new { isValid = false, message = "Mã giảm giá không tồn tại." });
            }

            // 1. Kiểm tra ngày hiệu lực
            var now = DateTime.Now;
            if (now < khuyenMai.NgayBatDau || now > khuyenMai.NgayKetThuc)
            {
                return Json(new { isValid = false, message = "Mã giảm giá đã hết hạn hoặc chưa đến thời gian áp dụng." });
            }

            // 2. Kiểm tra giá trị đơn hàng tối thiểu
            if (tongTien < khuyenMai.GiaTriToiThieu)
            {
                return Json(new { 
                    isValid = false, 
                    message = $"Đơn hàng tối thiểu để áp dụng mã này là {khuyenMai.GiaTriToiThieu:N0}đ." 
                });
            }

            // 3. Kiểm tra xem user đã sử dụng mã này chưa (Mỗi user chỉ dùng 1 lần)
            var isPromoUsedByCustomer = await _context.DatVes
                .AnyAsync(d => d.MaNguoiDung == currentUserId.Value && 
                               d.MaKhuyenMai == khuyenMai.MaKhuyenMai && 
                               d.TrangThai != "Đã hủy");

            if (isPromoUsedByCustomer)
            {
                return Json(new { isValid = false, message = "Bạn đã sử dụng mã giảm giá này cho một đơn hàng khác rồi." });
            }

            return Json(new {
                isValid = true,
                phanTramGiam = khuyenMai.PhanTramGiam,
                maKhuyenMai = khuyenMai.MaKhuyenMai,
                message = $"Áp dụng thành công! Giảm {khuyenMai.PhanTramGiam}%"
            });
        }

        // Xử lý đặt vé từ giao diện chọn ghế
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuDatVe(int maLichChieu, string selectedSeats, string maKhuyenMai = null, string selectedCombos = null)
        {
            if (string.IsNullOrEmpty(selectedSeats))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ghế";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            var selectedSeatsList = selectedSeats.Split(',');
            if (selectedSeatsList.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ghế";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.Ghes)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra ghế đã được đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync();

            var conflictedSeats = selectedSeatsList.Where(s => gheDaDat.Contains(s)).ToList();
            if (conflictedSeats.Any())
            {
                TempData["ErrorMessage"] = $"Ghế {string.Join(", ", conflictedSeats)} đã được đặt. Vui lòng chọn ghế khác";
                return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
            }

            // Tính tổng tiền dựa trên loại ghế
            decimal tongTien = 0;
            foreach (var soGhe in selectedSeatsList)
            {
                // Xác định loại ghế và giá tương ứng
                var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                var col = int.Parse(soGhe.Substring(1)); // Số ghế

                decimal giaGhe = lichChieu.GiaVe;

                // VIP: Hàng J-P, cột 1-3
                if (row >= 'J' && col >= 1 && col <= 3)
                {
                    giaGhe = lichChieu.GiaVe * 1.2m; // Ghế VIP giá cao hơn 20%
                }
                // Sweetbox: Hàng P, cột 1-4
                else if (row == 'P' && col >= 1 && col <= 4)
                {
                    giaGhe = lichChieu.GiaVe * 1.5m; // Sweetbox giá cao hơn 50%
                }

                tongTien += giaGhe;
            }

            // Xử lý Combos
            var comboSelections = new List<ComboSelection>();
            if (!string.IsNullOrEmpty(selectedCombos))
            {
                try {
                    comboSelections = JsonSerializer.Deserialize<List<ComboSelection>>(selectedCombos);
                } catch { /* Handle error or ignore */ }
            }

            if (comboSelections != null)
            {
                foreach (var selection in comboSelections)
                {
                    var combo = await _context.Combos.FindAsync(selection.MaCombo);
                    if (combo != null && combo.TrangThai && selection.SoLuong > 0)
                    {
                        tongTien += combo.Gia * selection.SoLuong;
                    }
                }
            }

            // Áp dụng chiết khấu thành viên trước khi áp dụng mã khuyến mãi
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (currentUserId.HasValue)
            {
                var totalSpent = await _context.DatVes
                    .Where(d => d.MaNguoiDung == currentUserId.Value && d.TrangThai == "Đã thanh toán")
                    .SumAsync(d => d.TongTien);

                decimal membershipDiscountPercent = 0;
                if (totalSpent >= 10000000) membershipDiscountPercent = 15;
                else if (totalSpent >= 5000000) membershipDiscountPercent = 10;
                else if (totalSpent >= 1000000) membershipDiscountPercent = 5;

                if (membershipDiscountPercent > 0)
                {
                    tongTien = tongTien * (1 - membershipDiscountPercent / 100);
                }
            }

            // Áp dụng khuyến mãi nếu có (Logic chuẩn thực tế)
            int? maKM = null;
            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                var khuyenMai = await _context.KhuyenMais
                    .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai);

                if (khuyenMai != null)
                {
                    // Re-validate strictly in Backend
                    var now = DateTime.Now;
                    var isPromoAlreadyRedeemed = await _context.DatVes.AnyAsync(d => d.MaNguoiDung == currentUserId && d.MaKhuyenMai == khuyenMai.MaKhuyenMai && d.TrangThai != "Đã hủy");
                    
                    if (now >= khuyenMai.NgayBatDau && now <= khuyenMai.NgayKetThuc && tongTien >= khuyenMai.GiaTriToiThieu && !isPromoAlreadyRedeemed)
                    {
                        tongTien = tongTien * (1 - (decimal)khuyenMai.PhanTramGiam / 100);
                        maKM = khuyenMai.MaKhuyenMai;
                    }
                }
            }

            // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra lại một lần nữa để đảm bảo ghế vẫn còn trống
                    var latestGheDaDat = await _context.DatVeGhes
                        .Include(d => d.DatVe)
                        .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                        .Select(d => d.Ghe.SoGhe)
                        .ToListAsync();

                    var newConflictedSeats = selectedSeatsList.Where(s => latestGheDaDat.Contains(s)).ToList();
                    if (newConflictedSeats.Any())
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Ghế {string.Join(", ", newConflictedSeats)} vừa được người khác đặt. Vui lòng chọn ghế khác";
                        return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
                    }

                    // Tạo đơn đặt vé
                    if (!currentUserId.HasValue)
                    {
                        await transaction.RollbackAsync();
                        return RedirectToAction("Login", "Account");
                    }

                    var datVe = new DatVe
                    {
                        MaNguoiDung = currentUserId.Value,
                        MaLichChieu = maLichChieu,
                        NgayDat = DateTime.Now,
                        TongTien = tongTien,
                        TrangThai = "Chưa thanh toán",
                        MaKhuyenMai = maKM
                    };

                    _context.DatVes.Add(datVe);
                    await _context.SaveChangesAsync();

                    // Thêm chi tiết Combo
                    if (comboSelections != null)
                    {
                        foreach (var selection in comboSelections)
                        {
                            if (selection.SoLuong > 0)
                            {
                                var datVeCombo = new DatVeCombo
                                {
                                    MaDatVe = datVe.MaDatVe,
                                    MaCombo = selection.MaCombo,
                                    SoLuong = selection.SoLuong
                                };
                                _context.DatVeCombos.Add(datVeCombo);
                            }
                        }
                    }

                    // Thêm chi tiết ghế
                    foreach (var soGhe in selectedSeatsList)
                    {
                        var ghe = await _context.Ghes
                            .FirstOrDefaultAsync(g => g.SoGhe == soGhe && g.MaPhong == lichChieu.MaPhong);

                        if (ghe != null)
                        {
                            var datVeGhe = new DatVeGhe
                            {
                                MaDatVe = datVe.MaDatVe,
                                MaGhe = ghe.MaGhe
                            };

                            _context.DatVeGhes.Add(datVeGhe);
                        }
                        else
                        {
                            // Nếu ghế không tồn tại trong cơ sở dữ liệu, tạo mới
                            var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                            var col = int.Parse(soGhe.Substring(1)); // Số ghế

                            // Xác định loại ghế
                            string loaiGhe = "Thường";

                            // Tính toán số cột dựa trên sức chứa
                            int soHangGhe = 10; // Mặc định là 10 hàng
                            var sucChua = lichChieu.PhongChieu.SucChua;
                            int soGheMotHang;

                            if (sucChua <= 100)
                            {
                                soGheMotHang = 10; // Phòng nhỏ: 10 cột
                            }
                            else if (sucChua <= 150)
                            {
                                soGheMotHang = 15; // Phòng trung bình: 15 cột
                            }
                            else
                            {
                                soGheMotHang = 20; // Phòng lớn: 20 cột
                            }

                            // Lấy danh sách hàng ghế
                            var hangGhe = Enumerable.Range(0, soHangGhe)
                                .Select(i => (char)('A' + i))
                                .ToArray();

                            // Tính vị trí hàng
                            int hangIndex = Array.IndexOf(hangGhe, row);
                            int tongSoHang = hangGhe.Length;

                            // VIP: 30% hàng cuối
                            if (hangIndex >= tongSoHang * 0.7)
                            {
                                loaiGhe = "VIP";
                            }

                            // Sweetbox: Hàng cuối, 4 ghế đầu tiên
                            if (row == hangGhe[hangGhe.Length - 1] && col <= 4)
                            {
                                loaiGhe = "Sweetbox";
                            }

                            var gheNew = new Ghe
                            {
                                MaPhong = lichChieu.MaPhong,
                                SoGhe = soGhe,
                                LoaiGhe = loaiGhe
                            };

                            _context.Ghes.Add(gheNew);
                            await _context.SaveChangesAsync();

                            var datVeGhe = new DatVeGhe
                            {
                                MaDatVe = datVe.MaDatVe,
                                MaGhe = gheNew.MaGhe
                            };

                            _context.DatVeGhes.Add(datVeGhe);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Chuyển hướng đến trang thanh toán
                    return RedirectToAction("Index", "ThanhToan", new { maDatVe = datVe.MaDatVe });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt vé. Vui lòng thử lại sau.";
                    return RedirectToAction(nameof(ChonGhe), new { maLichChieu });
                }
            }
        }

        // Xử lý đặt vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int maLichChieu, string[] selectedSeats, string maKhuyenMai = null)
        {
            if (selectedSeats == null || selectedSeats.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một ghế");
                return RedirectToAction(nameof(Create), new { maLichChieu });
            }

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

            if (lichChieu == null)
            {
                return NotFound();
            }

            // Kiểm tra ghế đã được đặt
            var gheDaDat = await _context.DatVeGhes
                .Include(d => d.DatVe)
                .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                .Select(d => d.Ghe.SoGhe)
                .ToListAsync();

            if (selectedSeats.Any(s => gheDaDat.Contains(s)))
            {
                ModelState.AddModelError("", "Một số ghế đã được đặt. Vui lòng chọn ghế khác");
                return RedirectToAction(nameof(Create), new { maLichChieu });
            }

            // Tính tổng tiền
            decimal tongTien = selectedSeats.Length * lichChieu.GiaVe;

            // Áp dụng khuyến mãi nếu có
            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                var khuyenMai = await _context.KhuyenMais
                    .FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai && k.NgayKetThuc > DateTime.Now);

                if (khuyenMai != null)
                {
                    tongTien = tongTien * (1 - khuyenMai.PhanTramGiam / 100);
                }
            }

            // Tạo đơn đặt vé
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var datVe = new DatVe
            {
                MaNguoiDung = currentUserId.Value,
                MaLichChieu = maLichChieu,
                NgayDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = "Đang xử lý",
                MaKhuyenMai = !string.IsNullOrEmpty(maKhuyenMai) ?
                    (await _context.KhuyenMais.FirstOrDefaultAsync(k => k.MaCode == maKhuyenMai))?.MaKhuyenMai : (int?)null
            };

            _context.DatVes.Add(datVe);
            await _context.SaveChangesAsync();

            // Thêm chi tiết ghế
            foreach (var soGhe in selectedSeats)
            {
                var ghe = await _context.Ghes
                    .FirstOrDefaultAsync(g => g.SoGhe == soGhe && g.MaPhong == lichChieu.MaPhong);

                if (ghe != null)
                {
                    var datVeGhe = new DatVeGhe
                    {
                        MaDatVe = datVe.MaDatVe,
                        MaGhe = ghe.MaGhe
                    };

                    _context.DatVeGhes.Add(datVeGhe);
                }
                else
                {
                    // Nếu ghế không tồn tại trong cơ sở dữ liệu, tạo mới
                    var row = soGhe[0]; // Ký tự đầu tiên là hàng (A, B, C...)
                    var col = int.Parse(soGhe.Substring(1)); // Số ghế

                    // Xác định loại ghế
                    string loaiGhe = "Thường";

                    // Tính toán số cột dựa trên sức chứa
                    int soHangGhe = 10; // Mặc định là 10 hàng
                    var sucChua = lichChieu.PhongChieu.SucChua;
                    int soGheMotHang;

                    if (sucChua <= 100)
                    {
                        soGheMotHang = 10; // Phòng nhỏ: 10 cột
                    }
                    else if (sucChua <= 150)
                    {
                        soGheMotHang = 15; // Phòng trung bình: 15 cột
                    }
                    else
                    {
                        soGheMotHang = 20; // Phòng lớn: 20 cột
                    }

                    // Lấy danh sách hàng ghế
                    var hangGhe = Enumerable.Range(0, soHangGhe)
                        .Select(i => (char)('A' + i))
                        .ToArray();

                    // Tính vị trí hàng
                    int hangIndex = Array.IndexOf(hangGhe, row);
                    int tongSoHang = hangGhe.Length;

                    // VIP: 30% hàng cuối
                    if (hangIndex >= tongSoHang * 0.7)
                    {
                        loaiGhe = "VIP";
                    }

                    // Sweetbox: Hàng cuối, 4 ghế đầu tiên
                    if (row == hangGhe[hangGhe.Length - 1] && col <= 4)
                    {
                        loaiGhe = "Sweetbox";
                    }

                    var gheNew = new Ghe
                    {
                        MaPhong = lichChieu.MaPhong,
                        SoGhe = soGhe,
                        LoaiGhe = loaiGhe
                    };

                    _context.Ghes.Add(gheNew);
                    await _context.SaveChangesAsync();

                    var datVeGhe = new DatVeGhe
                    {
                        MaDatVe = datVe.MaDatVe,
                        MaGhe = gheNew.MaGhe
                    };

                    _context.DatVeGhes.Add(datVeGhe);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "ThanhToan", new { maDatVe = datVe.MaDatVe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyVe(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                .Include(d => d.ThanhToans)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dvg => dvg.Ghe)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra xem vé có phải của người dùng hiện tại không
            var currentUserId = await User.GetLegacyUserIdAsync(_context);
            if (!currentUserId.HasValue || datVe.MaNguoiDung != currentUserId.Value)
            {
                return Forbid();
            }

            // Bắt đầu Transaction sớm để đảm bảo tính Atomic (Nguyên tử)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Kiểm tra thời gian hủy vé (ít nhất 4 giờ trước giờ chiếu)
                    DateTime lichChieuDateTime = datVe.LichChieu.NgayChieu.Date.Add(datVe.LichChieu.GioChieu);
                    DateTime limitTime = lichChieuDateTime.AddHours(-4);

                    if (DateTime.Now > limitTime)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Không thể hủy vé trong vòng 4 giờ trước giờ chiếu.";
                        return RedirectToAction("LichSuDatVe", "Home");
                    }

                    // 2. Xác định mức hoàn tiền dựa trên trạng thái và thời gian
                    decimal refundPercentage = 0;
                    string refundNote = "Không hoàn tiền";

                    if (datVe.TrangThai == "Đã thanh toán")
                    {
                        TimeSpan timeToScreening = lichChieuDateTime - DateTime.Now;
                        if (timeToScreening.TotalHours > 24) { refundPercentage = 1.0m; refundNote = "Hoàn tiền 100%"; }
                        else if (timeToScreening.TotalHours >= 4) { refundPercentage = 0.5m; refundNote = "Hoàn tiền 50%"; }
                    }
                    else if (datVe.TrangThai == "Chưa thanh toán" || datVe.TrangThai == "Chờ thanh toán")
                    {
                        refundPercentage = 1.0m;
                        refundNote = "Hủy vé chưa thanh toán";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Không thể hủy vé với trạng thái này.";
                        return RedirectToAction("LichSuDatVe", "Home");
                    }

                    // 3. Phân loại xử lý theo Phương thức thanh toán (NHU CẦU PHÁT TRIỂN TIẾP)
                    var lastPayment = datVe.ThanhToans?.OrderByDescending(t => t.NgayThanhToan).FirstOrDefault();
                    string paymentMethod = lastPayment?.PhuongThucThanhToan ?? "CHƯA XÁC ĐỊNH";

                    if (datVe.TrangThai == "Đã thanh toán" && refundPercentage > 0)
                    {
                        var user = await _context.NguoiDungs.FindAsync(currentUserId.Value);
                        if (user != null)
                        {
                            // Khấu trừ điểm thưởng cũ (1,000đ = 1 điểm)
                            int pointsEarned = (int)(datVe.TongTien / 1000);
                            user.DiemTichLuy -= pointsEarned;
                            if (user.DiemTichLuy < 0) user.DiemTichLuy = 0;

                            // XỬ LÝ HOÀN TIỀN THEO TỪNG CỔNG
                            if (paymentMethod.ToUpper() == "MOMO")
                            {
                                // TODO: Gọi API Refund của MoMo tại đây
                                // Tạm thời vẫn hoàn vào điểm nếu chưa có API thật, hoặc ghi log yêu cầu hoàn thủ công
                                int pointsRefund = (int)((datVe.TongTien * refundPercentage) / 1000);
                                user.DiemTichLuy += pointsRefund;
                                refundNote += " (Qua MoMo -> Quy đổi điểm)"; 
                            }
                            else if (paymentMethod.ToUpper() == "VNPAY")
                            {
                                // TODO: Gọi API Refund của VNPay tại đây
                                int pointsRefund = (int)((datVe.TongTien * refundPercentage) / 1000);
                                user.DiemTichLuy += pointsRefund;
                            }
                            else if (paymentMethod == "Tại rạp")
                            {
                                // Đối với thanh toán tại rạp bằng tiền mặt, thường Admin sẽ hoàn tiền mặt
                                // Không tự động cộng điểm, ghi chú để Admin biết
                                refundNote += " (Cần hoàn tiền mặt tại quầy)";
                            }
                            else 
                            {
                                // Mặc định hoàn vào điểm (Ví dụ thanh toán bằng Điểm/Voucher)
                                int pointsRefund = (int)((datVe.TongTien * refundPercentage) / 1000);
                                user.DiemTichLuy += pointsRefund;
                            }

                            _context.Update(user);

                            // Ghi log hoàn tiền kỹ thuật
                            _context.LichSuGiaoDiches.Add(new LichSuGiaoDich {
                                MaNguoiDung = currentUserId,
                                LoaiGiaoDich = "Hoàn tiền/điểm",
                                NoiDung = $"Hủy vé #{maDatVe} ({paymentMethod}). {refundNote}. Khấu trừ {pointsEarned}đ thưởng.",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            });
                        }
                    }

                    // 4. Giải phóng ghế (Release Seats) - Đảm bảo Atomic
                    string danhSachGhe = "N/A";
                    if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                    {
                        danhSachGhe = string.Join(", ", datVe.DatVeGhes.Select(dvg => dvg.Ghe?.SoGhe));
                        _context.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                    }

                    // 5. Cập nhật trạng thái Thanh toán liên quan
                    if (datVe.ThanhToans != null)
                    {
                        foreach (var t in datVe.ThanhToans)
                        {
                            t.TrangThai = "Đã hủy";
                            t.GhiChu = $"Hủy cùng vé #{maDatVe} vào {DateTime.Now:dd/MM HH:mm}";
                        }
                    }

                    // 6. Chốt DEAL: Cập nhật trạng thái vé
                    datVe.TrangThai = "Đã hủy";
                    datVe.GhiChu = $"Ghế hủy: {danhSachGhe}. {refundNote}";
                    _context.Update(datVe);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Hủy vé thành công! {refundNote}";
                    return RedirectToAction("LichSuDatVe", "Home");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống khi hủy vé: " + ex.Message;
                    return RedirectToAction("LichSuDatVe", "Home");
                }
            }
        }

        // API lấy danh sách ghế đã đặt theo thời gian thực
        [HttpGet]
        public async Task<IActionResult> GetBookedSeats(int maLichChieu)
        {
            try
            {
                var lichChieu = await _context.LichChieus
                    .Include(l => l.PhongChieu)
                    .FirstOrDefaultAsync(l => l.MaLichChieu == maLichChieu);

                if (lichChieu == null)
                {
                    return Json(new {
                        success = false,
                        message = "Không tìm thấy lịch chiếu"
                    });
                }

                // Lấy danh sách ghế đã đặt
                var gheDaDat = await _context.DatVeGhes
                    .Include(d => d.DatVe)
                    .Where(d => d.DatVe.MaLichChieu == maLichChieu && d.DatVe.TrangThai != "Đã hủy")
                    .Select(d => d.Ghe.SoGhe)
                    .ToListAsync() ?? new List<string>();

                // Lấy thông tin hàng ghế và số cột
                int soCot = 0;
                char[] hangGhe = Array.Empty<char>();

                // Tính toán số cột dựa trên sức chứa
                var sucChua = lichChieu.PhongChieu.SucChua;
                if (sucChua <= 100)
                {
                    soCot = 10; // Phòng nhỏ: 10 cột
                }
                else if (sucChua <= 150)
                {
                    soCot = 15; // Phòng trung bình: 15 cột
                }
                else
                {
                    soCot = 20; // Phòng lớn: 20 cột
                }

                // Tính số hàng cần thiết để đạt được sức chứa
                int soHang = (int)Math.Ceiling((double)sucChua / soCot);

                // Tạo mảng các ký tự hàng từ A đến Z
                hangGhe = Enumerable.Range(0, soHang)
                    .Select(i => (char)('A' + i))
                    .ToArray();

                return Json(new {
                    success = true,
                    bookedSeats = gheDaDat,
                    hangGhe = hangGhe,
                    soCot = soCot
                });
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = "Lỗi khi lấy danh sách ghế: " + ex.Message
                });
            }
        }

        // In vé
        public async Task<IActionResult> Print(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .Include(d => d.DatVeCombos)
                    .ThenInclude(dc => dc.Combo)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập: người dùng chỉ được in vé của mình
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue || datVe.MaNguoiDung != userId.Value)
            {
                return Forbid();
            }

            // Kiểm tra vé phải đã thanh toán
            if (datVe.TrangThai != "Đã thanh toán")
            {
                TempData["ErrorMessage"] = "Vé chưa được thanh toán, không thể in.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            return View(datVe);
        }

        // In từng vé cho từng ghế
        public async Task<IActionResult> PrintMultiple(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .Include(d => d.DatVeCombos)
                    .ThenInclude(dc => dc.Combo)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập: người dùng chỉ được in vé của mình
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue || datVe.MaNguoiDung != userId.Value)
            {
                return Forbid();
            }

            // Kiểm tra vé phải đã thanh toán
            if (datVe.TrangThai != "Đã thanh toán")
            {
                TempData["ErrorMessage"] = "Vé chưa được thanh toán, không thể in.";
                return RedirectToAction("LichSuDatVe", "Home");
            }

            return View(datVe);
        }
    }

    public class ComboSelection
    {
        public int MaCombo { get; set; }
        public int SoLuong { get; set; }
    }
}