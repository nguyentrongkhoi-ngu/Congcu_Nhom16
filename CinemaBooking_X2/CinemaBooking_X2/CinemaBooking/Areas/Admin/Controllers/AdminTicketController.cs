using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using System.Security.Claims;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminTicketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminTicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang kiểm tra vé
        public IActionResult Verify()
        {
            return View();
        }

        // Trang lịch sử kiểm tra vé
        public async Task<IActionResult> VerifyHistory()
        {
            // Lấy 50 lịch sử kiểm tra gần nhất
            var history = await _context.LichSuGiaoDiches
                .Where(l => l.LoaiGiaoDich == "Kiểm tra vé")
                .OrderByDescending(l => l.NgayGiaoDich)
                .Take(50)
                .ToListAsync();
                
            return View(history);
        }

        // API xác thực vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTicket(int ticketId, string seatNumber)
        {
            if (ticketId <= 0)
            {
                return Json(new { success = false, message = "Mã vé không hợp lệ" });
            }

            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.MaDatVe == ticketId);

            if (datVe == null)
            {
                LogVerification(ticketId, "Kiểm tra thất bại", "Không tìm thấy thông tin vé");
                return Json(new { success = false, message = "Không tìm thấy thông tin vé" });
            }

            if (datVe.TrangThai != "Đã thanh toán")
            {
                LogVerification(ticketId, "Kiểm tra thất bại", $"Vé chưa được thanh toán. Trạng thái: {datVe.TrangThai}");
                return Json(new { 
                    success = false, 
                    message = $"Vé chưa được thanh toán. Trạng thái hiện tại: {datVe.TrangThai}" 
                });
            }

            // Kiểm tra ghế nếu cung cấp
            bool seatValid = true;
            if (!string.IsNullOrEmpty(seatNumber))
            {
                seatValid = datVe.DatVeGhes.Any(dg => dg.Ghe.SoGhe == seatNumber);
                if (!seatValid)
                {
                    LogVerification(ticketId, "Kiểm tra thất bại", $"Ghế {seatNumber} không thuộc vé này");
                    return Json(new { 
                        success = false, 
                        message = $"Ghế {seatNumber} không thuộc vé này" 
                    });
                }
            }

            // Kiểm tra suất chiếu đã qua chưa
            var now = DateTime.Now;
            if (datVe.LichChieu.NgayChieu < now.Date || 
                (datVe.LichChieu.NgayChieu == now.Date && datVe.LichChieu.GioChieu < now.TimeOfDay))
            {
                LogVerification(ticketId, "Kiểm tra thất bại", "Vé đã hết hạn. Suất chiếu đã kết thúc.");
                return Json(new { 
                    success = false, 
                    message = "Vé đã hết hạn. Suất chiếu đã kết thúc."
                });
            }

            // Kiểm tra vé đã được sử dụng chưa
            var usedCheck = await _context.LichSuGiaoDiches
                .Where(l => l.NoiDung.Contains($"Vé #{ticketId} đã được xác nhận") && 
                       l.TrangThai == "Thành công" &&
                       l.LoaiGiaoDich == "Kiểm tra vé")
                .FirstOrDefaultAsync();

            // Suất chiếu còn trong tương lai
            var showDateTime = datVe.LichChieu.NgayChieu.Add(datVe.LichChieu.GioChieu);
            var isEarly = showDateTime > now.AddHours(1);
            var isUsed = usedCheck != null;
            
            string resultMessage;
            string statusString;
            
            if (isEarly)
            {
                resultMessage = "Vé hợp lệ, nhưng vẫn còn sớm (trước giờ chiếu hơn 1 giờ)";
                statusString = "Thành công (Sớm)";
            }
            else if (isUsed)
            {
                resultMessage = $"Vé đã được sử dụng trước đó vào lúc {usedCheck.NgayGiaoDich?.ToString("dd/MM/yyyy HH:mm:ss")}";
                statusString = "Thành công (Đã dùng)";
            }
            else
            {
                resultMessage = "Vé hợp lệ";
                statusString = "Thành công";
            }
            
            // Lưu thông tin kiểm tra
            string seatInfo = !string.IsNullOrEmpty(seatNumber) ? $" (Ghế: {seatNumber})" : "";
            LogVerification(
                ticketId, 
                statusString, 
                $"Vé #{ticketId} đã được xác nhận{seatInfo} cho phim '{datVe.LichChieu?.Phim?.TenPhim}'"
            );

            // Vé hợp lệ
            return Json(new { 
                success = true, 
                isEarly = isEarly,
                isUsed = isUsed,
                message = resultMessage,
                usedTime = isUsed ? usedCheck.NgayGiaoDich?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                ticket = new
                {
                    id = datVe.MaDatVe,
                    customer = datVe.NguoiDung.HoTen,
                    movie = datVe.LichChieu.Phim.TenPhim,
                    cinema = datVe.LichChieu.PhongChieu.RapPhim.TenRap,
                    room = datVe.LichChieu.PhongChieu.SoPhong,
                    showDateTime = showDateTime.ToString("dd/MM/yyyy HH:mm"),
                    seats = string.Join(", ", datVe.DatVeGhes.Select(dg => dg.Ghe.SoGhe)),
                    selectedSeat = seatNumber
                }
            });
        }
        
        // Lưu lịch sử kiểm tra vé
        private async void LogVerification(int ticketId, string status, string message)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdClaim, out int userId);
                
                var logEntry = new LichSuGiaoDich
                {
                    MaNguoiDung = userId > 0 ? userId : null,
                    LoaiGiaoDich = "Kiểm tra vé",
                    NoiDung = message,
                    NgayGiaoDich = DateTime.Now,
                    TrangThai = status
                };
                
                _context.LichSuGiaoDiches.Add(logEntry);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Bỏ qua lỗi khi ghi log
            }
        }
    }
} 