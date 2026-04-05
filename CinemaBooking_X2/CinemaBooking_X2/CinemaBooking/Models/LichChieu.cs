using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("lich_chieu")]
    public class LichChieu
    {
        [Key]
        [Column("ma_lich_chieu")]
        public int MaLichChieu { get; set; }

        [Required]
        [Column("ma_phim")]
        public int MaPhim { get; set; }

        [Required]
        [Column("ma_phong")]
        public int MaPhong { get; set; }

        [Required]
        [Column("ngay_chieu")]
        [DataType(DataType.Date)]
        public DateTime NgayChieu { get; set; }

        [Required]
        [Column("gio_chieu")]
        [DataType(DataType.Time)]
        public TimeSpan GioChieu { get; set; }

        [Required]
        [Column("gia_ve", TypeName = "decimal(10, 2)")]
        [DataType(DataType.Currency)]
        public decimal GiaVe { get; set; }

        [Column("ma_ngon_ngu")]
        public int? MaNgonNgu { get; set; }

        [ForeignKey("MaPhim")]
        public virtual Phim Phim { get; set; } = null!;

        [ForeignKey("MaPhong")]
        public virtual PhongChieu PhongChieu { get; set; } = null!;

        [ForeignKey("MaNgonNgu")]
        public virtual NgonNguPhim? NgonNguPhim { get; set; }

        public virtual ICollection<DatVe> DatVes { get; set; } = new List<DatVe>();
    }
} 