using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using CinemaBooking.Models;
using CinemaBooking.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PhimController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PhimController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Phim
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? maRap)
        {
            var today = DateTime.Today;
            var query = _context.Phims.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.TenPhim.Contains(searchString) || p.TheLoai.Contains(searchString));
            }

            if (maRap.HasValue)
            {
                query = query.Where(p => p.LichChieus.Any(l => l.PhongChieu.MaRap == maRap.Value));
                var cinema = await _context.RapPhims.FindAsync(maRap.Value);
                ViewBag.SelectedCinemaName = cinema?.TenRap;
            }

            var allMovies = await query
                .Include(p => p.LichChieus)
                .ToListAsync();

            var nowShowingMovies = allMovies
                .Where(p => p.LichChieus != null && p.LichChieus.Any(l => l.NgayChieu >= today))
                .OrderByDescending(p => p.LichChieus.Max(l => l.NgayChieu))
                .ToList();

            var comingSoonMovies = allMovies
                .Where(p => (p.NgayPhatHanh > today && (p.LichChieus == null || !p.LichChieus.Any(l => l.NgayChieu <= today))) || (p.NgayPhatHanh == null))
                .OrderBy(p => p.NgayPhatHanh)
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.SelectedMaRap = maRap;
            ViewBag.NowShowingMovies = nowShowingMovies;
            ViewBag.ComingSoonMovies = comingSoonMovies;

            return View(allMovies);
        }

        // GET: Phim/SearchSuggest?query=...
        [AllowAnonymous]
        public async Task<IActionResult> SearchSuggest(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Content("");
            }

            var results = await _context.Phims
                .Where(p => p.TenPhim.Contains(query) || p.TheLoai.Contains(query))
                .OrderByDescending(p => p.DiemIMDb)
                .Take(6)
                .ToListAsync();

            return PartialView("_SearchSuggestPartial", results);
        }

        // GET: Phim/Detail/5
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.NgonNguPhims)
                .Include(p => p.LichChieus!)
                    .ThenInclude(l => l.PhongChieu)
                        .ThenInclude(p => p.RapPhim)
                .FirstOrDefaultAsync(m => m.MaPhim == id);
                
            if (phim == null)
            {
                return NotFound();
            }

            // Kiểm tra xem phim có lịch chiếu trong tương lai không
            var now = DateTime.Now;
            bool hasScreenings = phim.LichChieus != null && phim.LichChieus
                .Any(l => l.NgayChieu.Date > now.Date || 
                    (l.NgayChieu.Date == now.Date && l.GioChieu > now.TimeOfDay));
            
            ViewBag.HasFutureScreenings = hasScreenings;

            // Lấy danh sách đánh giá của phim
            var danhGias = await _context.DanhGias
                .Include(d => d.NguoiDung)
                .Where(d => d.MaPhim == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToListAsync();

            // Tính điểm trung bình
            double diemTrungBinh = 0;
            if (danhGias.Any())
            {
                diemTrungBinh = danhGias.Average(d => d.DiemSo ?? 0);
            }

            // Tạo ViewModel
            var viewModel = new PhimDanhGiaViewModel
            {
                Phim = phim,
                DanhSachDanhGia = danhGias,
                DiemTrungBinh = Math.Round(diemTrungBinh, 1),
                TongSoDanhGia = danhGias.Count
            };

            return View(viewModel);
        }

        // GET: Phim/Create
        public IActionResult Create()
        {
            return View(new PhimViewModel());
        }

        // POST: Phim/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhimViewModel model)
        {
            try
            {
                // Kiểm tra xem đã chọn file chưa
                if (model.PosterFile == null || model.PosterFile.Length == 0)
                {
                    ModelState.AddModelError("PosterFile", "Vui lòng chọn ảnh poster cho phim");
                }
                
                if (model.TrailerFile == null || model.TrailerFile.Length == 0)
                {
                    ModelState.AddModelError("TrailerFile", "Vui lòng chọn file trailer cho phim");
                }
                
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        foreach (var error in state.Errors)
                        {
                            Console.WriteLine($"Error in {key}: {error.ErrorMessage}");
                        }
                    }
                    
                    return View(model);
                }
                
                // Kiểm tra kích thước file
                long maxFileSizeMB = 100; // Tăng giới hạn lên 100MB
                
                if (model.PosterFile != null && model.PosterFile.Length > maxFileSizeMB * 1024 * 1024)
                {
                    ModelState.AddModelError("PosterFile", $"Kích thước file quá lớn. Tối đa {maxFileSizeMB}MB.");
                    return View(model);
                }
                
                if (model.TrailerFile != null && model.TrailerFile.Length > maxFileSizeMB * 1024 * 1024)
                {
                    ModelState.AddModelError("TrailerFile", $"Kích thước file quá lớn. Tối đa {maxFileSizeMB}MB.");
                    return View(model);
                }
                
                // Xử lý upload poster
                string posterUrl = null;
                if (model.PosterFile != null)
                {
                    Console.WriteLine($"Uploading poster file: {model.PosterFile.FileName}, Size: {model.PosterFile.Length} bytes");
                    posterUrl = await UploadFile(model.PosterFile, "posters");
                    Console.WriteLine($"Poster URL: {posterUrl}");
                }
                
                // Xử lý upload trailer
                string trailerUrl = null;
                if (model.TrailerFile != null)
                {
                    Console.WriteLine($"Uploading trailer file: {model.TrailerFile.FileName}, Size: {model.TrailerFile.Length} bytes");
                    trailerUrl = await UploadFile(model.TrailerFile, "trailers");
                    Console.WriteLine($"Trailer URL: {trailerUrl}");
                }

                // Tạo đối tượng phim mới
                var phim = new Phim
                {
                    TenPhim = model.TenPhim,
                    MoTa = model.MoTa,
                    ThoiLuong = model.ThoiLuong,
                    TheLoai = model.TheLoai,
                    NgayPhatHanh = model.NgayPhatHanh,
                    DinhDang = model.DinhDang,
                    UrlPoster = posterUrl,
                    Trailer = trailerUrl
                };

                Console.WriteLine($"Saving movie: {phim.TenPhim}");
                _context.Add(phim);
                await _context.SaveChangesAsync();
                Console.WriteLine("Movie saved successfully");
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating movie: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                ModelState.AddModelError(string.Empty, $"Lỗi khi lưu phim: {ex.Message}");
                return View(model);
            }
        }

        // GET: Phim/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }

            var viewModel = new PhimViewModel
            {
                MaPhim = phim.MaPhim,
                TenPhim = phim.TenPhim,
                MoTa = phim.MoTa,
                ThoiLuong = phim.ThoiLuong,
                TheLoai = phim.TheLoai,
                NgayPhatHanh = phim.NgayPhatHanh,
                DinhDang = phim.DinhDang,
                UrlPoster = phim.UrlPoster,
                Trailer = phim.Trailer
            };

            return View(viewModel);
        }

        // POST: Phim/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PhimViewModel model)
        {
            if (id != model.MaPhim)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy thông tin phim hiện tại
                    var phim = await _context.Phims.FindAsync(id);
                    if (phim == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin
                    phim.TenPhim = model.TenPhim ?? phim.TenPhim;
                    phim.MoTa = model.MoTa ?? phim.MoTa;
                    phim.ThoiLuong = model.ThoiLuong;
                    phim.TheLoai = model.TheLoai ?? phim.TheLoai;
                    phim.NgayPhatHanh = model.NgayPhatHanh;
                    phim.DinhDang = model.DinhDang ?? phim.DinhDang;
                    phim.UrlPoster = model.UrlPoster ?? phim.UrlPoster;
                    phim.Trailer = model.Trailer ?? phim.Trailer;

                    // Xử lý upload poster mới (nếu có)
                    if (model.PosterFile != null)
                    {
                        DeleteFile(phim.UrlPoster);
                        string? newPosterUrl = await UploadFile(model.PosterFile, "posters");
                        phim.UrlPoster = newPosterUrl ?? phim.UrlPoster;
                    }

                    // Xử lý upload trailer mới (nếu có)
                    if (model.TrailerFile != null)
                    {
                        DeleteFile(phim.Trailer);
                        string? newTrailerUrl = await UploadFile(model.TrailerFile, "trailers");
                        phim.Trailer = newTrailerUrl ?? phim.Trailer;
                    }

                    _context.Update(phim);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhimExists(model.MaPhim))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi khi cập nhật phim: {ex.Message}");
                }
            }
            return View(model);
        }

        // GET: Phim/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .FirstOrDefaultAsync(m => m.MaPhim == id);
            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // POST: Phim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phim = await _context.Phims.FindAsync(id);
            
            if (phim == null)
            {
                return NotFound();
            }

            // Xóa file ảnh và video
            DeleteFile(phim.UrlPoster);
            DeleteFile(phim.Trailer);

            _context.Phims.Remove(phim);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult TestUpload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file selected" });
                }
                
                var result = new Dictionary<string, object>
                {
                    { "success", true },
                    { "fileName", file.FileName },
                    { "fileSize", file.Length },
                    { "contentType", file.ContentType }
                };
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Helper methods for file upload and deletion
        private async Task<string?> UploadFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            try
            {
                // Tạo tên file duy nhất
                string uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(file.FileName)}";
                
                // Đường dẫn lưu file
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderName);
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Sử dụng buffer nhỏ hơn và ghi file dần dần khi là video lớn
                if (file.Length > 10 * 1024 * 1024) // Nếu lớn hơn 10MB
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        const int bufferSize = 1024 * 1024; // 1MB buffer
                        byte[] buffer = new byte[bufferSize];
                        
                        using (var fileStream = file.OpenReadStream())
                        {
                            int bytesRead;
                            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, bytesRead);
                                await stream.FlushAsync();
                            }
                        }
                    }
                }
                else
                {
                    // Lưu file nhỏ hơn bằng cách thông thường
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                }
                
                // Trả về đường dẫn tương đối
                return $"/{folderName}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Error uploading file: {ex.Message}");
                throw new Exception($"Lỗi khi upload file: {ex.Message}", ex);
            }
        }

        private void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return;
            }

            try
            {
                // Đường dẫn tuyệt đối tới file
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                // Log lỗi
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        private bool PhimExists(int id)
        {
            return _context.Phims.Any(e => e.MaPhim == id);
        }
    }
} 