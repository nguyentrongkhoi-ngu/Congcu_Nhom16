using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminBooking
        public async Task<IActionResult> Index(int? rapId, int? phongId, string trangThai, DateTime? tuNgay, DateTime? denNgay, string search)
        {
            // Lấy danh sách rạp phim cho dropdown
            ViewBag.RapPhims = await _context.RapPhims.ToListAsync();
            
            // Lấy danh sách phòng chiếu (nếu có chọn rạp)
            if (rapId.HasValue)
            {
                ViewBag.PhongChieus = await _context.PhongChieus
                    .Where(p => p.MaRap == rapId.Value)
                    .ToListAsync();
            }
            
            // Danh sách các trạng thái đặt vé
            ViewBag.TrangThais = new List<string> { "Đã đặt", "Đã thanh toán", "Đã hủy", "Chờ thanh toán" };
            
            // Lấy query danh sách đặt vé
            var datVeQuery = _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.KhuyenMai)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dvg => dvg.Ghe)
                .AsQueryable();

            // Lọc theo rạp phim (nếu có)
            if (rapId.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.LichChieu.PhongChieu.MaRap == rapId.Value);
            }
            
            // Lọc theo phòng chiếu (nếu có)
            if (phongId.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.LichChieu.MaPhong == phongId.Value);
            }
            
            // Lọc theo trạng thái (nếu có)
            if (!string.IsNullOrEmpty(trangThai))
            {
                datVeQuery = datVeQuery.Where(d => d.TrangThai == trangThai);
            }
            
            // Lọc theo ngày đặt (nếu có)
            if (tuNgay.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.NgayDat >= tuNgay.Value.Date);
            }
            
            if (denNgay.HasValue)
            {
                datVeQuery = datVeQuery.Where(d => d.NgayDat <= denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            }
            
            // Lọc theo tìm kiếm (tên, email, số điện thoại hoặc mã vé)
            if (!string.IsNullOrEmpty(search))
            {
                string s = search.ToLower();
                datVeQuery = datVeQuery.Where(d => 
                    d.MaDatVe.ToString().Contains(s) || 
                    d.NguoiDung.HoTen.ToLower().Contains(s) || 
                    d.NguoiDung.Email.ToLower().Contains(s) || 
                    d.NguoiDung.SoDienThoai.Contains(s));
            }

            // Sắp xếp theo ngày đặt mới nhất
            datVeQuery = datVeQuery.OrderByDescending(d => d.NgayDat);
            
            // Lưu giá trị lọc vào ViewBag để hiển thị lại trên form
            ViewBag.RapId = rapId;
            ViewBag.PhongId = phongId;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;

            return View(await datVeQuery.ToListAsync());
        }

        // GET: AdminBooking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var datVe = await _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.KhuyenMai)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dvg => dvg.Ghe)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(m => m.MaDatVe == id);
                
            if (datVe == null)
            {
                return NotFound();
            }

            return View(datVe);
        }

        // POST: AdminBooking/CapNhatTrangThai/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
        {
            var datVe = await _context.DatVes
                .Include(d => d.DatVeGhes)
                .FirstOrDefaultAsync(d => d.MaDatVe == id);
                
            if (datVe == null)
            {
                return NotFound();
            }

            // Lưu trạng thái cũ trước khi cập nhật
            string trangThaiCu = datVe.TrangThai;
            
            // Cập nhật trạng thái mới
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    datVe.TrangThai = trangThai;

                    // Nếu cập nhật trạng thái thành "Đã hủy" và trạng thái trước đó không phải là "Đã hủy"
                    if (trangThai == "Đã hủy" && trangThaiCu != "Đã hủy")
                    {
                        // 1. Xử lý hoàn tiền/điểm nếu là vé Đã thanh toán - PHÂN LOẠI CỔNG
                        if (trangThaiCu == "Đã thanh toán")
                        {
                            var datVeFull = await _context.DatVes
                                .Include(d => d.LichChieu)
                                .Include(d => d.NguoiDung)
                                .Include(d => d.ThanhToans)
                                .FirstOrDefaultAsync(d => d.MaDatVe == id);

                            if (datVeFull != null && datVeFull.NguoiDung != null)
                            {
                                DateTime lichChieuDT = datVeFull.LichChieu.NgayChieu.Date.Add(datVeFull.LichChieu.GioChieu);
                                TimeSpan timeToScreening = lichChieuDT - DateTime.Now;
                                
                                decimal refundPercentage = 0;
                                string refundNote = "Không hoàn tiền";

                                if (timeToScreening.TotalHours > 24) { refundPercentage = 1.0m; refundNote = "Hoàn tiền 100%"; }
                                else if (timeToScreening.TotalHours >= 4) { refundPercentage = 0.5m; refundNote = "Hoàn tiền 50%"; }

                                if (refundPercentage > 0)
                                {
                                    var lastPayment = datVeFull.ThanhToans?.OrderByDescending(t => t.NgayThanhToan).FirstOrDefault();
                                    string method = lastPayment?.PhuongThucThanhToan ?? "CHƯA XÁC ĐỊNH";

                                    // Khấu trừ điểm cũ
                                    int pointsEarned = (int)(datVeFull.TongTien / 1000);
                                    datVeFull.NguoiDung.DiemTichLuy -= pointsEarned;
                                    if (datVeFull.NguoiDung.DiemTichLuy < 0) datVeFull.NguoiDung.DiemTichLuy = 0;

                                    if (method.ToUpper() == "MOMO" || method.ToUpper() == "VNPAY")
                                    {
                                        // TODO: Thực tế cần gọi API Refund portal
                                        int pointsRefund = (int)((datVeFull.TongTien * refundPercentage) / 1000);
                                        datVeFull.NguoiDung.DiemTichLuy += pointsRefund;
                                        refundNote += $" (Qua {method} -> Quy đổi điểm)";
                                    }
                                    else if (method == "Tại rạp")
                                    {
                                        refundNote += " (Admin cần hoàn tiền mặt tại quầy)";
                                    }
                                    else
                                    {
                                        int pointsRefund = (int)((datVeFull.TongTien * refundPercentage) / 1000);
                                        datVeFull.NguoiDung.DiemTichLuy += pointsRefund;
                                    }

                                    _context.Update(datVeFull.NguoiDung);
                                    
                                    // Log chi tiết
                                    _context.LichSuGiaoDiches.Add(new LichSuGiaoDich {
                                        MaNguoiDung = datVeFull.MaNguoiDung,
                                        LoaiGiaoDich = "Admin hoàn tiền",
                                        NoiDung = $"Admin xử lý hoàn vé #{id} ({method}). {refundNote}.",
                                        NgayGiaoDich = DateTime.Now,
                                        TrangThai = "Thành công"
                                    });
                                    
                                    TempData["SuccessMessage"] = $"Huỷ vé thành công! {refundNote}";
                                }
                            }
                        }

                        // 2. Giải phóng ghế
                        if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                        {
                            string thongTinGhe = string.Join(", ", datVe.DatVeGhes.Select(dvg => dvg.Ghe?.SoGhe));
                            _context.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                            
                            _context.LichSuGiaoDiches.Add(new LichSuGiaoDich {
                                MaNguoiDung = datVe.MaNguoiDung,
                                LoaiGiaoDich = "Hủy vé",
                                TrangThai = "Thành công",
                                NoiDung = $"Admin hủy vé #{datVe.MaDatVe}, mở lại ghế: {thongTinGhe}",
                                NgayGiaoDich = DateTime.Now
                            });
                        }
                    }
                    else if (trangThai == "Đã thanh toán" && trangThaiCu != "Đã thanh toán")
                    {
                        await UpdateUserPoints(id);
                        TempData["SuccessMessage"] = "Xác nhận thanh toán và cộng điểm thành công!";
                    }
                    
                    if (TempData["SuccessMessage"] == null)
                        TempData["SuccessMessage"] = "Cập nhật trạng thái vé thành công!";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật trạng thái: " + ex.Message;
                }
            }
            
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Ajax endpoint to get rooms by cinema
        [HttpGet]
        public async Task<JsonResult> GetPhongsByRap(int rapId)
        {
            var phongChieus = await _context.PhongChieus
                .Where(p => p.MaRap == rapId)
                .Select(p => new { 
                    MaPhong = p.MaPhong, 
                    TenPhong = $"Phòng {p.SoPhong}" 
                })
                .ToListAsync();
                
            return Json(phongChieus);
        }

        // Cập nhật điểm tích lũy cho người dùng sau khi thanh toán thành công
        private async Task UpdateUserPoints(int maDatVe)
        {
            try
            {
                var datVe = await _context.DatVes
                    .Include(d => d.NguoiDung)
                    .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

                if (datVe != null && datVe.NguoiDung != null && datVe.TrangThai == "Đã thanh toán")
                {
                    // Kiểm tra xem đã tích điểm cho vé này chưa (tránh cộng trùng)
                    var exists = await _context.LichSuGiaoDiches
                        .AnyAsync(l => l.MaNguoiDung == datVe.MaNguoiDung && l.LoaiGiaoDich == "Tích điểm" && l.NoiDung.Contains($"vé #{maDatVe}"));
                    
                    if (!exists)
                    {
                        // 1,000 VND = 1 điểm
                        int pointsEarned = (int)(datVe.TongTien / 1000);
                        
                        if (pointsEarned > 0)
                        {
                            datVe.NguoiDung.DiemTichLuy += pointsEarned;
                            
                            // Ghi log tích điểm
                            var lichSuDiem = new LichSuGiaoDich
                            {
                                MaNguoiDung = datVe.MaNguoiDung,
                                LoaiGiaoDich = "Tích điểm",
                                TrangThai = "Thành công",
                                NoiDung = $"Tích +{pointsEarned} điểm từ vé #{maDatVe} (Admin xác nhận)",
                                NgayGiaoDich = DateTime.Now
                            };
                            
                            _context.LichSuGiaoDiches.Add(lichSuDiem);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating points: {ex.Message}");
            }
        }
    }
}