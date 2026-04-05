using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CinemaBooking.Models;

namespace CinemaBooking.Models.ViewModels
{
    public class SearchViewModel
    {
        [Display(Name = "Tìm kiếm")]
        public string SearchTerm { get; set; }

        [Display(Name = "Ngày chiếu")]
        [DataType(DataType.Date)]
        public DateTime? SearchDate { get; set; }

        public List<Phim> Results { get; set; } = new List<Phim>();
    }
}
