using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("vai_tro")]
    public class VaiTro
    {
        [Key]
        [Column("ma_vai_tro")]
        public int MaVaiTro { get; set; }

        [Required]
        [Column("ten_vai_tro")]
        [StringLength(50)]
        public string TenVaiTro { get; set; } = string.Empty;

        [Column("mo_ta")]
        [StringLength(255)]
        public string? MoTa { get; set; }

        public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
    }
}