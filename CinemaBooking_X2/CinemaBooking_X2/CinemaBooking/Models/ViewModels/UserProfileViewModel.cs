using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CinemaBooking.Models.ViewModels
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string? HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(0[0-9]{9})$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng số 0 và có 10 chữ số)")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [Display(Name = "Mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 