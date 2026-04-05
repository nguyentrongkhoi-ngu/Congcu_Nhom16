using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CinemaBooking.Models;
using CinemaBooking.Data;
using CinemaBooking.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CinemaBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            // Tăng kích thước trang cho trang chủ để hiển thị đầy đủ hơn
            const int pageSize = 30; 
            var today = DateTime.Today;

            // 1. Lấy tất cả phim (đa dạng hơn 10 phim)
            var allMovies = await _context.Phims
                .Include(p => p.DanhGias)
                .OrderByDescending(p => p.NgayPhatHanh)
                .Take(pageSize)
                .ToListAsync();

            // 2. Phim Đang Chiếu (Có lịch chiếu trong tương lai)
            var nowShowingMovies = await _context.Phims
                .Include(p => p.DanhGias)
                .Include(p => p.LichChieus)
                .Where(p => p.LichChieus.Any(l => 
                    l.NgayChieu >= today))
                .OrderByDescending(p => p.LichChieus.Max(l => l.NgayChieu))
                .Take(15)
                .ToListAsync();

            // 3. Phim Sắp Chiếu (Ngày phát hành > hôm nay và chưa có lịch chiếu sớm)
            var comingSoonMovies = await _context.Phims
                .Include(p => p.DanhGias)
                .Where(p => p.NgayPhatHanh > today && !p.LichChieus.Any(l => l.NgayChieu <= today))
                .OrderBy(p => p.NgayPhatHanh)
                .Take(15)
                .ToListAsync();

            // 4. Phim Được Đánh Giá Cao
            var topRatedMovies = await _context.Phims
                .Include(p => p.DanhGias)
                .Where(p => p.DanhGias.Any())
                .OrderByDescending(p => p.DanhGias.Average(d => d.DiemSo ?? 0))
                .Where(p => p.DanhGias.Average(d => d.DiemSo ?? 0) >= 4.0)
                .Take(10)
                .ToListAsync();

            // 5. Phim Nổi Bật (Dựa trên lịch chiếu dày đặc nhất tuần tới)
            var featuredMovies = await _context.Phims
                .Include(p => p.DanhGias)
                .Include(p => p.LichChieus)
                .Where(p => p.LichChieus.Any(l => l.NgayChieu >= today && l.NgayChieu <= today.AddDays(7)))
                .OrderByDescending(p => p.LichChieus.Count(l => l.NgayChieu >= today))
                .Take(8)
                .ToListAsync();

            // Ưu tiên Spider-Man: No Way Home lên đầu trang chủ theo yêu cầu của người dùng
            var spiderman = await _context.Phims
                .Include(p => p.DanhGias)
                .Include(p => p.LichChieus)
                .FirstOrDefaultAsync(p => p.TenPhim == "Spider-Man: No Way Home");

            if (spiderman != null)
            {
                // Xóa khỏi danh sách hiện tại nếu có để tránh trùng lặp
                featuredMovies.RemoveAll(p => p.MaPhim == spiderman.MaPhim);
                // Chèn vào đầu danh sách
                featuredMovies.Insert(0, spiderman);
            }

            // Nếu danh sách nổi bật trống (sau khi xử lý Spider-Man), lấy những phim mới nhất
            if (!featuredMovies.Any())
            {
                featuredMovies = allMovies.Take(8).ToList();
            }
            else if (featuredMovies.Count > 10) // Giới hạn số lượng phim nổi bật
            {
                featuredMovies = featuredMovies.Take(10).ToList();
            }

            // Truyền dữ liệu vào ViewBag
            ViewBag.NowShowingMovies = nowShowingMovies;
            ViewBag.ComingSoonMovies = comingSoonMovies;
            ViewBag.TopRatedMovies = topRatedMovies;
            ViewBag.FeaturedMovies = featuredMovies;
            
            ViewBag.TotalMoviesCount = await _context.Phims.CountAsync();
            ViewBag.TotalCinemasCount = await _context.RapPhims.CountAsync();
            ViewBag.TotalBookings = await _context.DatVes.CountAsync();

            // Fetch extra data for redesign
            ViewBag.Cinemas = await _context.RapPhims.Take(6).ToListAsync();
            ViewBag.Promotions = await _context.KhuyenMais.Where(k => k.NgayKetThuc >= today).OrderByDescending(k => k.NgayBatDau).Take(4).ToListAsync();

            // Phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalMoviesCount / pageSize);

            return View(allMovies);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TestFileAccess()
        {
            var model = new Dictionary<string, object>();
            try
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var postersPath = Path.Combine(webRootPath, "posters");
                var trailersPath = Path.Combine(webRootPath, "trailers");

                model.Add("WebRootPath", webRootPath);

                // Kiểm tra thư mục poster
                if (!Directory.Exists(postersPath))
                {
                    try
                    {
                        Directory.CreateDirectory(postersPath);
                        model.Add("PostersFolder", $"Thư mục posters không tồn tại, đã tạo thư mục mới tại {postersPath}");
                    }
                    catch (Exception ex)
                    {
                        model.Add("PostersError", $"Không thể tạo thư mục posters: {ex.Message}");
                    }
                }
                else
                {
                    model.Add("PostersFolder", $"Thư mục posters tồn tại tại {postersPath}");

                    // Kiểm tra quyền ghi
                    try
                    {
                        var testFile = Path.Combine(postersPath, "test.txt");
                        System.IO.File.WriteAllText(testFile, "Test file permission");
                        model.Add("PostersWriteAccess", "Có quyền ghi vào thư mục posters");

                        // Xóa file test
                        System.IO.File.Delete(testFile);
                    }
                    catch (Exception ex)
                    {
                        model.Add("PostersWriteError", $"Không có quyền ghi vào thư mục posters: {ex.Message}");
                    }
                }

                // Kiểm tra thư mục trailers
                if (!Directory.Exists(trailersPath))
                {
                    try
                    {
                        Directory.CreateDirectory(trailersPath);
                        model.Add("TrailersFolder", $"Thư mục trailers không tồn tại, đã tạo thư mục mới tại {trailersPath}");
                    }
                    catch (Exception ex)
                    {
                        model.Add("TrailersError", $"Không thể tạo thư mục trailers: {ex.Message}");
                    }
                }
                else
                {
                    model.Add("TrailersFolder", $"Thư mục trailers tồn tại tại {trailersPath}");

                    // Kiểm tra quyền ghi
                    try
                    {
                        var testFile = Path.Combine(trailersPath, "test.txt");
                        System.IO.File.WriteAllText(testFile, "Test file permission");
                        model.Add("TrailersWriteAccess", "Có quyền ghi vào thư mục trailers");

                        // Xóa file test
                        System.IO.File.Delete(testFile);
                    }
                    catch (Exception ex)
                    {
                        model.Add("TrailersWriteError", $"Không có quyền ghi vào thư mục trailers: {ex.Message}");
                    }
                }

                model.Add("Success", true);
            }
            catch (Exception ex)
            {
                model.Add("Error", ex.Message);
                model.Add("Success", false);
            }

            return Json(model);
        }

        [Authorize]
        public async Task<IActionResult> LichSuDatVe()
        {
            // Lấy mã người dùng hiện tại
            var maNguoiDung = await User.GetLegacyUserIdAsync(_context);

            if (!maNguoiDung.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách đặt vé của người dùng
            var datVes = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(dg => dg.Ghe)
                .Include(d => d.KhuyenMai)
                .Include(d => d.ThanhToans)
                .Where(d => d.MaNguoiDung == maNguoiDung.Value)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(datVes);
        }

        // Trang hướng dẫn in vé và quét mã QR
        public IActionResult TicketHelp()
        {
            return View();
        }

        public IActionResult MomoSandboxGuide()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendFeedback(IFormFile attachment)
        {
            try
            {
                _logger.LogInformation("Bắt đầu xử lý gửi phản hồi");

                // In ra toàn bộ claim để debug
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"CLAIM TYPE: {claim.Type} - VALUE: {claim.Value}");
                }

                var name = User.GetUserFullName();
                var email = User.GetUserEmail();

                if (string.IsNullOrEmpty(email) && Request.Form.ContainsKey("email"))
                {
                    email = Request.Form["email"].ToString();
                    _logger.LogInformation($"Đã lấy email từ form: {email}");
                }
                if (string.IsNullOrEmpty(email))
                {
                    email = "khach@cinezore.com";
                    _logger.LogInformation("Không tìm thấy email, sử dụng email mặc định");
                }
                var message = Request.Form["message"].ToString();
                _logger.LogInformation($"Thông tin người gửi: {name} ({email})");
                _logger.LogInformation($"Nội dung phản hồi: {message}");
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("CineZore", "huy211439@gmail.com"));
                emailMessage.To.Add(new MailboxAddress("CineZore Support", "cinezore@gmail.com"));
                emailMessage.Subject = $"Phản hồi từ {name}";
                var builder = new BodyBuilder();

                builder.HtmlBody = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #E30613; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>CineZore - Phản hồi từ khách hàng</h2>
        </div>
        <div class='content'>
            <p><strong>Từ:</strong> {name}</p>
            <p><strong>Email:</strong> {email}</p>
            <p><strong>Thời gian:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
            <hr>
            <p><strong>Nội dung phản hồi:</strong></p>
            <p>{message.Replace("\n", "<br>")}</p>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống CineZore</p>
            <p>© {DateTime.Now.Year} CineZore - Hệ thống đặt vé xem phim trực tuyến</p>
        </div>
    </div>
