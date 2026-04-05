using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CinemasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CinemasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Cinemas
        public async Task<IActionResult> Index()
        {
            var cinemas = await _context.RapPhims
                .Include(r => r.PhongChieus)
                .OrderBy(r => r.TenRap)
                .AsNoTracking()
                .ToListAsync();
            return View(cinemas);
        }

        // GET: Admin/Cinemas/GetCinema/5 (For Modal)
        [HttpGet]
        public async Task<IActionResult> GetCinema(int id)
        {
            var cinema = await _context.RapPhims
                .Include(r => r.PhongChieus)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaRap == id);

            if (cinema == null) return NotFound();

            return Json(new
            {
                maRap = cinema.MaRap,
                tenRap = cinema.TenRap,
                diaChi = cinema.DiaChi,
                thanhPho = cinema.ThanhPho,
                phongs = cinema.PhongChieus.Select(p => new {
                    maPhong = p.MaPhong,
                    soPhong = p.SoPhong,
                    sucChua = p.SucChua,
                    loaiPhong = p.LoaiPhong ?? "Tiêu chuẩn"
                })
            });
        }

        // POST: Admin/Cinemas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RapPhim cinema)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cinema);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm rạp phim mới thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Cinemas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RapPhim cinema)
        {
            if (id != cinema.MaRap) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cinema);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật rạp phim thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CinemaExists(cinema.MaRap)) return NotFound();
                    else throw;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Cinemas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var cinema = await _context.RapPhims
                    .Include(r => r.PhongChieus)
                    .FirstOrDefaultAsync(r => r.MaRap == id);

                if (cinema != null)
                {
                    _context.RapPhims.Remove(cinema);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Xóa rạp phim thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy rạp phim để xóa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        #region Room Management (PhongChieu)

        // GET: Admin/Cinemas/GetRoom/5
        [HttpGet]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await _context.PhongChieus.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
            if (room == null) return NotFound();
            return Json(room);
        }

        // POST: Admin/Cinemas/UpsertRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertRoom(PhongChieu room)
        {
            try
            {
                if (room.MaPhong == 0)
                {
                    _context.PhongChieus.Add(room);
                }
                else
                {
                    _context.PhongChieus.Update(room);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Lưu thông tin phòng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Cinemas/DeleteRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                var room = await _context.PhongChieus.FindAsync(id);
                if (room != null)
                {
                    _context.PhongChieus.Remove(room);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Xóa phòng thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy phòng để xóa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        #endregion

        private bool CinemaExists(int id)
        {
            return _context.RapPhims.Any(e => e.MaRap == id);
        }
    }
}
