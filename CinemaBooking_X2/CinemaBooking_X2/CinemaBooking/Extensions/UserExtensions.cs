using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CinemaBooking.Models;
using CinemaBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// Gets the legacy user ID (MaNguoiDung) for the current user.
        /// This method handles both Identity users and legacy users.
        /// </summary>
        public static async Task<int?> GetLegacyUserIdAsync(this ClaimsPrincipal user, ApplicationDbContext context)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            // 1. First try to get from legacy claim (for backward compatibility)
            var legacyUserIdClaim = user.FindFirst("MaNguoiDung")?.Value;
            if (!string.IsNullOrEmpty(legacyUserIdClaim) && int.TryParse(legacyUserIdClaim, out int legacyUserId))
            {
                return legacyUserId;
            }

            // 2. Try to get email to look up legacy user
            var email = user.GetUserEmail();
            if (!string.IsNullOrEmpty(email))
            {
                // Find the corresponding legacy user by email
                var nguoiDung = await context.NguoiDungs
                    .FirstOrDefaultAsync(n => n.Email == email);
                
                if (nguoiDung != null)
                {
                    return nguoiDung.MaNguoiDung;
                }
            }

            // 3. Fallback: try to look up by Identity Name (often Username/Email)
            if (!string.IsNullOrEmpty(user.Identity.Name))
            {
                var nguoiDung = await context.NguoiDungs
                    .FirstOrDefaultAsync(n => n.TenDangNhap == user.Identity.Name || n.Email == user.Identity.Name);
                
                if (nguoiDung != null)
                {
                    return nguoiDung.MaNguoiDung;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the current user's email from claims
        /// </summary>
        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return string.Empty;

            // Try different claim types for email
            var email = user.FindFirst(ClaimTypes.Email)?.Value ??
                        user.FindFirst("email")?.Value ??
                        user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            // Fallback: If no email claim, check if Identity.Name looks like an email
            if (string.IsNullOrEmpty(email) && user.Identity.Name != null && user.Identity.Name.Contains("@"))
            {
                email = user.Identity.Name;
            }

            return email ?? string.Empty;
        }

        /// <summary>
        /// Gets the current user's full name from claims
        /// </summary>
        public static string GetUserFullName(this ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return "Khách";

            return user.FindFirst("FullName")?.Value ??
                   user.FindFirst(ClaimTypes.Name)?.Value ??
                   user.FindFirst("HoTen")?.Value ??
                   user.Identity.Name ??
                   "Người dùng";
        }
    }
}
