using System;
using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Models.ViewModels
{
    public class DanhGiaViewModel
    {
        public int MaPhim { get; set; }
        public string TenPhim { get; set; }
        public string UrlPoster { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn điểm số")]
        [Range(1, 5, ErrorMessage = "Điểm số phải từ 1-5")]
        public int DiemSo { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập bình luận")]
        [StringLength(500, ErrorMessage = "Bình luận không được quá 500 ký tự")]
        public string BinhLuan { get; set; }
        
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
    }
    
    public class PhimDanhGiaViewModel
    {
        public Phim Phim { get; set; }
        public DanhGia DanhGia { get; set; }
        public IEnumerable<DanhGia> DanhSachDanhGia { get; set; }
        public double DiemTrungBinh { get; set; }
        public int TongSoDanhGia { get; set; }
    }
} 