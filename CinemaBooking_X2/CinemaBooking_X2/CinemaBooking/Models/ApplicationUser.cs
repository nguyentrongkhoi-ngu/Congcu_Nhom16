using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? HoTen { get; set; }

        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        public DateTime? NgayTao { get; set; }

        // Navigation properties to existing entities
        public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
        public virtual ICollection<DatVe> DatVes { get; set; } = new List<DatVe>();
        public virtual ICollection<LichSuGiaoDich> LichSuGiaoDichs { get; set; } = new List<LichSuGiaoDich>();
        public virtual ICollection<OtpInfo> OtpInfos { get; set; } = new List<OtpInfo>();
    }
}