</body>
</html>";

                if (attachment != null && attachment.Length > 0)
                {
                    _logger.LogInformation($"Có file đính kèm: {attachment.FileName}, kích thước: {attachment.Length} bytes");
                    using (var ms = new MemoryStream())
                    {
                        await attachment.CopyToAsync(ms);
                        builder.Attachments.Add(attachment.FileName, ms.ToArray());
                    }
                }

                emailMessage.Body = builder.ToMessageBody();

                _logger.LogInformation("Đang kết nối đến SMTP server...");
                using (var smtp = new SmtpClient())
                {
                    try
                    {
                        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                        _logger.LogInformation("Đã kết nối thành công đến SMTP server");

                        _logger.LogInformation("Đang xác thực...");
                        await smtp.AuthenticateAsync("huy211439@gmail.com", "vclh bfyf onsw lauh");
                        _logger.LogInformation("Xác thực thành công");

                        _logger.LogInformation("Đang gửi email...");
                        await smtp.SendAsync(emailMessage);
                        _logger.LogInformation("Gửi email thành công");

                        await smtp.DisconnectAsync(true);
                        _logger.LogInformation("Đã ngắt kết nối SMTP");
                    }
                    catch (Exception smtpEx)
                    {
                        _logger.LogError($"Lỗi SMTP: {smtpEx.Message}");
                        _logger.LogError($"Stack trace: {smtpEx.StackTrace}");
                        throw;
                    }
                }

                // Gửi email cho admin
                try
                {
                    var confirmMessage = new MimeMessage();
                    confirmMessage.From.Add(new MailboxAddress("CineZore", "cinezore@gmail.com"));
                    confirmMessage.To.Add(new MailboxAddress(name, email));
                    confirmMessage.Subject = "CineZore - Xác nhận đã nhận phản hồi của bạn";
                    var confirmBuilder = new BodyBuilder();
                    confirmBuilder.HtmlBody = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #E30613; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .logo {{ height: 50px; margin-bottom: 10px; }}
        .info {{ font-size: 13px; color: #888; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <img src='https://i.imgur.com/1Q9Z1Zm.png' alt='CineZore' class='logo'>
            <h2>CineZore - Xác nhận đã nhận phản hồi của bạn</h2>
        </div>
        <div class='content'>
            <p>Xin chào <b>{name}</b>,</p>
            <p>Cảm ơn bạn đã gửi phản hồi đến CineZore!</p>
            <p>CineZore đã nhận được phản hồi từ bạn với nội dung:</p>
            <blockquote style='background:#fff3f3;border-left:4px solid #E30613;padding:10px 15px;margin:10px 0;color:#E30613;font-style:italic;'>
                {message.Replace("\n", "<br>")}
            </blockquote>
            <p>Chúng tôi sẽ xem xét và phản hồi trong thời gian sớm nhất. Nếu cần hỗ trợ gấp, bạn có thể liên hệ trực tiếp qua email này, số hotline <b>1900 1234</b> hoặc truy cập <a href='https://cinezore.com'>cinezore.com</a>.</p>
            <p style='color:gray;font-size:12px;'>Nếu bạn không gửi phản hồi này, vui lòng bỏ qua email này.</p>
            <p>Trân trọng,<br>Đội ngũ CineZore</p>
        </div>
        <div class='footer'>
            <div class='info'>
                CineZore - Hệ thống đặt vé xem phim trực tuyến<br>
                Địa chỉ: 123 Đường ABC, Quận 1, TP.HCM<br>
                Hotline: 1900 1234 | Email: support@cinezore.com<br>
                Website: <a href='https://cinezore.com'>cinezore.com</a>
            </div>
            <hr style='margin:15px 0;'>
            <p>Email này được gửi tự động từ hệ thống CineZore</p>
            <p>© {DateTime.Now.Year} CineZore</p>
        </div>
    </div>
</body>
</html>";
                    confirmMessage.Body = confirmBuilder.ToMessageBody();
                    using (var smtp2 = new SmtpClient())
                    {
                        await smtp2.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                        await smtp2.AuthenticateAsync("cinezore@gmail.com", "mrta sixk cwnu egrq");
                        await smtp2.SendAsync(confirmMessage);
                        await smtp2.DisconnectAsync(true);
                    }
                    _logger.LogInformation($"Đã gửi email xác nhận đến khách: {email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi khi gửi email xác nhận cho khách: {ex.Message}");
                }

                // Kiểm tra nếu là yêu cầu AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Cảm ơn bạn đã gửi phản hồi!" });
                }

                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi!";
                _logger.LogInformation("Hoàn thành gửi phản hồi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi gửi phản hồi: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                // Kiểm tra nếu là yêu cầu AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi gửi phản hồi. Vui lòng thử lại sau." });
                }

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi phản hồi. Vui lòng thử lại sau.";
            }

            // Nếu không phải AJAX, chuyển hướng về trang chủ
            return RedirectToAction("Index");
        }









        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
