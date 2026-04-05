using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CinemaBooking.Models.Services
{
    public class MomoService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly PaymentLogger _logger;

        public MomoService(
            IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor, 
            HttpClient httpClient,
            PaymentLogger logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> CreatePaymentUrl(int maDatVe, string tenPhim, decimal soTien)
        {
            // Lấy các thông tin cấu hình từ appsettings.json
            string endpoint = _configuration["Momo:Endpoint"];
            string partnerCode = _configuration["Momo:PartnerCode"];
            string accessKey = _configuration["Momo:AccessKey"];
            string secretKey = _configuration["Momo:SecretKey"];
            string redirectUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/ThanhToan/MomoReturn";
            string ipnUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/ThanhToan/MomoNotify";

            // Tạo thông tin thanh toán
            string orderId = $"{maDatVe}-{DateTime.Now.Ticks}";
            string requestId = Guid.NewGuid().ToString();
            string orderInfo = $"Thanh toán vé xem phim: {tenPhim}";
            string amount = Convert.ToInt32(soTien).ToString(); // MoMo yêu cầu số nguyên
            string requestType = "captureWallet";
            string extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(""));

            // Tạo chữ ký để xác thực - đúng thứ tự theo yêu cầu của MoMo
            var rawSignature = new StringBuilder();
            rawSignature.Append($"accessKey={accessKey}");
            rawSignature.Append($"&amount={amount}");
            rawSignature.Append($"&extraData={extraData}");
            rawSignature.Append($"&ipnUrl={ipnUrl}");
            rawSignature.Append($"&orderId={orderId}");
            rawSignature.Append($"&orderInfo={orderInfo}");
            rawSignature.Append($"&partnerCode={partnerCode}");
            rawSignature.Append($"&redirectUrl={redirectUrl}");
            rawSignature.Append($"&requestId={requestId}");
            rawSignature.Append($"&requestType={requestType}");

            string rawHash = rawSignature.ToString();
            System.Diagnostics.Debug.WriteLine("Raw hash string: " + rawHash);
            string signature = ComputeHmacSha256(rawHash, secretKey);
            System.Diagnostics.Debug.WriteLine("Computed signature: " + signature);

            // Tạo dữ liệu yêu cầu
            var requestData = new
            {
                partnerCode = partnerCode,
                partnerName = "Cinema Booking",
                storeId = partnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            // Log request data
            string requestJson = JsonSerializer.Serialize(requestData);
            System.Diagnostics.Debug.WriteLine("MoMo Request: " + requestJson);
            _logger.LogMomoRequest(requestId, requestData);

            // Gửi yêu cầu và nhận phản hồi từ MoMo
            var requestContent = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");

            try
            {
                // Set timeout for MoMo request
                _httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = await _httpClient.PostAsync(endpoint, requestContent);
                string responseContent = await response.Content.ReadAsStringAsync();

                // Log response data
                System.Diagnostics.Debug.WriteLine("MoMo Response: " + responseContent);
                _logger.LogMomoResponse(requestId, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true
                    };

                    var responseData = JsonSerializer.Deserialize<MomoResponse>(responseContent, options);

                    if (responseData?.ResultCode == 0)
                    {
                        return responseData.PayUrl;
                    }

                    // Handle specific MoMo error codes
                    string errorMessage = GetMomoErrorMessage(responseData?.ResultCode ?? -1);
                    var exception = new Exception($"Lỗi từ MoMo: {errorMessage} (Code: {responseData?.ResultCode})");
                    _logger.LogPaymentError("MoMo", requestId, exception);
                    throw exception;
                }

                var httpException = new Exception($"Không thể kết nối đến MoMo. HTTP Status: {response.StatusCode}. Response: {responseContent}");
                _logger.LogPaymentError("MoMo", requestId, httpException);
                throw httpException;
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine("JSON Exception: " + jsonEx.Message);
                _logger.LogPaymentError("MoMo", requestId, jsonEx);
                throw new Exception("Lỗi định dạng JSON từ MoMo: " + jsonEx.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("General Exception: " + ex.Message);
                _logger.LogPaymentError("MoMo", requestId, ex);
                throw new Exception("Lỗi khi kết nối đến MoMo: " + ex.Message);
            }
        }

        // Phương thức xác minh chữ ký từ MoMo
        public bool ValidateSignature(Dictionary<string, string> requestData, string receivedSignature)
        {
            string secretKey = _configuration["Momo:SecretKey"];
            StringBuilder rawData = new StringBuilder();
            
            // Sắp xếp các tham số theo thứ tự alphabet
            var sortedParams = new SortedDictionary<string, string>(requestData);
            
            foreach (var item in sortedParams)
            {
                // Bỏ qua signature
                if (item.Key.Equals("signature", StringComparison.OrdinalIgnoreCase))
                    continue;

                rawData.Append($"&{item.Key}={item.Value}");
            }
            
            // Xóa dấu & ở đầu chuỗi
            if (rawData.Length > 0)
                rawData.Remove(0, 1);
            
            // Tính toán chữ ký
            string calculatedSignature = ComputeHmacSha256(rawData.ToString(), secretKey);
            
            // So sánh chữ ký
            return calculatedSignature.Equals(receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetMomoErrorMessage(int resultCode)
        {
            return resultCode switch
            {
                0 => "Thành công",
                9 => "Merchant không được phép thực hiện giao dịch",
                10 => "Giao dịch không thành công do tài khoản của khách hàng không đủ số dư",
                11 => "Giao dịch không thành công do số tiền vượt quá hạn mức thanh toán",
                12 => "Giao dịch không thành công do thông tin thẻ/tài khoản không chính xác",
                13 => "Giao dịch không thành công do OTP không chính xác",
                20 => "Giao dịch không thành công do vượt quá số lần nhập sai OTP",
                21 => "Giao dịch không thành công do vượt quá thời gian thanh toán",
                40 => "RequestId bị trùng",
                41 => "OrderId bị trùng",
                42 => "OrderId không hợp lệ hoặc không được tìm thấy",
                43 => "Số tiền giao dịch không hợp lệ",
                99 => "Lỗi không xác định hoặc hệ thống MoMo đang bảo trì",
                1000 => "Giao dịch được khởi tạo, chờ người dùng xác nhận thanh toán",
                1001 => "Giao dịch thành công",
                1002 => "Giao dịch thất bại",
                1003 => "Giao dịch bị hủy",
                1004 => "Giao dịch bị từ chối",
                1005 => "Giao dịch hết hạn",
                1006 => "Giao dịch đang được xử lý",
                _ => $"Lỗi không xác định (Code: {resultCode})"
            };
        }

        private static string ComputeHmacSha256(string message, string secretKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashMessage = hmac.ComputeHash(messageBytes);

                // Convert to lowercase hexits for MoMo
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashMessage)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }

    public class MomoResponse
    {
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public long Amount { get; set; }
        public JsonElement ResponseTime { get; set; } // Sử dụng JsonElement để xử lý được cả chuỗi và số
        public string Message { get; set; }
        public int ResultCode { get; set; }
        public string PayUrl { get; set; }
    }
} 