using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CinemaBooking.Data;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Models;

namespace CinemaBooking.Services
{
    public class LichChieuCleanupService : BackgroundService
    {
        private readonly ILogger<LichChieuCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Chạy mỗi giờ

        public LichChieuCleanupService(
            ILogger<LichChieuCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dịch vụ dọn dẹp lịch chiếu đã khởi động. Đợi 30 giây để hệ thống ổn định...");

            // Đợi 30 giây để SeedData và Migrations hoàn tất, tránh xung đột DB
            try {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            } catch (OperationCanceledException) {
                return;
            }

            // Chạy lần đầu sau khi delay
            try {
                await ProcessExpiredLichChieu(stoppingToken);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Lỗi khi xử lý lịch chiếu hết hạn lần đầu. Sẽ thử lại trong chu kỳ sau.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                    
                    // Bỏ qua nếu token đã yêu cầu hủy
                    if (stoppingToken.IsCancellationRequested)
                        break;
                        
                    await ProcessExpiredLichChieu(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Bình thường khi service bị dừng
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý lịch chiếu hết hạn");
                }
            }
            
            _logger.LogInformation("Dịch vụ dọn dẹp lịch chiếu đã dừng");
        }

        private async Task ProcessExpiredLichChieu(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bắt đầu xử lý lịch chiếu đã chiếu xong");
            
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Lấy thời gian hiện tại
            var now = DateTime.Now;
            var today = now.Date;
            
            // Chỉ lấy các lịch chiếu trong vòng 7 ngày qua để tránh tải quá nhiều dữ liệu cũ (gây chậm/crash)
            var minDate = today.AddDays(-7);
            
            var lichChieuQuery = await dbContext.LichChieus
                .Include(lc => lc.Phim)
                .Where(lc => lc.NgayChieu >= minDate && lc.NgayChieu <= today)
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            // Lọc các lịch chiếu đã hết hạn trong bộ nhớ
            var expiredLichChieu = lichChieuQuery
                .Where(lc => 
                {
                    if (lc.NgayChieu.Date < now.Date) return true;
                    if (lc.NgayChieu.Date > now.Date) return false;
                    
                    // Nếu là ngày hiện tại, kiểm tra lịch chiếu đã kết thúc chưa
                    // Mặc định 120 phút nếu không có thông tin thời lượng phim
                    int duration = (lc.Phim != null && lc.Phim.ThoiLuong > 0) ? lc.Phim.ThoiLuong : 120;
                    return lc.GioChieu.Add(TimeSpan.FromMinutes(duration)) < now.TimeOfDay;
                })
                .ToList();
                
            _logger.LogInformation($"Tìm thấy {expiredLichChieu.Count} lịch chiếu đã hết hạn");
            
            if (!expiredLichChieu.Any())
                return;
                
            // Lấy tất cả các đặt vé liên quan đến các lịch chiếu đã hết hạn
            var maLichChieus = expiredLichChieu.Select(lc => lc.MaLichChieu).ToList();
            var datVes = await dbContext.DatVes
                .Include(dv => dv.DatVeGhes)
                .Where(dv => maLichChieus.Contains(dv.MaLichChieu))
                .ToListAsync(stoppingToken);
                
            _logger.LogInformation($"Tìm thấy {datVes.Count} vé cần xử lý");
            
            // Xóa tất cả các bản ghi dat_ve_ghe liên quan
            foreach (var datVe in datVes)
            {
                if (datVe.DatVeGhes != null && datVe.DatVeGhes.Any())
                {
                    dbContext.DatVeGhes.RemoveRange(datVe.DatVeGhes);
                }
                
                // Cập nhật trạng thái đặt vé thành "Đã hoàn thành"
                if (datVe.TrangThai != "Đã hủy")
                {
                    datVe.TrangThai = "Đã hoàn thành";
                }
            }
            
            // Tạo bản ghi lịch sử giao dịch
            var lichSuGiaoDich = new LichSuGiaoDich
            {
                LoaiGiaoDich = "Hệ thống",
                TrangThai = "Thành công",
                NoiDung = $"Tự động xử lý {expiredLichChieu.Count} lịch chiếu đã kết thúc, giải phóng {datVes.Count} vé",
                NgayGiaoDich = DateTime.Now
            };
            
            dbContext.LichSuGiaoDiches.Add(lichSuGiaoDich);
            
            // Lưu thay đổi vào database
            await dbContext.SaveChangesAsync(stoppingToken);
            
            _logger.LogInformation($"Đã xử lý thành công {expiredLichChieu.Count} lịch chiếu và {datVes.Count} vé");
        }
    }
} 