using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    [Table("otp_info")]
    public class OtpInfo
    {
        [Key]
        [Column("ma_otp")]
        public int MaOtp { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [StringLength(6)]
        [Column("ma_xac_thuc")]
        public string MaXacThuc { get; set; }

        [Required]
        [Column("thoi_gian_tao")]
        public DateTime ThoiGianTao { get; set; }

        [Required]
        [Column("thoi_gian_het_han")]
        public DateTime ThoiGianHetHan { get; set; }

        [Required]
        [StringLength(20)]
        [Column("loai_otp")]
        public string LoaiOtp { get; set; } // "DangKy", "QuenMatKhau", ...

        [Required]
        [Column("da_su_dung")]
        public bool DaSuDung { get; set; }

        [Column("ma_nguoi_dung")]
        public int? MaNguoiDung { get; set; }

        // For Identity integration
        [Column("user_id")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}