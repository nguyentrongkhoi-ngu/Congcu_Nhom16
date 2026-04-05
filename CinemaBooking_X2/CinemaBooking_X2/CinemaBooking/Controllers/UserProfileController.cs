using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CinemaBooking.Models;
using CinemaBooking.Data;
using CinemaBooking.Models.ViewModels;
using CinemaBooking.Extensions;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "User")]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public UserProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var userIdInt = await User.GetLegacyUserIdAsync(_context);
            if (!userIdInt.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.NguoiDungs
                .Include(u => u.DatVes)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == userIdInt.Value);
            
            if (user == null)
            {
                return NotFound();
            }

            // Tính toán thống kê
            ViewBag.TotalTickets = await _context.DatVes.CountAsync(d => d.MaNguoiDung == user.MaNguoiDung);
            ViewBag.TotalSpent = await _context.DatVes
                .Where(d => d.MaNguoiDung == user.MaNguoiDung && d.TrangThai == "Đã thanh toán")
                .SumAsync(d => d.TongTien);
            
            // Xếp hạng thành viên (Logic 2026)
            string rank = "Đồng";
            decimal discountPercent = 0;
            decimal totalSpent = (decimal)ViewBag.TotalSpent;
            if (totalSpent >= 10000000) { rank = "Kim Cương"; discountPercent = 15; }
            else if (totalSpent >= 5000000) { rank = "Vàng"; discountPercent = 10; }
            else if (totalSpent >= 1000000) { rank = "Bạc"; discountPercent = 5; }
            
            ViewBag.MemberRank = rank;
            ViewBag.DiscountPercent = discountPercent;

            // Thiết lập vai trò người dùng
            ViewBag.Roles = new List<string>();
            if (user.MaVaiTro.HasValue)
            {
                if (user.MaVaiTro == 1)
                {
                    ViewBag.Roles.Add("Admin");
                }
                else if (user.MaVaiTro == 2)
                {
                    ViewBag.Roles.Add("Thành viên");
                }
            }

            return View(user);
        }

        public async Task<IActionResult> Edit()
        {
            var userIdInt = await User.GetLegacyUserIdAsync(_context);
            if (!userIdInt.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userIdInt.Value);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserProfileViewModel
            {
                HoTen = user.HoTen,
                Email = user.Email,
                PhoneNumber = user.SoDienThoai,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdInt = await User.GetLegacyUserIdAsync(_context);
            if (!userIdInt.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userIdInt.Value);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra trùng email
            if (user.Email != model.Email)
            {
                var existingUser = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == model.Email && u.MaNguoiDung != userIdInt.Value);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }
            }

            // Cập nhật thông tin
            user.HoTen = model.HoTen;
            user.Email = model.Email;
            user.SoDienThoai = model.PhoneNumber;

            // Xử lý Upload Avatar
            if (model.AvatarFile != null)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder)) 
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Xóa ảnh cũ nếu là ảnh local (trong thư mục /uploads/)
                if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
                {
                    string oldPath = Path.Combine(_hostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(fileStream);
                }
                
                user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
            }
            else
            {
                // Nếu không upload file, cập nhật AvatarUrl từ input link
                // Nếu AvatarUrl mới khác AvatarUrl cũ và AvatarUrl cũ là ảnh local, hãy xóa nó
                if (user.AvatarUrl != model.AvatarUrl && !string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
                {
                    string oldPath = Path.Combine(_hostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
                user.AvatarUrl = model.AvatarUrl;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdInt = await User.GetLegacyUserIdAsync(_context);
            if (!userIdInt.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userIdInt.Value);
            if (user == null)
            {
                return NotFound();
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MatKhau))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác");
                return View(model);
            }

            // Set new password
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}