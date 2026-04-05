using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("combo")]
    public class Combo
    {
        [Key]
        [Column("ma_combo")]
        public int MaCombo { get; set; }

        [Required]
        [Column("ten_combo")]
        [StringLength(100)]
        public string TenCombo { get; set; }

        [Column("mo_ta")]
        public string MoTa { get; set; }

        [Required]
        [Column("gia", TypeName = "decimal(10, 2)")]
        public decimal Gia { get; set; }

        [Column("hinh_anh")]
        [StringLength(255)]
        public string HinhAnh { get; set; }

        [NotMapped]
        public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }

        [StringLength(50)]
        public string? Loai { get; set; } = "Combo";

        [Column("trang_thai")]
        public bool TrangThai { get; set; } = true;

        [Column("so_luong_ton")]
        public int SoLuongTon { get; set; } = 999;

        [Column("uu_tien")]
        public int UuTien { get; set; } = 0;

        [Column("nhan")]
        [StringLength(50)]
        public string? Nhan { get; set; }

        [Column("kich_thuoc")]
        [StringLength(50)]
        public string? KichThuoc { get; set; }

        public virtual ICollection<DatVeCombo> DatVeCombos { get; set; }
    }
}
