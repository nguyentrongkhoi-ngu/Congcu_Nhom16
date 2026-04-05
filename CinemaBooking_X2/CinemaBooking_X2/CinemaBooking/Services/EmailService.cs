using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace CinemaBooking.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var fromEmail = emailSettings["Email"];
            var password = emailSettings["Password"];
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CineZore 2026 Admin", fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = body };

            try
            {
                using (var client = new SmtpClient())
                {
                    // For Gmail on port 587, use StartTls
                    await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(fromEmail, password);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    
                    Console.WriteLine($"[EmailService] Email sent successfully to {toEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ERROR sending email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EmailService] Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
        
        public async Task<bool> SendOtpEmailAsync(string toEmail, string otp, string fullName = null)
        {
            var name = string.IsNullOrEmpty(fullName) ? "Quý khách" : fullName;
            var subject = "Mã xác thực đăng ký tài khoản CinemaBooking";
            var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #E50914 0%, #a3060d 100%); color: white; padding: 25px; text-align: center; border-radius: 12px 12px 0 0; }}
                    .content {{ padding: 30px; background-color: #fcfcfc; border-radius: 0 0 12px 12px; }}
                    .otp-box {{ background-color: #f7f7f7; border: 2px dashed #E50914; padding: 20px; text-align: center; margin: 30px 0; border-radius: 8px; }}
                    .otp-code {{ font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #E50914; margin: 0; }}
                    .footer {{ text-align: center; margin-top: 30px; font-size: 13px; color: #888; border-top: 1px solid #eee; padding-top: 20px; }}
                    .btn {{ display: inline-block; padding: 12px 25px; background-color: #E50914; color: white !important; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='margin:0; font-size: 24px;'>CineZore 2026</h1>
                        <p style='margin:5px 0 0; opacity: 0.9;'>Xác thực đăng ký thành viên</p>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{name}</strong>,</p>
                        <p>Cảm ơn bạn đã lựa chọn <strong>CineZore 2026</strong>. Để hoàn tất quy trình bảo mật và kích hoạt tài khoản, vui lòng sử dụng mã xác thực (OTP) dưới đây:</p>
                        
                        <div class='otp-box'>
                            <p style='margin:0 0 10px; font-size: 14px; color: #666; text-transform: uppercase;'>Mã xác nhận của bạn</p>
                            <h2 class='otp-code'>{otp}</h2>
                        </div>
                        
                        <p style='font-size: 14px; color: #d93025;'><em>Lưu ý: Mã này sẽ hết hạn trong vòng 10 phút. Không chia sẻ mã này với bất kỳ ai để bảo vệ tài khoản của bạn.</em></p>
                        
                        <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc liên hệ với bộ phận hỗ trợ nếu cần thiết.</p>
                        
                        <p>Trân trọng,<br><strong>Ban quản trị CineZore</strong></p>
                    </div>
                    <div class='footer'>
                        <p>© 2026 CineZore Cinema System. All rights reserved.</p>
                        <p>Địa chỉ: 123 Cine Way, Cinema District, HCMC</p>
                    </div>
                </div>
            </body>
            </html>";
            
            return await SendEmailAsync(toEmail, subject, body);
        }
        
        public async Task<bool> SendForgotPasswordOtpAsync(string toEmail, string otp, string fullName = null)
        {
            var name = string.IsNullOrEmpty(fullName) ? "Quý khách" : fullName;
            var subject = "Mã xác thực khôi phục mật khẩu CinemaBooking";
            var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #4285F4 0%, #1967D2 100%); color: white; padding: 25px; text-align: center; border-radius: 12px 12px 0 0; }}
                    .content {{ padding: 30px; background-color: #fcfcfc; border-radius: 0 0 12px 12px; }}
                    .otp-box {{ background-color: #f7f7f7; border: 2px dashed #4285F4; padding: 20px; text-align: center; margin: 30px 0; border-radius: 8px; }}
                    .otp-code {{ font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #4285F4; margin: 0; }}
                    .footer {{ text-align: center; margin-top: 30px; font-size: 13px; color: #888; border-top: 1px solid #eee; padding-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='margin:0; font-size: 24px;'>CineZore 2026</h1>
                        <p style='margin:5px 0 0; opacity: 0.9;'>Khôi phục mật khẩu rạp phim</p>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{name}</strong>,</p>
                        <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản CineZore của bạn. Vui lòng sử dụng mã OTP dưới đây để đặt lại mật khẩu mới:</p>
                        
                        <div class='otp-box'>
                            <p style='margin:0 0 10px; font-size: 14px; color: #666;'>Mã khôi phục</p>
                            <h2 class='otp-code'>{otp}</h2>
                        </div>
                        
                        <p style='font-size: 14px; color: #d93025;'><em>Mã này có hiệu lực trong 10 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này để giữ an toàn cho tài khoản.</em></p>
                        <p>Trân trọng,<br><strong>Đội ngũ hỗ trợ CineZore</strong></p>
                    </div>
                    <div class='footer'>
                        <p>© 2026 CineZore Cinema System. Bảo mật là ưu tiên hàng đầu của chúng tôi.</p>
                    </div>
                </div>
            </body>
            </html>";
            
            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}