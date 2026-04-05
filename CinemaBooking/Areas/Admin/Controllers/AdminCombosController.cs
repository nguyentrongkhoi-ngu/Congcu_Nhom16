using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;
using CinemaBooking.Data;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminCombosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminCombosController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/AdminCombos
        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .Include(c => c.DatVeCombos)
                .OrderByDescending(c => c.UuTien)
                .ThenBy(c => c.Gia)
                .ToListAsync();

            ViewBag.SoldCounts = new Dictionary<int, int>();
            return View(combos);
        }

        // GET: Admin/AdminCombos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminCombos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenCombo,MoTa,Gia,HinhAnh,ImageFile,Loai,TrangThai,SoLuongTon,UuTien,Nhan,KichThuoc")] Combo combo)
        {
            if (ModelState.IsValid)
            {
                if (combo.ImageFile != null && combo.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "combos");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(combo.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await combo.ImageFile.CopyToAsync(stream);
                    }
                    combo.HinhAnh = "/uploads/combos/" + fileName;
                }

                _context.Add(combo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm Combo mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(combo);
        }

        // GET: Admin/AdminCombos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var combo = await _context.Combos.FindAsync(id);
            if (combo == null) return NotFound();

            return View(combo);
        }

        // POST: Admin/AdminCombos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaCombo,TenCombo,MoTa,Gia,HinhAnh,ImageFile,Loai,TrangThai,SoLuongTon,UuTien,Nhan,KichThuoc")] Combo combo)
        {
            if (id != combo.MaCombo) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (combo.ImageFile != null && combo.ImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "combos");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(combo.ImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await combo.ImageFile.CopyToAsync(stream);
                        }
                        combo.HinhAnh = "/uploads/combos/" + fileName;
                    }
                    // Else: if no new file, it will keep the HinhAnh from the hidden input or original bind

                    _context.Update(combo);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật Combo thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComboExists(combo.MaCombo)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(combo);
        }

        // POST: Admin/AdminCombos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
            {
                return Json(new { success = false, message = "Không tìm thấy Combo." });
            }

            // Kiểm tra xem combo đã được đặt chưa
            bool isUsed = await _context.DatVeCombos.AnyAsync(d => d.MaCombo == id);
            if (isUsed)
            {
                return Json(new { success = false, message = "Combo này đã được sử dụng trong các đơn đặt vé, không thể xóa." });
            }

            _context.Combos.Remove(combo);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa Combo thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null) return Json(new { success = false });

            combo.TrangThai = !combo.TrangThai;
            await _context.SaveChangesAsync();
            return Json(new { success = true, newState = combo.TrangThai });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int amount)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null) return Json(new { success = false });

            combo.SoLuongTon = amount;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private bool ComboExists(int id)
        {
            return _context.Combos.Any(e => e.MaCombo == id);
        }
    }
}
