using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CinemaBooking.Models.ViewModels
{
    public class PhimViewModel
    {
        public int MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phim")]
        [Display(Name = "Tên phim")]
        [StringLength(100, ErrorMessage = "Tên phim không được vượt quá 100 ký tự")]
        public string TenPhim { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời lượng phim")]
        [Display(Name = "Thời lượng (phút)")]
        [Range(1, 500, ErrorMessage = "Thời lượng phim phải từ 1 đến 500 phút")]
        public int ThoiLuong { get; set; }

        [Display(Name = "Thể loại")]
        [StringLength(50, ErrorMessage = "Thể loại không được vượt quá 50 ký tự")]
        public string? TheLoai { get; set; }

        [Display(Name = "Ngày phát hành")]
        [DataType(DataType.Date)]
        public DateTime? NgayPhatHanh { get; set; }

        [Display(Name = "Định dạng")]
        [StringLength(20, ErrorMessage = "Định dạng không được vượt quá 20 ký tự")]
        public string? DinhDang { get; set; }

        // File upload properties
        [Display(Name = "Poster phim")]
        [Required(ErrorMessage = "Vui lòng chọn ảnh poster cho phim")]
        public IFormFile? PosterFile { get; set; }

        [Display(Name = "Trailer phim")]
        [Required(ErrorMessage = "Vui lòng chọn file trailer cho phim")]
        public IFormFile? TrailerFile { get; set; }

        // Existing URLs - không bắt buộc vì chúng ta dùng PosterFile và TrailerFile
        public string? UrlPoster { get; set; }
        public string? Trailer { get; set; }
    }
} 