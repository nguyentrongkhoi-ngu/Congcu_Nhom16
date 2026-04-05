using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("thanh_toan")]
    public class ThanhToan
    {
        [Key]
        [Column("ma_thanh_toan")]
        public int MaThanhToan { get; set; }

        [Required]
        [Column("ma_dat_ve")]
        public int MaDatVe { get; set; }

        [Required]
        [Column("so_tien", TypeName = "decimal(10, 2)")]
        [DataType(DataType.Currency)]
        public decimal SoTien { get; set; }

        [Required]
        [Column("phuong_thuc_thanh_toan")]
        [StringLength(50)]
        public string PhuongThucThanhToan { get; set; }

        [Column("trang_thai")]
        [StringLength(50)]
        public string TrangThai { get; set; }

        [Column("ma_giao_dich")]
        [StringLength(100)]
        public string? MaGiaoDich { get; set; }

        [Column("ma_giao_dich_ngan_hang")]
        [StringLength(100)]
        public string? MaGiaoDichNganHang { get; set; }

        [Column("ngay_thanh_toan")]
        public DateTime? NgayThanhToan { get; set; }

        [Column("ghi_chu")]
        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey("MaDatVe")]
        public virtual DatVe DatVe { get; set; }
    }
} 