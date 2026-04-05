using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;

namespace CinemaBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            // 1. TOTALS
            var totalUsers = await _context.NguoiDungs.CountAsync();
            var totalMovies = await _context.Phims.CountAsync();
            var totalBookings = await _context.DatVes.CountAsync();
            var totalRevenue = await _context.ThanhToans
                .Where(t => t.TrangThai == "Thành công")
                .SumAsync(t => t.SoTien);

            // 2. TODAY'S STATS
            var todayUsers = await _context.NguoiDungs.CountAsync(u => u.NgayTao >= today);
            var todayTickets = await _context.DatVeGhes
                .Include(dg => dg.DatVe)
                .CountAsync(dg => dg.DatVe.NgayDat >= today && dg.DatVe.TrangThai != "Đã hủy");
            var todayRevenue = await _context.ThanhToans
                .Where(t => t.TrangThai == "Thành công" && t.NgayThanhToan >= today)
                .SumAsync(t => t.SoTien);

            // 3. YESTERDAY'S STATS (for Trends)
            var yesterdayUsers = await _context.NguoiDungs.CountAsync(u => u.NgayTao >= yesterday && u.NgayTao < today);
            var yesterdayTickets = await _context.DatVeGhes
                .Include(dg => dg.DatVe)
                .CountAsync(dg => dg.DatVe.NgayDat >= yesterday && dg.DatVe.NgayDat < today && dg.DatVe.TrangThai != "Đã hủy");
            var yesterdayRevenue = await _context.ThanhToans
                .Where(t => t.TrangThai == "Thành công" && t.NgayThanhToan >= yesterday && t.NgayThanhToan < today)
                .SumAsync(t => t.SoTien);

            // 4. CALCULATE TRENDS (PRO: "So What?" Rule)
            double usersTrend = yesterdayUsers == 0 ? (todayUsers > 0 ? 100 : 0) : Math.Round((double)(todayUsers - yesterdayUsers) / yesterdayUsers * 100, 1);
            double ticketsTrend = yesterdayTickets == 0 ? (todayTickets > 0 ? 100 : 0) : Math.Round((double)(todayTickets - yesterdayTickets) / yesterdayTickets * 100, 1);
            double revenueTrend = yesterdayRevenue == 0 ? (todayRevenue > 0 ? 100 : 0) : Math.Round((double)(todayRevenue - yesterdayRevenue) / (double)yesterdayRevenue * 100, 1);

            // 5. REVENUE HISTORY (Last 7 days for Chart)
            var revenueHistory = new List<DailyRevenueData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDate = date.AddDays(1);
                var dailyRev = await _context.ThanhToans
                    .Where(t => t.TrangThai == "Thành công" && t.NgayThanhToan >= date && t.NgayThanhToan < nextDate)
                    .SumAsync(t => t.SoTien);
                
                revenueHistory.Add(new DailyRevenueData 
                { 
                    DateLabel = date.ToString("dd/MM"), 
                    Revenue = dailyRev 
                });
            }

            // 6. HOT MOVIES (Top 5 by ticket count)
            var hotMovies = await _context.DatVeGhes
                .Include(dg => dg.DatVe)
                    .ThenInclude(dv => dv.LichChieu)
                        .ThenInclude(lc => lc.Phim)
                .Where(dg => dg.DatVe.TrangThai != "Đã hủy")
                .GroupBy(dg => dg.DatVe.LichChieu.Phim.TenPhim)
                .Select(g => new MovieSalesData
                {
                    MovieTitle = g.Key,
                    TicketCount = g.Count()
                })
                .OrderByDescending(m => m.TicketCount)
                .Take(5)
                .ToListAsync();

            var stats = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalMovies = totalMovies,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                TodayUsers = todayUsers,
                TodayTickets = todayTickets,
                TodayRevenue = todayRevenue,
                UsersTrend = usersTrend,
                TicketsTrend = ticketsTrend,
                RevenueTrend = revenueTrend,
                RevenueHistory = revenueHistory,
                HotMovies = hotMovies,
                RecentBookings = await _context.DatVes
                    .Include(d => d.NguoiDung)
                    .Include(d => d.LichChieu)
                        .ThenInclude(l => l.Phim)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(10)
                    .ToListAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Movies()
        {
            return View();
        }

        public IActionResult Bookings()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
