using Microsoft.AspNetCore.Identity;
using CinemaBooking.Models;

namespace CinemaBooking.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context)
        {
            // Seed roles
            await SeedRolesAsync(roleManager);
            
            // Seed admin user
            await SeedAdminUserAsync(userManager);

            // Seed test user
            await SeedUserTestAsync(userManager, context);
            
            // Migrate existing users from NguoiDung to Identity
            await MigrateExistingUsersAsync(userManager, context);
        }

        private static async Task SeedUserTestAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var userEmail = "user@test.com";
            var user = await userManager.FindByEmailAsync(userEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "usertest",
                    Email = userEmail,
                    EmailConfirmed = true,
                    HoTen = "User Test",
                    SoDienThoai = "0987654321",
                    NgayTao = DateTime.Now
                };

                // Lưu ý: Password phải thỏa mãn chính sách bảo mật trong Program.cs
                var result = await userManager.CreateAsync(user, "123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            // ĐẢM BẢO LUÔN CÓ BẢN GHI Ở BẢNG NGUOIDUNGS (LEGACY) CHO USER TEST
            if (!context.NguoiDungs.Any(n => n.Email == userEmail))
            {
                var userRole = context.VaiTros.FirstOrDefault(v => v.TenVaiTro == "User");
                var legacyUser = new NguoiDung
                {
                    TenDangNhap = "usertest",
                    MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Email = userEmail,
                    HoTen = "User Test",
                    SoDienThoai = "0987654321",
                    NgayTao = DateTime.Now,
                    MaVaiTro = userRole?.MaVaiTro ?? 2
                };
                context.NguoiDungs.Add(legacyUser);
                await context.SaveChangesAsync();
                Console.WriteLine($"Synchronized legacy user for: {userEmail}");
            }
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Admin" },
                new ApplicationRole { Name = "User" }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@cinezore.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    HoTen = "Quản trị viên",
                    SoDienThoai = "0123456789",
                    NgayTao = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Ensure admin user has admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task MigrateExistingUsersAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            var existingUsers = context.NguoiDungs
                .Where(u => !context.Users.Any(au => au.Email == u.Email))
                .ToList();

            foreach (var nguoiDung in existingUsers)
            {
                var applicationUser = new ApplicationUser
                {
                    UserName = nguoiDung.TenDangNhap,
                    Email = nguoiDung.Email,
                    EmailConfirmed = true,
                    HoTen = nguoiDung.HoTen,
                    SoDienThoai = nguoiDung.SoDienThoai,
                    NgayTao = nguoiDung.NgayTao ?? DateTime.Now
                };

                // Create user with a temporary password (they'll need to reset it)
                var result = await userManager.CreateAsync(applicationUser, "TempPassword123!");
                
                if (result.Succeeded)
                {
                    // Assign role based on MaVaiTro
                    var roleName = nguoiDung.MaVaiTro == 1 ? "Admin" : "User";
                    await userManager.AddToRoleAsync(applicationUser, roleName);

                    // Update related records to link to the new Identity user
                    await UpdateRelatedRecords(context, nguoiDung.MaNguoiDung, applicationUser.Id);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task UpdateRelatedRecords(
            ApplicationDbContext context,
            int oldUserId,
            string newUserId)
        {
            // Update DatVe records
            var datVes = context.DatVes.Where(d => d.MaNguoiDung == oldUserId).ToList();
            foreach (var datVe in datVes)
            {
                datVe.UserId = newUserId;
            }

            // Update DanhGia records
            var danhGias = context.DanhGias.Where(d => d.MaNguoiDung == oldUserId).ToList();
            foreach (var danhGia in danhGias)
            {
                danhGia.UserId = newUserId;
            }

            // Update LichSuGiaoDich records
            var lichSuGiaoDichs = context.LichSuGiaoDiches.Where(l => l.MaNguoiDung == oldUserId).ToList();
            foreach (var lichSu in lichSuGiaoDichs)
            {
                lichSu.UserId = newUserId;
            }

            // Update OtpInfo records
            var otpInfos = context.OtpInfos.Where(o => o.MaNguoiDung == oldUserId).ToList();
            foreach (var otp in otpInfos)
            {
                otp.UserId = newUserId;
            }
        }
    }
}
