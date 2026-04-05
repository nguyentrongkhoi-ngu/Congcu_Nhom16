using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("rap_phim")]
    public class RapPhim
    {
        [Key]
        [Column("ma_rap")]
        public int MaRap { get; set; }

        [Required]
        [Column("ten_rap")]
        [StringLength(100)]
        public string TenRap { get; set; }

        [Required]
        [Column("dia_chi")]
        [StringLength(255)]
        public string DiaChi { get; set; }

        [Column("thanh_pho")]
        [StringLength(50)]
        public string ThanhPho { get; set; }

        public virtual ICollection<PhongChieu> PhongChieus { get; set; }
    }
} 