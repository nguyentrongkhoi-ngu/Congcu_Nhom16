using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CinemaBooking.Extensions;

namespace CinemaBooking.Controllers
{
    public class DanhGiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DanhGiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DanhGia/CreateDanhGia/5
        [Authorize]
        public async Task<IActionResult> CreateDanhGia(int id)
        {
            var phim = await _context.Phims
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng đã đánh giá phim này chưa
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingDanhGia = await _context.DanhGias
                .FirstOrDefaultAsync(d => d.MaPhim == id && d.MaNguoiDung == userId.Value);

            var viewModel = new DanhGiaViewModel
            {
                MaPhim = phim.MaPhim,
                TenPhim = phim.TenPhim,
                UrlPoster = phim.UrlPoster
            };

            if (existingDanhGia != null)
            {
                // Nếu đã đánh giá rồi, hiển thị thông tin đánh giá cũ
                viewModel.DiemSo = existingDanhGia.DiemSo ?? 0;
                viewModel.BinhLuan = existingDanhGia.BinhLuan;
                TempData["DanhGiaMessage"] = "Bạn đã đánh giá phim này trước đó. Bạn có thể cập nhật đánh giá.";
            }

            return View(viewModel);
        }

        // POST: DanhGia/CreateDanhGia
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateDanhGia(DanhGiaViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var phim = await _context.Phims.FindAsync(viewModel.MaPhim);
                viewModel.TenPhim = phim.TenPhim;
                viewModel.UrlPoster = phim.UrlPoster;
                return View(viewModel);
            }

            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra xem đã đánh giá chưa
            var existingDanhGia = await _context.DanhGias
                .FirstOrDefaultAsync(d => d.MaPhim == viewModel.MaPhim && d.MaNguoiDung == userId.Value);

            if (existingDanhGia != null)
            {
                // Cập nhật đánh giá cũ
                existingDanhGia.DiemSo = viewModel.DiemSo;
                existingDanhGia.BinhLuan = viewModel.BinhLuan;
                existingDanhGia.NgayDanhGia = DateTime.Now;
                
                _context.Update(existingDanhGia);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cập nhật đánh giá thành công!";
            }
            else
            {
                // Tạo đánh giá mới
                var danhGia = new DanhGia
                {
                    MaNguoiDung = userId.Value,
                    MaPhim = viewModel.MaPhim,
                    DiemSo = viewModel.DiemSo,
                    BinhLuan = viewModel.BinhLuan,
                    NgayDanhGia = DateTime.Now
                };
                
                _context.Add(danhGia);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Đánh giá phim thành công!";
            }

            // Chuyển hướng đến trang chi tiết phim
            return RedirectToAction("Detail", "Phim", new { id = viewModel.MaPhim });
        }

        // GET: DanhGia/DanhSachDanhGia/5
        public async Task<IActionResult> DanhSachDanhGia(int id)
        {
            var phim = await _context.Phims
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            var danhGias = await _context.DanhGias
                .Include(d => d.NguoiDung)
                .Where(d => d.MaPhim == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToListAsync();

            var diemTrungBinh = 0.0;
            if (danhGias.Any())
            {
                diemTrungBinh = danhGias.Average(d => d.DiemSo ?? 0);
            }

            var viewModel = new PhimDanhGiaViewModel
            {
                Phim = phim,
                DanhSachDanhGia = danhGias,
                DiemTrungBinh = Math.Round(diemTrungBinh, 1),
                TongSoDanhGia = danhGias.Count
            };

            // Thêm thông tin legacy users để view có thể kiểm tra quyền
            ViewBag.LegacyUsers = await _context.NguoiDungs.ToListAsync();

            return View(viewModel);
        }

        // POST: DanhGia/XoaDanhGia/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> XoaDanhGia(int id)
        {
            var danhGia = await _context.DanhGias.FindAsync(id);
            
            if (danhGia == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xóa (chỉ người tạo đánh giá hoặc admin mới có quyền xóa)
            var userId = await User.GetLegacyUserIdAsync(_context);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("Admin");

            if (danhGia.MaNguoiDung != userId.Value && !isAdmin)
            {
                return Forbid();
            }

            var maPhim = danhGia.MaPhim;
            
            _context.DanhGias.Remove(danhGia);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Xóa đánh giá thành công!";
            
            return RedirectToAction("DanhSachDanhGia", new { id = maPhim });
        }
    }
} 