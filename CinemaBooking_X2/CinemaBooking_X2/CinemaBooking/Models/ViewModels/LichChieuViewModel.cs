using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CinemaBooking.Models.ViewModels
{
    public class LichChieuViewModel
    {
        public int MaLichChieu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phim")]
        [Display(Name = "Phim")]
        public int MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu")]
        [Display(Name = "Phòng chiếu")]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày chiếu")]
        [Display(Name = "Ngày chiếu")]
        [DataType(DataType.Date)]
        public DateTime NgayChieu { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng nhập giờ chiếu")]
        [Display(Name = "Giờ chiếu")]
        [DataType(DataType.Time)]
        public TimeSpan GioChieu { get; set; } = new TimeSpan(9, 0, 0); // Mặc định 9:00 AM

        [Required(ErrorMessage = "Vui lòng nhập giá vé")]
        [Display(Name = "Giá vé")]
        [Range(0, 1000000, ErrorMessage = "Giá vé phải nằm trong khoảng từ 0 đến 1,000,000")]
        [DataType(DataType.Currency)]
        public decimal GiaVe { get; set; }

        [Display(Name = "Ngôn ngữ")]
        public int? MaNgonNgu { get; set; }
        
        // Properties cho dropdown list - không yêu cầu validation
        [ValidateNever]
        public SelectList? PhimList { get; set; }
        
        [ValidateNever]
        public SelectList? PhongList { get; set; }
        
        [ValidateNever]
        public SelectList? NgonNguList { get; set; }
        
        // Thông tin bổ sung để hiển thị - không yêu cầu validation
        [ValidateNever]
        public string? TenPhim { get; set; }
        
        [ValidateNever]
        public string? TenPhong { get; set; }
        
        [ValidateNever]
        public string? TenRap { get; set; }
        
        [ValidateNever]
        public string? NgonNgu { get; set; }
    }
    
    public class LichChieuListViewModel
    {
        public List<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
        public string? SearchTerm { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public int? MaPhim { get; set; }
        public int? MaRap { get; set; }
        
        [ValidateNever]
        public SelectList? PhimList { get; set; }
        
        [ValidateNever]
        public SelectList? RapList { get; set; }
    }
} 