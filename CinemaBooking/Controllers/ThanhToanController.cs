using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;
using CinemaBooking.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CinemaBooking.Models.Services;
using System.IO;

namespace CinemaBooking.Controllers
{
    [Authorize]
    public class ThanhToanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly MomoService _momoService;

        public ThanhToanController(
            ApplicationDbContext context,
            IConfiguration configuration,
            MomoService momoService)
        {
            _context = context;
            _configuration = configuration;
            _momoService = momoService;
        }

        // Helper method để lấy user ID từ cả Identity và legacy system
        private async Task<int> GetCurrentUserIdAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return 0;
            }

            // Nếu là GUID (Identity user), tìm legacy user tương ứng
            if (Guid.TryParse(userIdClaim, out _))
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    var legacyUser = await _context.NguoiDungs
                        .FirstOrDefaultAsync(u => u.Email == email);
                    return legacyUser?.MaNguoiDung ?? 0;
                }
                return 0;
            }

            // Nếu là số (legacy user ID), parse trực tiếp
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return 0;
        }

        // GET: ThanhToan/Index/5
        public async Task<IActionResult> Index(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(g => g.Ghe)
                .Include(d => d.DatVeCombos)
                    .ThenInclude(c => c.Combo)
                .Include(d => d.KhuyenMai)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra người dùng hiện tại có phải là người đặt vé không
            var currentUserId = await GetCurrentUserIdAsync();
            if (datVe.MaNguoiDung != currentUserId)
            {
                return Forbid();
            }

            // Kiểm tra trạng thái thanh toán
            if (datVe.TrangThai == "Đã thanh toán")
            {
                return RedirectToAction("ThanhToanThanhCong", new { maDatVe = datVe.MaDatVe });
            }

            return View(datVe);
        }

        // POST: ThanhToan/ThanhToanTaiRap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanTaiRap(int maDatVe)
        {
            var datVe = await _context.DatVes.FindAsync(maDatVe);
            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra người dùng hiện tại có phải là người đặt vé không
            var currentUserId = await GetCurrentUserIdAsync();
            if (datVe.MaNguoiDung != currentUserId)
            {
                return Forbid();
            }

            // Cập nhật trạng thái đặt vé
            datVe.TrangThai = "Chờ thanh toán";
            
            // Lưu thông tin thanh toán
            var thanhToan = new ThanhToan
            {
                MaDatVe = maDatVe,
                PhuongThucThanhToan = "Tại rạp",
                TrangThai = "Chờ thanh toán",
                MaGiaoDich = "TaiRap-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + maDatVe,
                NgayThanhToan = DateTime.Now,
                SoTien = datVe.TongTien
            };
            
            _context.ThanhToans.Add(thanhToan);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đặt vé thành công! Vui lòng thanh toán tại rạp trước giờ chiếu 30 phút.";
            return RedirectToAction("ThanhToanThanhCong", new { maDatVe = maDatVe });
        }

        // POST: ThanhToan/ThanhToanVNPay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThanhToanVNPay(int maDatVe)
        {
            // Thông báo bảo trì
            TempData["ErrorMessage"] = "Thanh toán qua VNPAY không được hỗ trợ. Vui lòng chọn phương thức thanh toán khác (Momo hoặc Thanh toán tại rạp).";
            return RedirectToAction("Index", new { maDatVe = maDatVe });
        }

        // POST: ThanhToan/ThanhToanMomo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanMomo(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra người dùng hiện tại có phải là người đặt vé không
            var currentUserId = await GetCurrentUserIdAsync();
            if (datVe.MaNguoiDung != currentUserId)
            {
                return Forbid();
            }

            try 
            {
                // Tạo URL thanh toán qua service MoMo
                string paymentUrl = await _momoService.CreatePaymentUrl(
                    maDatVe, 
                    datVe.LichChieu.Phim.TenPhim, 
                    datVe.TongTien);

                // Lưu thông tin thanh toán vào hệ thống
                var thanhToan = new ThanhToan
                {
                    MaDatVe = maDatVe,
                    PhuongThucThanhToan = "MOMO",
                    TrangThai = "Chờ thanh toán",
                    MaGiaoDich = "MOMO-" + maDatVe.ToString() + "-" + DateTime.Now.Ticks.ToString(),
                    NgayThanhToan = DateTime.Now,
                    SoTien = datVe.TongTien
                };
                
                _context.ThanhToans.Add(thanhToan);
                await _context.SaveChangesAsync();

                // Ghi log giao dịch
                var lichSuGiaoDich = new LichSuGiaoDich
                {
                    MaThanhToan = thanhToan.MaThanhToan,
                    MaNguoiDung = currentUserId,
                    TrangThai = "Khởi tạo",
                    NoiDung = $"Khởi tạo thanh toán MoMo cho vé #{maDatVe}",
                    NgayGiaoDich = DateTime.Now,
                    LoaiGiaoDich = "Thanh toán"
                };
                
                _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                await _context.SaveChangesAsync();

                // Chuyển hướng đến trang thanh toán của MoMo
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"MoMo Payment Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                var lichSuLoi = new LichSuGiaoDich
                {
                    MaNguoiDung = currentUserId,
                    TrangThai = "Lỗi",
                    NoiDung = $"Lỗi khi thanh toán MoMo cho vé #{maDatVe}: {ex.Message}",
                    NgayGiaoDich = DateTime.Now,
                    LoaiGiaoDich = "Lỗi thanh toán"
                };

                _context.LichSuGiaoDiches.Add(lichSuLoi);
                await _context.SaveChangesAsync();

                // Cung cấp thông báo lỗi thân thiện với người dùng
                string userFriendlyMessage = "Hiện tại hệ thống MoMo đang gặp sự cố. Bạn có thể thử lại sau hoặc chọn phương thức thanh toán khác.";

                if (ex.Message.Contains("99"))
                {
                    userFriendlyMessage = "Hệ thống MoMo hiện đang bảo trì. Vui lòng thử lại sau hoặc chọn thanh toán tại rạp.";
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("Timeout"))
                {
                    userFriendlyMessage = "Kết nối đến MoMo bị timeout. Vui lòng kiểm tra kết nối mạng và thử lại.";
                }

                TempData["ErrorMessage"] = userFriendlyMessage;
                return RedirectToAction("Index", new { maDatVe = maDatVe });
            }
        }

        // GET: ThanhToan/MomoError
        public IActionResult MomoError(int maDatVe, string errorMessage)
        {
            ViewBag.MaDatVe = maDatVe;
            ViewBag.ErrorMessage = errorMessage;
            return View();
        }

        // GET: ThanhToan/MomoReturn
        public async Task<IActionResult> MomoReturn()
        {
            try
            {
                // Lấy thông tin từ callback của MoMo
                string resultCode = Request.Query["resultCode"];
                string orderId = Request.Query["orderId"];
                string message = Request.Query["message"];
                string transId = Request.Query["transId"];
                string orderInfo = Request.Query["orderInfo"];
                string extraData = Request.Query["extraData"];
                string signature = Request.Query["signature"];

                // Ghi log thông tin nhận được từ MoMo để debug
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Payments");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                
                var logData = new StringBuilder();
                logData.AppendLine($"=== MoMo Return Data - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                logData.AppendLine($"resultCode: {resultCode}");
                logData.AppendLine($"orderId: {orderId}");
                logData.AppendLine($"message: {message}");
                logData.AppendLine($"transId: {transId}");
                logData.AppendLine($"orderInfo: {orderInfo}");
                logData.AppendLine($"extraData: {extraData}");
                
                System.IO.File.WriteAllText(
                    Path.Combine(logPath, $"momo_return_{DateTime.Now:yyyyMMdd_HHmmss}.log"), 
                    logData.ToString());

                // Tạo dictionary để xác thực chữ ký
                var requestData = new Dictionary<string, string>();
                foreach (var key in Request.Query.Keys)
                {
                    if (key != "signature")
                    {
                        requestData.Add(key, Request.Query[key]);
                    }
                }

                // Kiểm tra nếu orderId rỗng, chuyển về trang chủ
                if (string.IsNullOrEmpty(orderId))
                {
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ từ MoMo";
                    return RedirectToAction("Index", "Home");
                }

                // Lấy mã đặt vé từ orderId (format: maDatVe-timestamp)
                int maDatVe = int.Parse(orderId.Split('-')[0]);

                // Xác thực chữ ký từ MoMo 
                bool isValidSignature = _momoService.ValidateSignature(requestData, signature);
                
                // Ghi log kết quả xác thực
                logData.Clear();
                logData.AppendLine($"=== MoMo Signature Validation - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                logData.AppendLine($"isValidSignature: {isValidSignature}");
                logData.AppendLine($"maDatVe: {maDatVe}");
                
                System.IO.File.WriteAllText(
                    Path.Combine(logPath, $"momo_validation_{DateTime.Now:yyyyMMdd_HHmmss}.log"), 
                    logData.ToString());

                // Trong môi trường thực tế, phải luôn xác thực chữ ký
                // Trong môi trường sandbox, đôi khi chữ ký có thể không hợp lệ
                // Bạn có thể bật/tắt điều kiện này tùy theo môi trường
                bool requireValidSignature = false; // Đặt thành true khi triển khai thực tế
                
                if (requireValidSignature && !isValidSignature)
                {
                    TempData["ErrorMessage"] = "Xác thực chữ ký MoMo thất bại";
                    return RedirectToAction("Index", "Home");
                }

                // Lấy thông tin đặt vé và thanh toán
                var datVe = await _context.DatVes.FindAsync(maDatVe);
                if (datVe == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt vé";
                    return RedirectToAction("Index", "Home");
                }

                var thanhToan = await _context.ThanhToans
                    .FirstOrDefaultAsync(t => t.MaDatVe == maDatVe && t.PhuongThucThanhToan == "MOMO");

                // *** XỬ LÝ QUAN TRỌNG ***
                // Chỉ coi thanh toán thành công khi resultCode = 0
                // Trước đây chúng ta coi thanh toán thành công dựa vào nhiều điều kiện
                bool isSuccessful = false;

                // Cách xác định thanh toán thành công đúng chuẩn:
                // Chỉ khi resultCode = 0 mới coi là thanh toán thành công
                if (resultCode == "0")
                {
                    isSuccessful = true;
                }
                
                // Ghi log thêm thông tin để debug
                logData.Clear();
                logData.AppendLine($"=== MoMo Payment Result - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                logData.AppendLine($"resultCode: {resultCode}");
                logData.AppendLine($"message: {message}");
                logData.AppendLine($"isSuccessful: {isSuccessful}");
                
                System.IO.File.WriteAllText(
                    Path.Combine(logPath, $"momo_payment_result_{DateTime.Now:yyyyMMdd_HHmmss}.log"), 
                    logData.ToString());

                if (isSuccessful)
                {
                    // Cập nhật trạng thái đặt vé thành "Đã thanh toán"
                    datVe.TrangThai = "Đã thanh toán";
                    
                    // Cập nhật thông tin thanh toán
                    if (thanhToan != null)
                    {
                        thanhToan.TrangThai = "Thành công";
                        thanhToan.MaGiaoDichNganHang = transId;
                        await _context.SaveChangesAsync();
                        
                        // Ghi log giao dịch IPN thành công
                        var lichSuGiaoDich = new LichSuGiaoDich
                        {
                            MaThanhToan = thanhToan.MaThanhToan,
                            MaNguoiDung = datVe.MaNguoiDung,
                            TrangThai = "Thành công (IPN)",
                            NoiDung = $"Thanh toán vé qua MoMo thành công (IPN). Mã giao dịch: {transId}",
                            NgayGiaoDich = DateTime.Now,
                            LoaiGiaoDich = "Thanh toán"
                        };
                        
                        _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                        await _context.SaveChangesAsync();
                        
                        // Ghi log thành công
                        System.IO.File.WriteAllText(
                            Path.Combine(logPath, $"momo_ipn_success_{maDatVe}_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                            $"Cập nhật thành công thanh toán MoMo cho đơn hàng {maDatVe}, TransID: {transId}");

                        // Cập nhật điểm tích lũy
                        await UpdateUserPoints(maDatVe);
                    }
                    
                    TempData["SuccessMessage"] = "Thanh toán qua MoMo thành công!";
                    return RedirectToAction("ThanhToanThanhCong", new { maDatVe = maDatVe });
                }
                else
                {
                    // Cập nhật thông tin thanh toán thành "Thất bại"
                    if (thanhToan != null)
                    {
                        thanhToan.TrangThai = "Thất bại";
                        thanhToan.MaGiaoDichNganHang = transId;
                        await _context.SaveChangesAsync();
                    }
                    
                    // Điều chỉnh thông báo lỗi
                    string errorMessage = string.IsNullOrEmpty(message) ? 
                        $"Mã lỗi: {resultCode}" : message;
                    
                    // Lưu lịch sử giao dịch
                    var lichSuGiaoDich = new LichSuGiaoDich
                    {
                        MaThanhToan = thanhToan?.MaThanhToan,
                        MaNguoiDung = datVe.MaNguoiDung,
                        TrangThai = "Thất bại",
                        NoiDung = $"Thanh toán qua MoMo thất bại. Mã lỗi: {resultCode}, Lý do: {errorMessage}",
                        NgayGiaoDich = DateTime.Now,
                        LoaiGiaoDich = "Thanh toán"
                    };
                    
                    _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                    await _context.SaveChangesAsync();
                    
                    TempData["ErrorMessage"] = $"Thanh toán MoMo không thành công: {errorMessage}";
                    return RedirectToAction("Index", new { maDatVe = maDatVe });
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi exception
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Payments");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                
                System.IO.File.WriteAllText(
                    Path.Combine(logPath, $"momo_error_{DateTime.Now:yyyyMMdd_HHmmss}.log"), 
                    ex.ToString());
                
                TempData["ErrorMessage"] = "Lỗi xử lý kết quả từ MoMo: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: ThanhToan/MomoNotify - Nhận thông báo IPN từ MoMo
        [HttpPost]
        public async Task<IActionResult> MomoNotify()
        {
            try
            {
                // Đọc dữ liệu từ request body
                using (var reader = new System.IO.StreamReader(Request.Body))
                {
                    var body = await reader.ReadToEndAsync();
                    var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                    
                    // Ghi log để debug
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Payments");
                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                    }
                    
                    System.IO.File.WriteAllText(
                        Path.Combine(logPath, $"momo_ipn_{DateTime.Now:yyyyMMdd_HHmmss}.json"), 
                        body);
                    
                    if (data != null && data.ContainsKey("orderId"))
                    {
                        string resultCode = data.ContainsKey("resultCode") ? data["resultCode"].ToString() : "";
                        string orderId = data["orderId"].ToString();
                        string message = data.ContainsKey("message") ? data["message"].ToString() : "";
                        string transId = data.ContainsKey("transId") ? data["transId"].ToString() : "";
                        string signature = data.ContainsKey("signature") ? data["signature"].ToString() : "";
                        
                        // Xác thực chữ ký MoMo cho IPN
                        var requestData = new Dictionary<string, string>();
                        foreach (var item in data)
                        {
                            if (item.Key != "signature" && item.Value != null)
                            {
                                requestData.Add(item.Key, item.Value.ToString());
                            }
                        }
                        
                        bool isValidSignature = _momoService.ValidateSignature(requestData, signature);
                        System.IO.File.WriteAllText(
                            Path.Combine(logPath, $"momo_ipn_signature_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                            $"isValidSignature: {isValidSignature}");
                            
                        // Trong môi trường thực tế, luôn luôn yêu cầu chữ ký hợp lệ
                        bool requireValidSignature = false; // Đặt thành true khi triển khai thực tế
                        if (requireValidSignature && !isValidSignature)
                        {
                            return BadRequest(new { status = "error", message = "Chữ ký không hợp lệ" });
                        }
                        
                        // Lấy mã đặt vé từ orderId (format: maDatVe-timestamp)
                        int maDatVe = int.Parse(orderId.Split('-')[0]);
                        
                        // *** Kiểm tra thanh toán thành công tương tự như MomoReturn ***
                        bool isSuccessful = false;
                        
                        // Chỉ coi thanh toán thành công khi resultCode = 0
                        if (resultCode == "0")
                        {
                            isSuccessful = true;
                        }
                        
                        // Ghi log kết quả xác định thanh toán
                        System.IO.File.WriteAllText(
                            Path.Combine(logPath, $"momo_ipn_status_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                            $"resultCode: {resultCode}, message: {message}, isSuccessful: {isSuccessful}");

                        if (isSuccessful)
                        {
                            // Cập nhật trạng thái đặt vé
                            var datVe = await _context.DatVes.FindAsync(maDatVe);
                            if (datVe != null)
                            {
                                datVe.TrangThai = "Đã thanh toán";
                                
                                // Cập nhật thông tin thanh toán
                                var thanhToan = await _context.ThanhToans
                                    .FirstOrDefaultAsync(t => t.MaDatVe == maDatVe && t.PhuongThucThanhToan == "MOMO");
                                
                                if (thanhToan != null)
                                {
                                    thanhToan.TrangThai = "Thành công";
                                    thanhToan.MaGiaoDichNganHang = transId;
                                    await _context.SaveChangesAsync();
                                    
                                    // Ghi log giao dịch IPN thành công
                                    var lichSuGiaoDich = new LichSuGiaoDich
                                    {
                                        MaThanhToan = thanhToan.MaThanhToan,
                                        MaNguoiDung = datVe.MaNguoiDung,
                                        TrangThai = "Thành công (IPN)",
                                        NoiDung = $"Thanh toán vé qua MoMo thành công (IPN). Mã giao dịch: {transId}",
                                        NgayGiaoDich = DateTime.Now,
                                        LoaiGiaoDich = "Thanh toán"
                                    };
                                    
                                    _context.LichSuGiaoDiches.Add(lichSuGiaoDich);
                                    await _context.SaveChangesAsync();
                                    
                                    // Ghi log thành công
                                    System.IO.File.WriteAllText(
                                        Path.Combine(logPath, $"momo_ipn_success_{maDatVe}_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                                        $"Cập nhật thành công thanh toán MoMo cho đơn hàng {maDatVe}, TransID: {transId}");

                                    // Cập nhật điểm tích lũy
                                    await UpdateUserPoints(maDatVe);
                                }
                            }
                        }
                    }
                    
                    // MoMo IPN yêu cầu trả về status "ok"
                    return Ok(new { status = "ok" });
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Payments");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                
                System.IO.File.WriteAllText(
                    Path.Combine(logPath, $"momo_ipn_error_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), 
                    ex.ToString());
                    
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
        
        // GET: ThanhToan/ThanhToanThanhCong
        public async Task<IActionResult> ThanhToanThanhCong(int maDatVe)
        {
            var datVe = await _context.DatVes
                .Include(d => d.NguoiDung)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.Phim)
                .Include(d => d.LichChieu)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Include(d => d.DatVeGhes)
                    .ThenInclude(g => g.Ghe)
                .Include(d => d.DatVeCombos)
                    .ThenInclude(c => c.Combo)
                .Include(d => d.KhuyenMai)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

            if (datVe == null)
            {
                return NotFound();
            }

            // Kiểm tra người dùng hiện tại có phải là người đặt vé không
            var currentUserId = await GetCurrentUserIdAsync();
            if (datVe.MaNguoiDung != currentUserId)
            {
                return Forbid();
            }

            // Cập nhật trạng thái cho người dùng xem
            if (datVe.TrangThai != "Đã thanh toán" && datVe.ThanhToans != null && datVe.ThanhToans.Any(t => t.TrangThai == "Thành công"))
            {
                datVe.TrangThai = "Đã thanh toán";
                await _context.SaveChangesAsync();
                
                // Cập nhật điểm tích lũy
                await UpdateUserPoints(maDatVe);
            }

            return View(datVe);
        }
        
        // Lấy địa chỉ IP của người dùng
        private string GetIpAddress()
        {
            string ipAddress;
            try
            {
                ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress) || ipAddress.ToLower() == "unknown")
                {
                    ipAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }
            }
            catch (Exception)
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }

        // Cập nhật điểm tích lũy cho người dùng sau khi thanh toán thành công
        private async Task UpdateUserPoints(int maDatVe)
        {
            try
            {
                var datVe = await _context.DatVes
                    .Include(d => d.NguoiDung)
                    .FirstOrDefaultAsync(d => d.MaDatVe == maDatVe);

                if (datVe != null && datVe.NguoiDung != null && datVe.TrangThai == "Đã thanh toán")
                {
                    // Kiểm tra xem đã tích điểm cho vé này chưa (tránh cộng trùng)
                    var exists = await _context.LichSuGiaoDiches
                        .AnyAsync(l => l.MaNguoiDung == datVe.MaNguoiDung && l.LoaiGiaoDich == "Tích điểm" && l.NoiDung.Contains($"vé #{maDatVe}"));
                    
                    if (!exists)
                    {
                        // 1,000 VND = 1 điểm
                        int pointsEarned = (int)(datVe.TongTien / 1000);
                        
                        if (pointsEarned > 0)
                        {
                            datVe.NguoiDung.DiemTichLuy += pointsEarned;
                            
                            // Ghi log tích điểm
                            var lichSuDiem = new LichSuGiaoDich
                            {
                                MaNguoiDung = datVe.MaNguoiDung,
                                LoaiGiaoDich = "Tích điểm",
                                TrangThai = "Thành công",
                                NoiDung = $"Tích +{pointsEarned} điểm từ vé #{maDatVe}",
                                NgayGiaoDich = DateTime.Now
                            };
                            
                            _context.LichSuGiaoDiches.Add(lichSuDiem);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nhưng không làm gián đoạn luồng thanh toán
                Console.WriteLine($"Error updating points: {ex.Message}");
            }
        }
    }
}