using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Services
{
    public class OtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        
        public OtpService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        
        // Tạo OTP ngẫu nhiên 6 chữ số
        public string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        
        // Tạo và lưu OTP mới
        public async Task<string> CreateOtp(string email, string loaiOtp, string hoTen = null, int? maNguoiDung = null)
        {
            var otp = GenerateOtp();
            
            // Thời gian hết hạn: 10 phút sau khi tạo
            var expirationTime = DateTime.Now.AddMinutes(10);
            
            // Tạo đối tượng OtpInfo
            var otpInfo = new OtpInfo
            {
                Email = email,
                MaXacThuc = otp,
                ThoiGianTao = DateTime.Now,
                ThoiGianHetHan = expirationTime,
                LoaiOtp = loaiOtp,
                DaSuDung = false,
                MaNguoiDung = maNguoiDung
            };
            
            // Lưu vào database
            _context.OtpInfos.Add(otpInfo);
            await _context.SaveChangesAsync();
            
            // In thông tin OTP ra console để debug
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine($"MÃ OTP CHO {email}: {otp}");
            Console.WriteLine($"THỜI GIAN HẾT HẠN: {expirationTime}");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            
            try
            {
                // Gửi email OTP thực tế
                bool emailSent;
                
                if (loaiOtp == "QuenMatKhau")
                {
                    emailSent = await _emailService.SendForgotPasswordOtpAsync(email, otp, hoTen);
                }
                else
                {
                    emailSent = await _emailService.SendOtpEmailAsync(email, otp, hoTen);
                }
                
                if (emailSent)
                {
                    Console.WriteLine($"Đã gửi email OTP thành công đến {email}");
                }
                else
                {
                    Console.WriteLine($"Không thể gửi email OTP đến {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi email OTP: {ex.Message}");
                // Không ảnh hưởng đến luồng xử lý, vẫn trả về OTP
            }
            
            return otp;
        }
        
        // Xác thực OTP
        public async Task<bool> VerifyOtp(string email, string otp, string loaiOtp)
        {
            var otpInfo = await _context.OtpInfos
                .Where(o => o.Email == email && o.MaXacThuc == otp && o.LoaiOtp == loaiOtp && !o.DaSuDung)
                .OrderByDescending(o => o.ThoiGianTao)
                .FirstOrDefaultAsync();
            
            if (otpInfo == null)
            {
                return false;
            }
            
            // Kiểm tra thời gian hết hạn
            if (otpInfo.ThoiGianHetHan < DateTime.Now)
            {
                return false;
            }
            
            // Đánh dấu OTP đã được sử dụng
            otpInfo.DaSuDung = true;
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        // Xóa OTP cũ của một email
        public async Task InvalidateOldOtps(string email, string loaiOtp)
        {
            var oldOtps = await _context.OtpInfos
                .Where(o => o.Email == email && o.LoaiOtp == loaiOtp && !o.DaSuDung)
                .ToListAsync();
            
            foreach (var oldOtp in oldOtps)
            {
                oldOtp.DaSuDung = true;
            }
            
            await _context.SaveChangesAsync();
        }
    }
} 