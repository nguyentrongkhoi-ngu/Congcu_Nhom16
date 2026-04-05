using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CinemaBooking.Data;

namespace CinemaBooking.Models
{
    [Table("lich_su_giao_dich")]
    public class LichSuGiaoDich
    {
        [Key]
        [Column("ma_giao_dich")]
        public int MaGiaoDich { get; set; }

        [Column("ma_nguoi_dung")]
        public int? MaNguoiDung { get; set; }

        // For Identity integration
        [Column("user_id")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Column("ma_thanh_toan")]
        public int? MaThanhToan { get; set; }

        [Required]
        [Column("loai_giao_dich")]
        [StringLength(50)]
        public string LoaiGiaoDich { get; set; } = string.Empty;

        [Column("trang_thai")]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        [Column("noi_dung")]
        [StringLength(255)]
        public string NoiDung { get; set; } = string.Empty;

        [Column("ngay_giao_dich")]
        public DateTime? NgayGiaoDich { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey("MaThanhToan")]
        public virtual ThanhToan? ThanhToan { get; set; }
    }
} 