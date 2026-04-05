using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using System.Security.Claims;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var users = await _userManager.Users
                .OrderByDescending(u => u.NgayTao)
                .ToListAsync();

            var userViewModels = new List<object>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new
                {
                    User = user,
                    Roles = userRoles
                });
            }

            ViewBag.AllRoles = roles;
            ViewBag.Users = userViewModels;
            return View();
        }

        // GET: Admin/Users/GetUser/5 (For Modal)
        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Json(new { 
                id = user.Id, 
                userName = user.UserName, 
                email = user.Email, 
                hoTen = user.HoTen, 
                soDienThoai = user.SoDienThoai,
                roles = roles
            });
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string UserName, string Email, string Password, string HoTen, string SoDienThoai, string Role)
        {
            var user = new ApplicationUser
            {
                UserName = UserName,
                Email = Email,
                HoTen = HoTen,
                SoDienThoai = SoDienThoai,
                NgayTao = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(Role))
                {
                    await _userManager.AddToRoleAsync(user, Role);
                }
                TempData["SuccessMessage"] = "Thêm người dùng mới thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string HoTen, string SoDienThoai, string Email, string Role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Special protection for central 'admin' username
            if (user.UserName.ToLower() == "admin" && (string.IsNullOrEmpty(Role) || Role != "Admin"))
            {
                TempData["ErrorMessage"] = "Không thể gỡ bỏ vai trò Quản trị viên của tài khoản Admin hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            user.HoTen = HoTen;
            user.SoDienThoai = SoDienThoai;
            user.Email = Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update Roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                
                // Only update roles if different
                if (!currentRoles.Contains(Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(Role))
                    {
                        await _userManager.AddToRoleAsync(user, Role);
                    }
                }
                
                TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Self-deletion protection
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            // System 'admin' protection
            if (user.UserName.ToLower() == "admin")
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã xóa người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
