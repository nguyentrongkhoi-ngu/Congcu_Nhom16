using System;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.Services
{
    public static class PasswordHasher
    {
        // Hash mật khẩu
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Xác thực mật khẩu
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }
    }
} 