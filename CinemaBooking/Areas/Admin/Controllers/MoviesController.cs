using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Phims
                .Include(p => p.LichChieus)
                .OrderByDescending(p => p.MaPhim)
                .ToListAsync();
            return View(movies);
        }

        // GET: Admin/Movies/GetMovie/5 (For Modal)
        [HttpGet]
        public async Task<IActionResult> GetMovie(int id)
        {
            var phim = await _context.Phims
                .Include(p => p.NgonNguPhims)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null) return NotFound();

            return Json(new
            {
                maPhim = phim.MaPhim,
                tenPhim = phim.TenPhim,
                moTa = phim.MoTa,
                thoiLuong = phim.ThoiLuong,
                theLoai = phim.TheLoai,
                ngayPhatHanh = phim.NgayPhatHanh?.ToString("yyyy-MM-dd"),
                urlPoster = phim.UrlPoster,
                dinhDang = phim.DinhDang,
                trailer = phim.Trailer,
                trangThai = phim.TrangThai
            });
        }

        // GET: Admin/Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.NgonNguPhims)
                .Include(p => p.LichChieus)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(pc => pc.RapPhim)
                .Include(p => p.DanhGias)
                    .ThenInclude(d => d.NguoiDung)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // GET: Admin/Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phim phim, IFormFile? posterFile, IFormFile? trailerFile)
        {
            if (ModelState.IsValid)
            {
                // Handle file uploads
                if (posterFile != null && posterFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await posterFile.CopyToAsync(stream);
                    }
                    
                    phim.UrlPoster = "/posters/" + fileName;
                }

                if (trailerFile != null && trailerFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(trailerFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await trailerFile.CopyToAsync(stream);
                    }
                    
                    phim.Trailer = "/trailers/" + fileName;
                }

                _context.Add(phim);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Thêm phim mới thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }
            
            return View(phim);
        }

        // POST: Admin/Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Phim phim, IFormFile? posterFile, IFormFile? trailerFile)
        {
            if (id != phim.MaPhim) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Track original entity to keep existing files if no data provided
                    var existingPhim = await _context.Phims.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhim == id);
                    if (existingPhim == null) return NotFound();

                    // Handle file uploads
                    if (posterFile != null && posterFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters");
                        Directory.CreateDirectory(uploadsFolder);
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(posterFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await posterFile.CopyToAsync(stream); }
                        phim.UrlPoster = "/posters/" + fileName;
                    }
                    else if (string.IsNullOrEmpty(phim.UrlPoster))
                    {
                        // No new file and no new URL, keep existing
                        phim.UrlPoster = existingPhim.UrlPoster;
                    }

                    if (trailerFile != null && trailerFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                        Directory.CreateDirectory(uploadsFolder);
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(trailerFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await trailerFile.CopyToAsync(stream); }
                        phim.Trailer = "/trailers/" + fileName;
                    }
                    else if (string.IsNullOrEmpty(phim.Trailer))
                    {
                        // No new file and no new URL, keep existing
                        phim.Trailer = existingPhim.Trailer;
                    }

                    _context.Update(phim);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phim thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhimExists(phim.MaPhim)) return NotFound();
                    else throw;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var phim = await _context.Phims
                    .Include(p => p.LichChieus)
                    .Include(p => p.DanhGias)
                    .Include(p => p.NgonNguPhims)
                    .FirstOrDefaultAsync(p => p.MaPhim == id);

                if (phim != null)
                {
                    // Manually handle cascading deletes if not configured in DB
                    if (phim.LichChieus != null && phim.LichChieus.Any())
                        _context.LichChieus.RemoveRange(phim.LichChieus);
                    
                    if (phim.DanhGias != null && phim.DanhGias.Any())
                        _context.DanhGias.RemoveRange(phim.DanhGias);
                    
                    if (phim.NgonNguPhims != null && phim.NgonNguPhims.Any())
                        _context.NgonNguPhims.RemoveRange(phim.NgonNguPhims);

                    _context.Phims.Remove(phim);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Xóa phim và các dữ liệu liên quan thành công!" });
                }
                
                return Json(new { success = false, message = "Không tìm thấy phim để xóa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // POST: Admin/Movies/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                // Cascade delete should handle most things, but to be sure and avoid FK issues
                // we'll remove related records in order
                var allLichChieu = await _context.LichChieus.ToListAsync();
                if (allLichChieu.Any()) _context.LichChieus.RemoveRange(allLichChieu);

                var allDanhGia = await _context.DanhGias.ToListAsync();
                if (allDanhGia.Any()) _context.DanhGias.RemoveRange(allDanhGia);

                var allNgonNguPhim = await _context.NgonNguPhims.ToListAsync();
                if (allNgonNguPhim.Any()) _context.NgonNguPhims.RemoveRange(allNgonNguPhim);

                var allPhim = await _context.Phims.ToListAsync();
                if (allPhim.Any()) _context.Phims.RemoveRange(allPhim);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa tất cả phim và các dữ liệu liên quan thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi xóa tất cả: " + ex.Message });
            }
        }

        private bool PhimExists(int id)
        {
            return _context.Phims.Any(e => e.MaPhim == id);
        }
    }
}
