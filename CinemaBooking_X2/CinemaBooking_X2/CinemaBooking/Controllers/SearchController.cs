using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;

namespace CinemaBooking.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Search
        public async Task<IActionResult> Index(string searchTerm, DateTime? searchDate)
        {
            var viewModel = new SearchViewModel
            {
                SearchTerm = searchTerm,
                SearchDate = searchDate
            };

            if (!string.IsNullOrEmpty(searchTerm) || searchDate.HasValue)
            {
                // Tìm kiếm phim theo tên
                var query = _context.Phims
                    .Include(p => p.LichChieus)
                        .ThenInclude(l => l.PhongChieu)
                            .ThenInclude(p => p.RapPhim)
                    .AsQueryable();

                // Lọc theo tên phim nếu có
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(p => p.TenPhim.Contains(searchTerm) ||
                                           p.TheLoai.Contains(searchTerm));
                }

                // Lọc theo ngày chiếu nếu có
                if (searchDate.HasValue)
                {
                    query = query.Where(p => p.LichChieus.Any(l => l.NgayChieu.Date == searchDate.Value.Date));
                }

                // Lấy kết quả
                viewModel.Results = await query.ToListAsync();
            }

            return View(viewModel);
        }

        // GET: Search/MoviesByDate
        public async Task<IActionResult> MoviesByDate(DateTime? date)
        {
            // Nếu không có ngày được chọn, sử dụng ngày hiện tại
            var searchDate = date ?? DateTime.Now.Date;

            // Lấy danh sách phim có lịch chiếu vào ngày được chọn
            var movies = await _context.Phims
                .Include(p => p.LichChieus)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .Where(p => p.LichChieus.Any(l => l.NgayChieu.Date == searchDate.Date))
                .ToListAsync();

            // Truyền ngày tìm kiếm vào ViewBag
            ViewBag.Date = searchDate;

            return View("MoviesByDateEnhanced", movies);
        }
    }
}
