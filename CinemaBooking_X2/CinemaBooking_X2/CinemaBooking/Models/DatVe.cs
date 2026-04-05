using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("dat_ve")]
    public class DatVe
    {
        [Key]
        [Column("ma_dat_ve")]
        public int MaDatVe { get; set; }

        [Required]
        [Column("ma_nguoi_dung")]
        public int MaNguoiDung { get; set; }

        // For Identity integration
        [Column("user_id")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [Column("ma_lich_chieu")]
        public int MaLichChieu { get; set; }

        [Column("ngay_dat")]
        public DateTime? NgayDat { get; set; }

        [Required]
        [Column("tong_tien", TypeName = "decimal(10, 2)")]
        [DataType(DataType.Currency)]
        public decimal TongTien { get; set; }

        [Column("trang_thai")]
        [StringLength(50)]
        public string TrangThai { get; set; }

        [Column("ma_khuyen_mai")]
        public int? MaKhuyenMai { get; set; }

        [Column("ghi_chu")]
        [StringLength(255)]
        public string? GhiChu { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey("MaLichChieu")]
        public virtual LichChieu LichChieu { get; set; }

        [ForeignKey("MaKhuyenMai")]
        public virtual KhuyenMai KhuyenMai { get; set; }

        public virtual ICollection<DatVeGhe> DatVeGhes { get; set; }
        public virtual ICollection<DatVeCombo> DatVeCombos { get; set; }
        public virtual ICollection<ThanhToan> ThanhToans { get; set; }
    }
} 