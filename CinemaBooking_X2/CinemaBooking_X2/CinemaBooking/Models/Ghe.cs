using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CinemaBooking.Models
{
    [Table("ghe")]
    public class Ghe
    {
        [Key]
        [Column("ma_ghe")]
        public int MaGhe { get; set; }

        [Required]
        [Column("ma_phong")]
        public int MaPhong { get; set; }

        [Required]
        [Column("so_ghe")]
        [StringLength(10)]
        public string SoGhe { get; set; } = string.Empty;

        [Column("loai_ghe")]
        [StringLength(20)]
        public string LoaiGhe { get; set; } = "Thường"; // Giá trị mặc định là "Thường", có thể là "VIP" hoặc "Sweetbox"

        [ForeignKey("MaPhong")]
        public virtual PhongChieu PhongChieu { get; set; } = null!;

        public virtual ICollection<DatVeGhe> DatVeGhes { get; set; } = new List<DatVeGhe>();
    }
} 