using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;
using CinemaBooking.Data;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminKhuyenMaiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminKhuyenMaiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminKhuyenMai
        public async Task<IActionResult> Index()
        {
            var khuyenMais = await _context.KhuyenMais
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();
            return View(khuyenMais);
        }

        // GET: Admin/AdminKhuyenMai/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminKhuyenMai/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaCode,PhanTramGiam,GiaTriToiThieu,NgayBatDau,NgayKetThuc,MoTa")] KhuyenMai khuyenMai)
        {
            if (ModelState.IsValid)
            {
                if (khuyenMai.NgayBatDau > khuyenMai.NgayKetThuc)
                {
                    ModelState.AddModelError("", "Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
                    return View(khuyenMai);
                }

                _context.Add(khuyenMai);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mã khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(khuyenMai);
        }

        // GET: Admin/AdminKhuyenMai/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null) return NotFound();

            return View(khuyenMai);
        }

        // POST: Admin/AdminKhuyenMai/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaKhuyenMai,MaCode,PhanTramGiam,GiaTriToiThieu,NgayBatDau,NgayKetThuc,MoTa")] KhuyenMai khuyenMai)
        {
            if (id != khuyenMai.MaKhuyenMai) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (khuyenMai.NgayBatDau > khuyenMai.NgayKetThuc)
                    {
                        ModelState.AddModelError("", "Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
                        return View(khuyenMai);
                    }

                    _context.Update(khuyenMai);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật mã khuyến mãi thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhuyenMaiExists(khuyenMai.MaKhuyenMai)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(khuyenMai);
        }

        // POST: Admin/AdminKhuyenMai/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã khuyến mãi." });
            }

            // Kiểm tra xem mã đã được sử dụng chưa
            bool isUsed = await _context.DatVes.AnyAsync(d => d.MaKhuyenMai == id);
            if (isUsed)
            {
                return Json(new { success = false, message = "Mã này đã được sử dụng trong các đơn đặt vé, không thể xóa." });
            }

            _context.KhuyenMais.Remove(khuyenMai);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa mã khuyến mãi thành công!" });
        }

        private bool KhuyenMaiExists(int id)
        {
            return _context.KhuyenMais.Any(e => e.MaKhuyenMai == id);
        }
    }
}
