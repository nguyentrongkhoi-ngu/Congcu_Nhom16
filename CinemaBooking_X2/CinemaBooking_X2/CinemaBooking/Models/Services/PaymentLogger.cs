using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CinemaBooking.Models.Services
{
    public class PaymentLogger
    {
        private readonly IConfiguration _configuration;
        private readonly string _logPath;

        public PaymentLogger(IConfiguration configuration)
        {
            _configuration = configuration;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _logPath = Path.Combine(baseDirectory, "Logs", "Payments");
            
            // Đảm bảo thư mục log tồn tại
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
        }

        public void LogMomoRequest(string requestId, object requestData)
        {
            try
            {
                string fileName = $"momo_req_{requestId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_logPath, fileName);
                string jsonData = JsonSerializer.Serialize(requestData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(filePath, jsonData, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging Momo request: {ex.Message}");
            }
        }

        public void LogMomoResponse(string requestId, string responseData)
        {
            try
            {
                string fileName = $"momo_res_{requestId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_logPath, fileName);
                
                // Attempt to format the JSON if possible
                try
                {
                    var jsonDocument = JsonDocument.Parse(responseData);
                    responseData = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                }
                catch
                {
                    // If parsing fails, just log the raw response
                }
                
                File.WriteAllText(filePath, responseData, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging Momo response: {ex.Message}");
            }
        }

        public void LogPaymentError(string paymentMethod, string requestId, Exception exception)
        {
            try
            {
                string fileName = $"error_{paymentMethod}_{requestId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(_logPath, fileName);
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== Payment Error - {paymentMethod} ===");
                sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Request ID: {requestId}");
                sb.AppendLine($"Error Message: {exception.Message}");
                sb.AppendLine($"Stack Trace: {exception.StackTrace}");
                
                if (exception.InnerException != null)
                {
                    sb.AppendLine($"Inner Exception: {exception.InnerException.Message}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging payment error: {ex.Message}");
            }
        }
    }
} 