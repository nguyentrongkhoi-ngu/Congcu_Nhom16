using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("nguoi_dung")]
    public class NguoiDung
    {
        [Key]
        [Column("ma_nguoi_dung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [Column("ten_dang_nhap")]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [Column("mat_khau")]
        [StringLength(255)]
        public string MatKhau { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Column("ho_ten")]
        [StringLength(100)]
        public string? HoTen { get; set; }

        [Column("so_dien_thoai")]
        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        [Column("ngay_tao")]
        public DateTime? NgayTao { get; set; }

        [Column("ma_vai_tro")]
        public int? MaVaiTro { get; set; }

        [Column("avatar_url")]
        [StringLength(255)]
        public string? AvatarUrl { get; set; }

        [Column("diem_tich_luy")]
        public int DiemTichLuy { get; set; } = 0;

        [ForeignKey("MaVaiTro")]
        public virtual VaiTro? VaiTro { get; set; }

        public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
        public virtual ICollection<DatVe> DatVes { get; set; } = new List<DatVe>();
        public virtual ICollection<LichSuGiaoDich> LichSuGiaoDichs { get; set; } = new List<LichSuGiaoDich>();
        public virtual ICollection<OtpInfo> OtpInfos { get; set; } = new List<OtpInfo>();
    }
}