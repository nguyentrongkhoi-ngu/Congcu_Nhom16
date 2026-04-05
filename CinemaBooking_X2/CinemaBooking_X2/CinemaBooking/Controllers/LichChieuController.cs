using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaBooking.Controllers
{
    public class LichChieuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichChieuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LichChieu
        public async Task<IActionResult> Index(LichChieuListViewModel viewModel = null)
        {
            if (viewModel == null)
            {
                viewModel = new LichChieuListViewModel();
            }

            // Lấy danh sách phim và rạp cho dropdown filter
            viewModel.PhimList = new SelectList(await _context.Phims.OrderBy(p => p.TenPhim).ToListAsync(), "MaPhim", "TenPhim");
            viewModel.RapList = new SelectList(await _context.RapPhims.OrderBy(r => r.TenRap).ToListAsync(), "MaRap", "TenRap");

            // Truy vấn cơ bản
            var query = _context.LichChieus
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                    .ThenInclude(p => p.RapPhim)
                .Include(l => l.NgonNguPhim)
                .AsQueryable();

            // Chỉ hiển thị lịch chiếu ở hiện tại và tương lai
            var now = DateTime.Now;
            query = query.Where(l => l.NgayChieu > now.Date || (l.NgayChieu == now.Date && l.GioChieu >= now.TimeOfDay));

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(viewModel.SearchTerm))
            {
                query = query.Where(l => l.Phim.TenPhim.Contains(viewModel.SearchTerm) || 
                                        l.PhongChieu.RapPhim.TenRap.Contains(viewModel.SearchTerm));
            }

            if (viewModel.NgayBatDau.HasValue)
            {
                query = query.Where(l => l.NgayChieu >= viewModel.NgayBatDau.Value);
            }

            if (viewModel.NgayKetThuc.HasValue)
            {
                query = query.Where(l => l.NgayChieu <= viewModel.NgayKetThuc.Value);
            }

            if (viewModel.MaPhim.HasValue)
            {
                query = query.Where(l => l.MaPhim == viewModel.MaPhim.Value);
            }

            if (viewModel.MaRap.HasValue)
            {
                query = query.Where(l => l.PhongChieu.MaRap == viewModel.MaRap.Value);
            }

            // Sắp xếp
            query = query.OrderBy(l => l.NgayChieu).ThenBy(l => l.GioChieu);

            // Gán kết quả cho view model
            viewModel.LichChieus = await query.ToListAsync();

            return View(viewModel);
        }
    }
}