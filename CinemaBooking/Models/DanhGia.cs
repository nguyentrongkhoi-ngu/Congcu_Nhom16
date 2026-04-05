using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("danh_gia")]
    public class DanhGia
    {
        [Key]
        [Column("ma_danh_gia")]
        public int MaDanhGia { get; set; }

        [Required]
        [Column("ma_nguoi_dung")]
        public int MaNguoiDung { get; set; }

        // For Identity integration
        [Column("user_id")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [Column("ma_phim")]
        public int MaPhim { get; set; }

        [Column("diem_so")]
        public int? DiemSo { get; set; }

        [Column("binh_luan", TypeName = "ntext")]
        public string BinhLuan { get; set; }

        [Column("ngay_danh_gia")]
        public DateTime? NgayDanhGia { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey("MaPhim")]
        public virtual Phim Phim { get; set; }
    }
} 