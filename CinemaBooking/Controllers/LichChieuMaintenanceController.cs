using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LichChieuMaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LichChieuMaintenanceController> _logger;

        public LichChieuMaintenanceController(
            ApplicationDbContext context, 
            ILogger<LichChieuMaintenanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: LichChieuMaintenance
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            
            // Lấy các lịch chiếu của ngày hiện tại và trước đó
            var lichChieuQuery = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.PhongChieu)
                    .ThenInclude(pc => pc.RapPhim)
                .Where(lc => lc.NgayChieu.Date <= now.Date)
                .ToListAsync();

            // Lọc các lịch chiếu đã hết hạn trong bộ nhớ
            var expiredLichChieu = lichChieuQuery
                .Where(lc => 
                    lc.NgayChieu.Date < now.Date || 
                    (lc.NgayChieu.Date == now.Date && 
                    lc.GioChieu.Add(TimeSpan.FromMinutes(lc.Phim.ThoiLuong > 0 ? lc.Phim.ThoiLuong : 120)) < now.TimeOfDay))
                .OrderByDescending(lc => lc.NgayChieu)
                .ThenByDescending(lc => lc.GioChieu)
                .ToList();
                
            // Đếm số vé đã đặt cho mỗi lịch chiếu
            foreach (var lichChieu in expiredLichChieu)
            {
                lichChieu.DatVes = await _context.DatVes
                    .Where(dv => dv.MaLichChieu == lichChieu.MaLichChieu)
                    .ToListAsync();
            }
            
            return View(expiredLichChieu);
        }
        
        // GET: LichChieuMaintenance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichChieu = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.PhongChieu)
                    .ThenInclude(pc => pc.RapPhim)
                .Include(lc => lc.DatVes)
                    .ThenInclude(dv => dv.DatVeGhes)
                        .ThenInclude(dvg => dvg.Ghe)
                .Include(lc => lc.DatVes)
                    .ThenInclude(dv => dv.NguoiDung)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);
                
            if (lichChieu == null)
            {
                return NotFound();
            }

            return View(lichChieu);
        }
        
        // POST: LichChieuMaintenance/Reset/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(int id)
        {
            var lichChieu = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.DatVes)
                    .ThenInclude(dv => dv.DatVeGhes)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);
                
            if (lichChieu == null)
            {
                return NotFound();
            }
            
            // Đếm số vé bị ảnh hưởng
            int soVeCount = lichChieu.DatVes.Count;
            int soGheCount = 0;
            
            // Xóa tất cả các bản ghi dat_ve_ghe liên quan
            foreach (var datVe in lichChieu.DatVes)
            {
                if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                {
                    soGheCount += datVe.DatVeGhes.Count;
                    _context.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                }
                
                // Cập nhật trạng thái đặt vé thành "Đã hoàn thành" nếu chưa bị hủy
                if (datVe.TrangThai != "Đã hủy")
                {
                    datVe.TrangThai = "Đã hoàn thành";
                }
            }
            
            // Tạo bản ghi lịch sử giao dịch
            var lichSuGiaoDich = new LichSuGiaoDich
            {
                LoaiGiaoDich = "Hệ thống",
                TrangThai = "Thành công",
                NoiDung = $"Admin reset lịch chiếu #{lichChieu.MaLichChieu} ({lichChieu.Phim.TenPhim}), giải phóng {soGheCount} ghế từ {soVeCount} vé",
                NgayGiaoDich = DateTime.Now
            };
            
            _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
            
            // Lưu thay đổi vào database
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã reset thành công lịch chiếu, giải phóng {soGheCount} ghế từ {soVeCount} vé";
            
            return RedirectToAction(nameof(Index));
        }
        
        // POST: LichChieuMaintenance/ResetAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAll()
        {
            var now = DateTime.Now;
            
            // Lấy các lịch chiếu của ngày hiện tại và trước đó
            var lichChieuQuery = await _context.LichChieus
                .Include(lc => lc.Phim)
                .Include(lc => lc.DatVes)
                    .ThenInclude(dv => dv.DatVeGhes)
                .Where(lc => lc.NgayChieu.Date <= now.Date)
                .ToListAsync();

            // Lọc các lịch chiếu đã hết hạn trong bộ nhớ
            var expiredLichChieu = lichChieuQuery
                .Where(lc => 
                    lc.NgayChieu.Date < now.Date || 
                    (lc.NgayChieu.Date == now.Date && 
                    lc.GioChieu.Add(TimeSpan.FromMinutes(lc.Phim.ThoiLuong > 0 ? lc.Phim.ThoiLuong : 120)) < now.TimeOfDay))
                .ToList();
                
            int soLichChieuCount = expiredLichChieu.Count;
            int soVeCount = 0;
            int soGheCount = 0;
            
            foreach (var lichChieu in expiredLichChieu)
            {
                soVeCount += lichChieu.DatVes.Count;
                
                // Xóa tất cả các bản ghi dat_ve_ghe liên quan
                foreach (var datVe in lichChieu.DatVes)
                {
                    if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                    {
                        soGheCount += datVe.DatVeGhes.Count;
                        _context.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                    }
                    
                    // Cập nhật trạng thái đặt vé thành "Đã hoàn thành" nếu chưa bị hủy
                    if (datVe.TrangThai != "Đã hủy")
                    {
                        datVe.TrangThai = "Đã hoàn thành";
                    }
                }
            }
            
            if (soLichChieuCount > 0)
            {
                // Tạo bản ghi lịch sử giao dịch
                var lichSuGiaoDich = new LichSuGiaoDich
                {
                    LoaiGiaoDich = "Hệ thống",
                    TrangThai = "Thành công",
                    NoiDung = $"Admin reset tất cả {soLichChieuCount} lịch chiếu đã kết thúc, giải phóng {soGheCount} ghế từ {soVeCount} vé",
                    NgayGiaoDich = DateTime.Now
                };
                
                _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                
                // Lưu thay đổi vào database
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã reset thành công {soLichChieuCount} lịch chiếu, giải phóng {soGheCount} ghế từ {soVeCount} vé";
            }
            else
            {
                TempData["InfoMessage"] = "Không có lịch chiếu nào cần reset";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
} 