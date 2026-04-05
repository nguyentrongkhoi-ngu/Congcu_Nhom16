using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using CinemaBooking.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Google;

namespace CinemaBooking.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<NguoiDung> _passwordHasher;
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<NguoiDung> passwordHasher,
            OtpService otpService,
            EmailService emailService,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _otpService = otpService;
            _emailService = emailService;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Account/Login
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                return Redirect(await GetRedirectUrl(user, returnUrl));
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            Console.WriteLine($"=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Identifier: {model.TenDangNhap}");

            if (ModelState.IsValid)
            {
                var identifier = model.TenDangNhap;

                // First try to login with Identity system
                var identityUser = await _userManager.FindByNameAsync(identifier) ??
                                  await _userManager.FindByEmailAsync(identifier);

                Console.WriteLine($"Identity user found: {identityUser != null}");

                if (identityUser != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        identityUser.UserName,
                        model.MatKhau,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    Console.WriteLine($"Identity login result: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        Console.WriteLine("Identity login successful");
                        
                        // Sync legacy ID to claims if needed
                        var claims = await _userManager.GetClaimsAsync(identityUser);
                        if (!claims.Any(c => c.Type == "MaNguoiDung"))
                        {
                            var legacyUserRecord = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == identityUser.Email);
                            if (legacyUserRecord != null)
                            {
                                await _userManager.AddClaimAsync(identityUser, new System.Security.Claims.Claim("MaNguoiDung", legacyUserRecord.MaNguoiDung.ToString()));
                            }
                        }
                        
                        return Redirect(await GetRedirectUrl(identityUser));
                    }
                }

                // Fallback to legacy system
                var legacyUser = await _context.NguoiDungs
                    .FirstOrDefaultAsync(u => u.TenDangNhap == identifier || u.Email == identifier);

                Console.WriteLine($"Legacy user found: {legacyUser != null}");

                if (legacyUser != null)
                {
                    Console.WriteLine($"Legacy user: {legacyUser.TenDangNhap}, Email: {legacyUser.Email}");

                    // Try BCrypt first
                    bool bcryptValid = false;
                    try
                    {
                        bcryptValid = BCrypt.Net.BCrypt.Verify(model.MatKhau, legacyUser.MatKhau);
                        Console.WriteLine($"BCrypt verification: {bcryptValid}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"BCrypt verification failed: {ex.Message}");
                    }

                    // Try SHA256 as fallback
                    bool sha256Valid = false;
                    try
                    {
                        sha256Valid = Services.PasswordHasher.VerifyPassword(model.MatKhau, legacyUser.MatKhau);
                        Console.WriteLine($"SHA256 verification: {sha256Valid}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SHA256 verification failed: {ex.Message}");
                    }

                    if (bcryptValid || sha256Valid)
                    {
                        Console.WriteLine("Legacy password verification successful");

                        // If password was SHA256, update to BCrypt
                        if (sha256Valid && !bcryptValid)
                        {
                            Console.WriteLine("Updating password from SHA256 to BCrypt");
                            legacyUser.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);
                            _context.NguoiDungs.Update(legacyUser);
                            await _context.SaveChangesAsync();
                        }

                        // Try to find corresponding Identity user
                        var existingIdentityUser = await _userManager.FindByEmailAsync(legacyUser.Email);

                        if (existingIdentityUser != null)
                        {
                            Console.WriteLine("Signing in with existing Identity user");
                            // Sign in with Identity
                            await _signInManager.SignInAsync(existingIdentityUser, model.RememberMe);
                            return Redirect(await GetRedirectUrl(existingIdentityUser));
                        }
                        else
                        {
                            Console.WriteLine("Creating new Identity user for legacy user");
                            // Create Identity user for legacy user
                            var newIdentityUser = new ApplicationUser
                            {
                                UserName = legacyUser.TenDangNhap,
                                Email = legacyUser.Email,
                                EmailConfirmed = true,
                                HoTen = legacyUser.HoTen,
                                SoDienThoai = legacyUser.SoDienThoai,
                                NgayTao = legacyUser.NgayTao ?? DateTime.Now
                            };

                            var createResult = await _userManager.CreateAsync(newIdentityUser, model.MatKhau);
                            if (createResult.Succeeded)
                            {
                                Console.WriteLine("Identity user created successfully");
                                // Assign role based on legacy role
                                var roleName = legacyUser.MaVaiTro == 1 ? "Admin" : "User";
                                await _userManager.AddToRoleAsync(newIdentityUser, roleName);

                                // Sign in with new Identity user
                                await _signInManager.SignInAsync(newIdentityUser, model.RememberMe);
                                return Redirect(await GetRedirectUrl(newIdentityUser));
                            }
                            else
                            {
                                Console.WriteLine($"Failed to create Identity user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Password verification failed for both BCrypt and SHA256");
                    }
                }
                else
                {
                    Console.WriteLine("No legacy user found");
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không chính xác");
            }
            else
            {
                Console.WriteLine("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            Console.WriteLine("=== LOGIN FAILED ===");
            return View(model);
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: Account/Register - Xử lý bước 1: Gửi OTP hoặc bước 2: Xác thực OTP và đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            Console.WriteLine($"Register called. BuocNhapOTP: {model.BuocNhapOTP}, DaGuiOTP: {model.DaGuiOTP}, MaXacThuc: {(string.IsNullOrEmpty(model.MaXacThuc) ? "empty" : model.MaXacThuc)}");
            Console.WriteLine($"MatKhau: {(string.IsNullOrEmpty(model.MatKhau) ? "NULL" : "FILLED")}");
            Console.WriteLine($"TenDangNhap: {model.TenDangNhap}, Email: {model.Email}");

            // Kiểm tra nếu có OTP nhập vào, coi như đang ở bước xác thực OTP
            if (!string.IsNullOrEmpty(model.MaXacThuc))
            {
                model.BuocNhapOTP = true;
            }

            // Bước xác thực OTP và đăng ký
            if (model.BuocNhapOTP && !string.IsNullOrEmpty(model.MaXacThuc))
            {
                Console.WriteLine($"Đang xác thực OTP: {model.MaXacThuc} cho email {model.Email}");

                // Lấy lại thông tin từ Session nếu cần
                if (string.IsNullOrEmpty(model.Email))
                {
                    model.Email = HttpContext.Session.GetString("RegisterEmail");
                }

                // Kiểm tra OTP có hợp lệ không
                bool otpValid = await _otpService.VerifyOtp(model.Email, model.MaXacThuc, "DangKy");

                Console.WriteLine($"Kết quả xác thực OTP: {otpValid}");

                if (!otpValid)
                {
                    model.BuocNhapOTP = true;
                    ModelState.AddModelError("MaXacThuc", "Mã xác thực không hợp lệ hoặc đã hết hạn");

                    // Phục hồi thông tin từ session
                    model.TenDangNhap = HttpContext.Session.GetString("RegisterTenDangNhap");
                    model.Email = HttpContext.Session.GetString("RegisterEmail");
                    model.HoTen = HttpContext.Session.GetString("RegisterHoTen");
                    model.SoDienThoai = HttpContext.Session.GetString("RegisterSoDienThoai");
                    model.DaGuiOTP = true;

                    return View(model);
                }

                // Phục hồi các thông tin cần thiết từ Session
                var password = HttpContext.Session.GetString("RegisterPassword");
                var tenDangNhap = HttpContext.Session.GetString("RegisterTenDangNhap");
                var email = HttpContext.Session.GetString("RegisterEmail");
                var hoTen = HttpContext.Session.GetString("RegisterHoTen");
                var soDienThoai = HttpContext.Session.GetString("RegisterSoDienThoai");

                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError(string.Empty, "Phiên đăng ký đã hết hạn, vui lòng thử lại");
                    return RedirectToAction("Register");
                }

                // OTP hợp lệ, tiến hành đăng ký
                if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == tenDangNhap))
                {
                    model.BuocNhapOTP = true;
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập đã tồn tại");
                    return View(model);
                }

                if (await _context.NguoiDungs.AnyAsync(u => u.Email == email))
                {
                    model.BuocNhapOTP = true;
                    ModelState.AddModelError(string.Empty, "Email đã được sử dụng");
                    return View(model);
                }

                var user = new NguoiDung
                {
                    TenDangNhap = tenDangNhap,
                    Email = email,
                    SoDienThoai = soDienThoai,
                    HoTen = hoTen,
                    NgayTao = DateTime.Now,
                    MaVaiTro = 2 // Vai trò mặc định là User
                };

                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(password);

                _context.NguoiDungs.Add(user);
                await _context.SaveChangesAsync();

                // Xóa thông tin mật khẩu khỏi session sau khi đăng ký thành công
                HttpContext.Session.Remove("RegisterPassword");
                HttpContext.Session.Remove("RegisterEmail");
                HttpContext.Session.Remove("RegisterTenDangNhap");
                HttpContext.Session.Remove("RegisterHoTen");
                HttpContext.Session.Remove("RegisterSoDienThoai");

                // Create Identity user for auto-login
                var identityUser = new ApplicationUser
                {
                    UserName = user.TenDangNhap,
                    Email = user.Email,
                    EmailConfirmed = true,
                    HoTen = user.HoTen,
                    SoDienThoai = user.SoDienThoai,
                    NgayTao = user.NgayTao
                };

                var createResult = await _userManager.CreateAsync(identityUser, password);
                if (createResult.Succeeded)
                {
                    // Assign User role
                    await _userManager.AddToRoleAsync(identityUser, "User");

                    // Auto sign-in with Identity
                    await _signInManager.SignInAsync(identityUser, isPersistent: false);
                }

                Console.WriteLine($"Đăng ký thành công cho người dùng: {user.TenDangNhap}");
                TempData["SuccessMessage"] = "Đăng ký thành công và đã đăng nhập tự động!";
                return Redirect(await GetRedirectUrl(identityUser));
            }

            // Bước hiển thị form OTP nếu ở trạng thái cần nhập OTP
            if (model.BuocNhapOTP)
            {
                // Phục hồi thông tin từ session để hiển thị lại form OTP
                if (string.IsNullOrEmpty(model.Email))
                {
                    model.Email = HttpContext.Session.GetString("RegisterEmail");
                    model.TenDangNhap = HttpContext.Session.GetString("RegisterTenDangNhap");
                    model.HoTen = HttpContext.Session.GetString("RegisterHoTen");
                    model.SoDienThoai = HttpContext.Session.GetString("RegisterSoDienThoai");
                }

                model.DaGuiOTP = true;
                Console.WriteLine("Đang ở bước nhập OTP, hiển thị lại form OTP");
                return View(model);
            }

            // Xóa lỗi validation của trường MaXacThuc nếu chưa phải bước nhập OTP
            if (!model.BuocNhapOTP && ModelState.ContainsKey("MaXacThuc"))
            {
                ModelState.Remove("MaXacThuc");
            }

            // Bước 1: Kiểm tra thông tin đăng ký và gửi OTP
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên đăng nhập
                if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == model.TenDangNhap))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập đã tồn tại");
                    return View(model);
                }

                // Kiểm tra trùng email
                if (await _context.NguoiDungs.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError(string.Empty, "Email đã được sử dụng");
                    return View(model);
                }

                try
                {
                    // Hủy các OTP cũ
                    await _otpService.InvalidateOldOtps(model.Email, "DangKy");

                    // Tạo và gửi OTP mới
                    var otp = await _otpService.CreateOtp(model.Email, "DangKy", model.HoTen);

                    // In ra log để kiểm tra
                    Console.WriteLine($"OTP đã được tạo: {otp} cho email {model.Email}");

                    // Lưu thông tin đăng ký vào Session
                    HttpContext.Session.SetString("RegisterPassword", model.MatKhau);
                    HttpContext.Session.SetString("RegisterEmail", model.Email);
                    HttpContext.Session.SetString("RegisterTenDangNhap", model.TenDangNhap);
                    HttpContext.Session.SetString("RegisterHoTen", model.HoTen ?? "");
                    HttpContext.Session.SetString("RegisterSoDienThoai", model.SoDienThoai ?? "");

                    Console.WriteLine("Đã lưu thông tin đăng ký vào Session");

                    // Tạo model mới để chuyển sang bước 2
                    var newModel = new RegisterViewModel
                    {
                        TenDangNhap = model.TenDangNhap,
                        Email = model.Email,
                        HoTen = model.HoTen,
                        SoDienThoai = model.SoDienThoai,
                        DaGuiOTP = true,
                        BuocNhapOTP = true
                    };

                    // Thông báo
                    TempData["OtpMessage"] = $"Mã xác thực đã được gửi đến email {model.Email}. Vui lòng kiểm tra hộp thư của bạn.";

                    Console.WriteLine("Đã cập nhật model với DaGuiOTP=true, BuocNhapOTP=true. Chuyển sang bước nhập OTP.");
                    return View(newModel);
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi
                    Console.WriteLine($"Lỗi khi gửi OTP: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Không thể gửi mã xác thực: {ex.Message}");
                    return View(model);
                }
            }

            // Đảm bảo hiển thị lỗi validation
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    Console.WriteLine($"Lỗi ở trường {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return View(model);
        }

        // GET: Account/ResendOtp
        [HttpGet]
        public async Task<IActionResult> ResendOtp(string email, string hoTen)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            // Hủy các OTP cũ
            await _otpService.InvalidateOldOtps(email, "DangKy");

            // Tạo và gửi OTP mới
            await _otpService.CreateOtp(email, "DangKy", hoTen);

            // Thông báo và chuyển hướng lại form đăng ký
            TempData["OtpMessage"] = $"Mã xác thực mới đã được gửi đến email {email}. Vui lòng kiểm tra hộp thư của bạn.";

            var model = new RegisterViewModel
            {
                Email = email,
                HoTen = hoTen,
                DaGuiOTP = true,
                BuocNhapOTP = true
            };

            return View("Register", model);
        }

        // POST/GET: Account/Logout
        [HttpPost, HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new ForgotPasswordViewModel());
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            // Log lại đầy đủ thông tin của model để debug
            Console.WriteLine($"=========== FORGOT PASSWORD FORM SUBMIT ===========");
            Console.WriteLine($"Buoc: {model.Buoc}");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"DaGuiOTP: {model.DaGuiOTP}");
            Console.WriteLine($"MaXacThuc: {model.MaXacThuc ?? "null"}");
            Console.WriteLine($"MatKhauMoi: {(string.IsNullOrEmpty(model.MatKhauMoi) ? "null" : "[HIDDEN]")}");
            Console.WriteLine($"XacNhanMatKhauMoi: {(string.IsNullOrEmpty(model.XacNhanMatKhauMoi) ? "null" : "[HIDDEN]")}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Error in field {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }
            Console.WriteLine($"====================================================");

            // BƯỚC 3: ĐẶT MẬT KHẨU MỚI
            if (model.Buoc == 3)
            {
                Console.WriteLine("Đang xử lý bước 3: Đặt mật khẩu mới");

                // Bỏ qua validation cho MaXacThuc ở bước 3
                ModelState.Remove("MaXacThuc");

                // Lấy email từ Session
                string email = HttpContext.Session.GetString("ForgotPasswordEmail");

                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError(string.Empty, "Phiên làm việc đã hết hạn, vui lòng thử lại từ đầu");
                    model.Buoc = 1;
                    return View(model);
                }

                // Đặt email từ session
                model.Email = email;

                // Kiểm tra xem OTP đã được xác thực chưa
                bool otpVerified = HttpContext.Session.GetString("ForgotPasswordOtpVerified") == "true";
                if (!otpVerified)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng xác thực OTP trước khi đặt mật khẩu mới");
                    model.Buoc = 2;
                    return View(model);
                }

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(model.MatKhauMoi))
                {
                    ModelState.AddModelError("MatKhauMoi", "Vui lòng nhập mật khẩu mới");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.XacNhanMatKhauMoi))
                {
                    ModelState.AddModelError("XacNhanMatKhauMoi", "Vui lòng xác nhận mật khẩu mới");
                    return View(model);
                }

                if (model.MatKhauMoi != model.XacNhanMatKhauMoi)
                {
                    ModelState.AddModelError("XacNhanMatKhauMoi", "Mật khẩu mới và xác nhận mật khẩu không khớp");
                    return View(model);
                }

                // Tìm người dùng
                var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với email này");
                    model.Buoc = 1;
                    return View(model);
                }

                // Cập nhật mật khẩu
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
                await _context.SaveChangesAsync();

                // Xóa Session
                HttpContext.Session.Remove("ForgotPasswordEmail");
                HttpContext.Session.Remove("ForgotPasswordOtpVerified");

                // Thông báo thành công
                TempData["SuccessMessage"] = "Mật khẩu đã được cập nhật thành công. Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            // BƯỚC 2: XÁC THỰC OTP
            if (model.Buoc == 2)
            {
                Console.WriteLine("Đang xử lý bước 2: Xác thực OTP");

                // Bỏ qua validation cho MatKhauMoi và XacNhanMatKhauMoi ở bước 2
                ModelState.Remove("MatKhauMoi");
                ModelState.Remove("XacNhanMatKhauMoi");

                // Lấy email từ Session hoặc model
                string email = HttpContext.Session.GetString("ForgotPasswordEmail");
                if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(model.Email))
                {
                    email = model.Email;
                    HttpContext.Session.SetString("ForgotPasswordEmail", email);
                }

                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError(string.Empty, "Phiên làm việc đã hết hạn, vui lòng thử lại từ đầu");
                    model.Buoc = 1;
                    return View(model);
                }

                // Đặt email
                model.Email = email;

                // Kiểm tra OTP
                if (string.IsNullOrEmpty(model.MaXacThuc))
                {
                    ModelState.AddModelError("MaXacThuc", "Vui lòng nhập mã xác thực");
                    return View(model);
                }

                // Xác thực OTP
                bool otpValid = await _otpService.VerifyOtp(email, model.MaXacThuc, "QuenMatKhau");

                if (!otpValid)
                {
                    ModelState.AddModelError("MaXacThuc", "Mã xác thực không hợp lệ hoặc đã hết hạn");
                    return View(model);
                }

                // Đánh dấu đã xác thực OTP thành công
                HttpContext.Session.SetString("ForgotPasswordOtpVerified", "true");

                Console.WriteLine("Xác thực OTP thành công, chuyển sang bước 3");

                // Chuyển sang bước 3 và hiển thị form nhập mật khẩu mới
                var newModel = new ForgotPasswordViewModel
                {
                    Email = email,
                    Buoc = 3,
                    DaGuiOTP = true
                };

                return View(newModel);
            }

            // BƯỚC 1: NHẬP EMAIL VÀ GỬI OTP
            if (model.Buoc == 1)
            {
                Console.WriteLine("Đang xử lý bước 1: Nhập email và gửi OTP");

                // Bỏ qua validation cho MaXacThuc, MatKhauMoi và XacNhanMatKhauMoi ở bước 1
                ModelState.Remove("MaXacThuc");
                ModelState.Remove("MatKhauMoi");
                ModelState.Remove("XacNhanMatKhauMoi");

                if (string.IsNullOrEmpty(model.Email))
                {
                    ModelState.AddModelError("Email", "Vui lòng nhập email");
                    return View(model);
                }

                // Kiểm tra email có tồn tại trong hệ thống không
                var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email không tồn tại trong hệ thống");
                    return View(model);
                }

                try
                {
                    // Hủy các OTP cũ
                    await _otpService.InvalidateOldOtps(model.Email, "QuenMatKhau");

                    // Tạo và gửi OTP mới
                    var otp = await _otpService.CreateOtp(model.Email, "QuenMatKhau", user.HoTen);

                    // Lưu email vào Session
                    HttpContext.Session.SetString("ForgotPasswordEmail", model.Email);

                    // Xóa session xác thực OTP cũ nếu có
                    HttpContext.Session.Remove("ForgotPasswordOtpVerified");

                    // Tạo model mới cho bước 2
                    var newModel = new ForgotPasswordViewModel
                    {
                        Email = model.Email,
                        DaGuiOTP = true,
                        Buoc = 2
                    };

                    TempData["OtpMessage"] = $"Mã xác thực đã được gửi đến email {model.Email}. Vui lòng kiểm tra hộp thư của bạn.";

                    Console.WriteLine("Đã gửi OTP, chuyển sang bước 2");
                    return View(newModel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi gửi OTP: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Không thể gửi mã xác thực: {ex.Message}");
                    return View(model);
                }
            }

            // Trường hợp không xác định được bước
            Console.WriteLine($"Không xác định được bước xử lý, model.Buoc = {model.Buoc}");
            model.Buoc = 1;
            return View(model);
        }

        // GET: Account/ResendForgotPasswordOtp
        [HttpGet]
        public async Task<IActionResult> ResendForgotPasswordOtp()
        {
            string email = HttpContext.Session.GetString("ForgotPasswordEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Tìm thông tin người dùng
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            string hoTen = user?.HoTen;

            // Hủy các OTP cũ
            await _otpService.InvalidateOldOtps(email, "QuenMatKhau");

            // Tạo và gửi OTP mới
            await _otpService.CreateOtp(email, "QuenMatKhau", hoTen);

            // Đánh dấu OTP chưa được xác thực
            HttpContext.Session.Remove("ForgotPasswordOtpVerified");

            // Thông báo và chuyển hướng lại form
            TempData["OtpMessage"] = $"Mã xác thực mới đã được gửi đến email {email}. Vui lòng kiểm tra hộp thư của bạn.";

            var model = new ForgotPasswordViewModel
            {
                Email = email,
                DaGuiOTP = true,
                Buoc = 2
            };

            return View("ForgotPassword", model);
        }

        // Đăng nhập bằng Google
        [HttpGet]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", new { returnUrl }) };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync("Identity.External");
            if (!result.Succeeded)
                return RedirectToAction("Login");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            // Check if Identity user exists
            var identityUser = await _userManager.FindByEmailAsync(email);

            if (identityUser == null)
            {
                // Create new Identity user
                identityUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    HoTen = name,
                    NgayTao = DateTime.Now
                };

                var createResult = await _userManager.CreateAsync(identityUser);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(identityUser, "User");

                    // Also create legacy user for compatibility
                    var legacyUser = new NguoiDung
                    {
                        TenDangNhap = email,
                        Email = email,
                        HoTen = name,
                        NgayTao = DateTime.Now,
                        MaVaiTro = 2, // User thường
                        MatKhau = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                    };
                    _context.NguoiDungs.Add(legacyUser);
                    await _context.SaveChangesAsync();
                }
            }

            // Sign in with Identity
            await _signInManager.SignInAsync(identityUser, isPersistent: true);

            return Redirect(await GetRedirectUrl(identityUser, returnUrl));
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<string> GetRedirectUrl(ApplicationUser user, string returnUrl = null)
        {
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                // Đối với Admin, chúng ta ưu tiên trang Dashboard của Admin
                // trừ khi họ đang cố truy cập một trang Admin cụ thể (đã có returnUrl)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.Contains("/Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return returnUrl;
                }
                return Url.Action("Index", "Home", new { area = "Admin" });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action("Index", "Home");
        }
    }
}