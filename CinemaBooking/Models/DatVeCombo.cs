using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("dat_ve_combo")]
    public class DatVeCombo
    {
        [Key, Column("ma_dat_ve", Order = 0)]
        public int MaDatVe { get; set; }

        [Key, Column("ma_combo", Order = 1)]
        public int MaCombo { get; set; }

        [Required]
        [Column("so_luong")]
        public int SoLuong { get; set; }

        [ForeignKey("MaDatVe")]
        public virtual DatVe DatVe { get; set; }

        [ForeignKey("MaCombo")]
        public virtual Combo Combo { get; set; }
    }
}
