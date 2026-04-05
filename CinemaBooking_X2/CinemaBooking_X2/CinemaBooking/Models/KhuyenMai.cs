using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("khuyen_mai")]
    public class KhuyenMai
    {
        [Key]
        [Column("ma_khuyen_mai")]
        public int MaKhuyenMai { get; set; }

        [Required]
        [Column("ma_code")]
        [StringLength(20)]
        public string MaCode { get; set; }

        [Required]
        [Column("phan_tram_giam")]
        public int PhanTramGiam { get; set; }

        [Column("gia_tri_toi_thieu")]
        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        public decimal GiaTriToiThieu { get; set; } = 0;

        [Required]
        [Column("ngay_bat_dau")]
        [DataType(DataType.Date)]
        public DateTime NgayBatDau { get; set; }

        [Required]
        [Column("ngay_ket_thuc")]
        [DataType(DataType.Date)]
        public DateTime NgayKetThuc { get; set; }

        [Column("mo_ta")]
        [StringLength(255)]
        public string MoTa { get; set; }

        public virtual ICollection<DatVe> DatVes { get; set; }
    }
} 