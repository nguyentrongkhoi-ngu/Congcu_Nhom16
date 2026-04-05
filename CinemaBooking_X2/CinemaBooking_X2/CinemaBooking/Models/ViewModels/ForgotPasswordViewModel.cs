using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        // Trường dành cho OTP - chỉ bắt buộc ở bước 2
        [Display(Name = "Mã xác thực")]
        public string MaXacThuc { get; set; }
        
        // Mật khẩu mới - chỉ bắt buộc ở bước 3
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhauMoi { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp")]
        public string XacNhanMatKhauMoi { get; set; }
        
        // Cờ để đánh dấu trạng thái
        public bool DaGuiOTP { get; set; }
        
        // Bước hiện tại của quy trình (1: Nhập email, 2: Nhập OTP, 3: Đặt mật khẩu mới)
        public int Buoc { get; set; } = 1;
    }
} 