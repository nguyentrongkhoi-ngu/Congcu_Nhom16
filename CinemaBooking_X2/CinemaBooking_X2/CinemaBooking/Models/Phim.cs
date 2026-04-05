using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("phim")]
    public class Phim
    {
        [Key]
        [Column("ma_phim")]
        public int MaPhim { get; set; }

        [Required]
        [Column("ten_phim")]
        [StringLength(100)]
        public string TenPhim { get; set; }

        [Column("mo_ta")]
        public string MoTa { get; set; }

        [Required]
        [Column("thoi_luong")]
        public int ThoiLuong { get; set; }

        [Column("the_loai")]
        [StringLength(50)]
        public string TheLoai { get; set; }

        [Column("ngay_phat_hanh")]
        [DataType(DataType.Date)]
        public DateTime? NgayPhatHanh { get; set; }

        [Column("url_poster")]
        [StringLength(255)]
        public string UrlPoster { get; set; }

        [Column("url_backdrop")]
        [StringLength(255)]
        public string UrlBackdrop { get; set; }

        [Column("diem_imdb")]
        public double? DiemIMDb { get; set; }

        [Column("dinh_dang")]
        [StringLength(20)]
        public string DinhDang { get; set; }

        [Column("trailer")]
        [StringLength(255)]
        public string Trailer { get; set; }

        [Column("trang_thai")]
        [StringLength(50)]
        public string TrangThai { get; set; }

        public virtual ICollection<NgonNguPhim> NgonNguPhims { get; set; }
        public virtual ICollection<LichChieu> LichChieus { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        
        [NotMapped]
        public string HinhAnh => UrlPoster;
    }
} 