using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LichChieuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichChieuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/LichChieu
        public async Task<IActionResult> Index(LichChieuListViewModel viewModel = null)
        {
            if (viewModel == null)
            {
                viewModel = new LichChieuListViewModel();
            }

            // Lấy danh sách phim và rạp cho dropdown filter
            viewModel.PhimList = new SelectList(await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), "MaPhim", "TenPhim");
            viewModel.RapList = new SelectList(await _context.RapPhims.OrderBy(r => r.TenRap).ToListAsync(), "MaRap", "TenRap");

            // Truy vấn cơ bản
            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .AsQueryable();

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(viewModel.SearchTerm))
            {
                query = query.Where(l => l.Phim.TenPhim.Contains(viewModel.SearchTerm) || 
                                        l.PhongChieu.RapPhim.TenRap.Contains(viewModel.SearchTerm));
            }

            if (viewModel.NgayBatDau.HasValue)
            {
                query = query.Where(l => l.NgayChieu >= viewModel.NgayBatDau.Value);
            }

            if (viewModel.NgayKetThuc.HasValue)
            {
                query = query.Where(l => l.NgayChieu <= viewModel.NgayKetThuc.Value);
            }

            if (viewModel.MaPhim.HasValue)
            {
                query = query.Where(l => l.MaPhim == viewModel.MaPhim.Value);
            }

            if (viewModel.MaRap.HasValue)
            {
                query = query.Where(l => l.PhongChieu.MaRap == viewModel.MaRap.Value);
            }

            // Sắp xếp
            query = query.OrderByDescending(l => l.NgayChieu).ThenByDescending(l => l.GioChieu);

            // Gán kết quả cho view model
            viewModel.LichChieus = await query.ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/LichChieu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);

            if (lichChieu == null) return NotFound();

            return View(lichChieu);
        }

        // GET: Admin/LichChieu/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new LichChieuViewModel
            {
                NgayChieu = DateTime.Today,
                GioChieu = new TimeSpan(9, 0, 0),
                GiaVe = 100000
            };
            
            await LoadRelatedData(viewModel);
            return View(viewModel);
        }
        
        // POST: Admin/LichChieu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LichChieuViewModel viewModel)
        {
            try
            {
                var trungLich = await CheckTrungLich(viewModel);
                if (trungLich)
                {
                    ModelState.AddModelError("", "Lịch chiếu bị trùng với lịch chiếu khác trong cùng phòng");
                    await LoadRelatedData(viewModel);
                    return View(viewModel);
                }
                
                var lichChieu = new LichChieu
                {
                    MaPhim = viewModel.MaPhim,
                    MaPhong = viewModel.MaPhong,
                    NgayChieu = viewModel.NgayChieu,
                    GioChieu = viewModel.GioChieu,
                    GiaVe = viewModel.GiaVe,
                    MaNgonNgu = viewModel.MaNgonNgu
                };
                
                _context.LichChieus.Add(lichChieu);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Tạo lịch chiếu mới thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi lưu lịch chiếu: {ex.Message}";
                await LoadRelatedData(viewModel);
                return View(viewModel);
            }
        }

        // GET: Admin/LichChieu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lichChieu = await _context.LichChieus.FindAsync(id);
            if (lichChieu == null) return NotFound();

            var viewModel = new LichChieuViewModel
            {
                MaLichChieu = lichChieu.MaLichChieu,
                MaPhim = lichChieu.MaPhim,
                MaPhong = lichChieu.MaPhong,
                NgayChieu = lichChieu.NgayChieu,
                GioChieu = lichChieu.GioChieu,
                GiaVe = lichChieu.GiaVe,
                MaNgonNgu = lichChieu.MaNgonNgu
            };

            await LoadRelatedData(viewModel);
            return View(viewModel);
        }

        // POST: Admin/LichChieu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LichChieuViewModel viewModel)
        {
            if (id != viewModel.MaLichChieu) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var trungLich = await CheckTrungLich(viewModel, id);
                    if (trungLich)
                    {
                        ModelState.AddModelError("", "Lịch chiếu bị trùng với lịch chiếu khác trong cùng phòng");
                        await LoadRelatedData(viewModel);
                        return View(viewModel);
                    }

                    var lichChieu = await _context.LichChieus.FindAsync(id);
                    if (lichChieu == null) return NotFound();

                    lichChieu.MaPhim = viewModel.MaPhim;
                    lichChieu.MaPhong = viewModel.MaPhong;
                    lichChieu.NgayChieu = viewModel.NgayChieu;
                    lichChieu.GioChieu = viewModel.GioChieu;
                    lichChieu.GiaVe = viewModel.GiaVe;
                    lichChieu.MaNgonNgu = viewModel.MaNgonNgu;

                    _context.Update(lichChieu);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật lịch chiếu thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LichChieuExists(viewModel.MaLichChieu)) return NotFound();
                    else throw;
                }
            }
            
            await LoadRelatedData(viewModel);
            return View(viewModel);
        }

        // GET: Admin/LichChieu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var lichChieu = await _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(m => m.MaLichChieu == id);
                
            if (lichChieu == null) return NotFound();

            return View(lichChieu);
        }

        // POST: Admin/LichChieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichChieu = await _context.LichChieus.FindAsync(id);
            if (lichChieu == null) return NotFound();

            var existingDatVe = await _context.DatVes.AnyAsync(d => d.MaLichChieu == id);
            if (existingDatVe)
            {
                TempData["ErrorMessage"] = "Không thể xóa lịch chiếu vì đã có người đặt vé";
                return RedirectToAction(nameof(Index));
            }

            _context.LichChieus.Remove(lichChieu);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa lịch chiếu thành công";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRelatedData(LichChieuViewModel viewModel)
        {
            viewModel.PhimList = new SelectList(await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), "MaPhim", "TenPhim");
            
            var phongChieus = await _context.PhongChieus
                .Include(p => p.RapPhim)
                .OrderBy(p => p.RapPhim.TenRap)
                .ThenBy(p => p.SoPhong)
                .Select(p => new {
                    p.MaPhong,
                    TenPhong = $"Phòng {p.SoPhong} - {p.RapPhim.TenRap}"
                })
                .ToListAsync();
            viewModel.PhongList = new SelectList(phongChieus, "MaPhong", "TenPhong");
            
            var ngonNguList = await _context.NgonNguPhims
                .OrderBy(n => n.NgonNgu)
                .Select(n => new { 
                    n.MaNgonNgu, 
                    TenNgonNgu = $"{n.NgonNgu} ({n.PhuDe})" 
                })
                .ToListAsync();
            viewModel.NgonNguList = new SelectList(ngonNguList, "MaNgonNgu", "TenNgonNgu");
        }

        private async Task<bool> CheckTrungLich(LichChieuViewModel viewModel, int? excludeId = null)
        {
            var phim = await _context.Phims.FindAsync(viewModel.MaPhim);
            if (phim == null) return false;

            TimeSpan thoiLuongPhim = TimeSpan.FromMinutes(phim.ThoiLuong);
            DateTime thoiGianBatDau = viewModel.NgayChieu.Add(viewModel.GioChieu);
            DateTime thoiGianKetThuc = thoiGianBatDau.Add(thoiLuongPhim).AddMinutes(30);

            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Where(l => l.MaPhong == viewModel.MaPhong && l.NgayChieu == viewModel.NgayChieu);

            if (excludeId.HasValue) query = query.Where(l => l.MaLichChieu != excludeId.Value);

            var lichChieuTrung = await query.ToListAsync();

            foreach (var lc in lichChieuTrung)
            {
                TimeSpan thoiLuongLC = TimeSpan.FromMinutes(lc.Phim.ThoiLuong);
                DateTime lcBatDau = lc.NgayChieu.Add(lc.GioChieu);
                DateTime lcKetThuc = lcBatDau.Add(thoiLuongLC).AddMinutes(30);

                if ((thoiGianBatDau >= lcBatDau && thoiGianBatDau < lcKetThuc) ||
                    (thoiGianKetThuc > lcBatDau && thoiGianKetThuc <= lcKetThuc) ||
                    (thoiGianBatDau <= lcBatDau && thoiGianKetThuc >= lcKetThuc))
                {
                    return true;
                }
            }
            return false;
        }

        private bool LichChieuExists(int id)
        {
            return _context.LichChieus.Any(e => e.MaLichChieu == id);
        }
    }
}
