using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("ngon_ngu_phim")]
    public class NgonNguPhim
    {
        [Key]
        [Column("ma_ngon_ngu")]
        public int MaNgonNgu { get; set; }

        [Column("ma_phim")]
        public int? MaPhim { get; set; }

        [Required]
        [Column("ngon_ngu")]
        [StringLength(50)]
        public string NgonNgu { get; set; }

        [Column("phu_de")]
        [StringLength(50)]
        public string PhuDe { get; set; }

        [ForeignKey("MaPhim")]
        public virtual Phim? Phim { get; set; }
        
        public virtual ICollection<LichChieu> LichChieus { get; set; } = new List<LichChieu>();
    }
} 