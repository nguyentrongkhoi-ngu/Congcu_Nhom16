using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;

namespace CinemaBooking.Controllers
{
    public class RapPhimController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RapPhimController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim
        public async Task<IActionResult> Index(string citySearch, string nameSearch)
        {
            var today = DateTime.Today;
            var query = _context.RapPhims
                .Include(r => r.PhongChieus)
                .AsQueryable();

            if (!string.IsNullOrEmpty(citySearch))
            {
                query = query.Where(r => r.ThanhPho.Contains(citySearch));
            }

            if (!string.IsNullOrEmpty(nameSearch))
            {
                query = query.Where(r => r.TenRap.Contains(nameSearch));
            }

            var cinemas = await query.ToListAsync();
            
            // Get unique cities for filter dropdown
            ViewBag.Cities = await _context.RapPhims
                .Select(r => r.ThanhPho)
                .Distinct()
                .ToListAsync();
                
            ViewBag.CurrentCity = citySearch;
            ViewBag.CurrentName = nameSearch;

            return View(cinemas);
        }
    }
}
