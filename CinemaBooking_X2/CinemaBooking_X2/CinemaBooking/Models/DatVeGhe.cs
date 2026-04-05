using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("dat_ve_ghe")]
    public class DatVeGhe
    {
        [Column("ma_dat_ve")]
        public int MaDatVe { get; set; }

        [Column("ma_ghe")]
        public int MaGhe { get; set; }

        [ForeignKey("MaDatVe")]
        public virtual DatVe? DatVe { get; set; }

        [ForeignKey("MaGhe")]
        public virtual Ghe? Ghe { get; set; }
    }
} 