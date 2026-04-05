using CinemaBooking.Models;
using System.Collections.Generic;

namespace CinemaBooking.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // TODAY'S KPI (PRO)
        public int TodayTickets { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayUsers { get; set; }
        
        // TRENDS (PRO: "So What?" Rule) - Percentage change from Yesterday
        public double TicketsTrend { get; set; }
        public double RevenueTrend { get; set; }
        public double UsersTrend { get; set; }
        
        // CHART DATA (PRO: Visuals)
        public List<DailyRevenueData> RevenueHistory { get; set; } = new List<DailyRevenueData>();
        public List<MovieSalesData> HotMovies { get; set; } = new List<MovieSalesData>();
        
        public List<DatVe> RecentBookings { get; set; } = new List<DatVe>();
    }

    public class DailyRevenueData
    {
        public string DateLabel { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MovieSalesData
    {
        public string MovieTitle { get; set; }
        public int TicketCount { get; set; }
    }
}
