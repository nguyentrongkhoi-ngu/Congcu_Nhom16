<div align="center">

# 🎬 CinemaBooking
### *Hệ thống đặt vé xem phim hiện đại*

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-blue?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red?style=for-the-badge&logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.0-purple?style=for-the-badge&logo=bootstrap)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

*Trải nghiệm đặt vé xem phim trực tuyến tuyệt vời với giao diện hiện đại và tính năng đầy đủ*

</div>

---

## 🌟 Giới thiệu

**CinemaBooking** là một ứng dụng web quản lý rạp chiếu phim và đặt vé trực tuyến được phát triển bằng **ASP.NET Core MVC**. Với giao diện hiện đại, trực quan và tính năng đầy đủ, hệ thống mang đến trải nghiệm tuyệt vời cho cả người dùng và quản trị viên.

### ✨ Điểm nổi bật
- 🎨 **Giao diện hiện đại** với thiết kế responsive
- 🎫 **Đặt vé trực tuyến** với sơ đồ ghế tương tác
- 💳 **Thanh toán đa dạng** (MoMo, VNPay, tại rạp)
- 📱 **Responsive design** hoạt động mượt mà trên mọi thiết bị
- 🔐 **Bảo mật cao** với ASP.NET Core Identity
- ⚡ **Hiệu suất tối ưu** với Entity Framework Core

## 🚀 Tính năng chính

<table>
<tr>
<td width="50%">

### 👥 Dành cho người dùng
- 🎬 **Xem phim** - Danh sách phim đang chiếu và sắp chiếu
- 📋 **Chi tiết phim** - Thông tin, lịch chiếu, đánh giá
- 🎫 **Đặt vé** - Chọn lịch chiếu và ghế ngồi tương tác
- 💳 **Thanh toán** - MoMo, VNPay hoặc tại rạp
- 📜 **Lịch sử** - Xem lịch sử đặt vé
- ⭐ **Đánh giá** - Đánh giá phim sau khi xem
- 🔐 **Tài khoản** - Đăng ký, đăng nhập (Google)
- 📧 **Bảo mật** - Quên mật khẩu và OTP qua email

</td>
<td width="50%">

### 👨‍💼 Dành cho quản trị viên
- 🎭 **Quản lý phim** - Thêm, sửa, xóa phim
- 🏢 **Quản lý rạp** - Rạp phim và phòng chiếu
- 📅 **Lịch chiếu** - Quản lý lịch chiếu phim
- 🎟️ **Đặt vé** - Quản lý đặt vé và thanh toán
- 📱 **Kiểm tra vé** - Quét mã QR
- 👤 **Người dùng** - Quản lý và phân quyền
- 🎁 **Khuyến mãi** - Tạo và quản lý khuyến mãi
- 📊 **Báo cáo** - Thống kê và lịch sử giao dịch

</td>
</tr>
</table>

## 🛠️ Công nghệ sử dụng

<div align="center">

| Loại | Công nghệ | Phiên bản |
|------|-----------|-----------|
| 🖥️ **Backend** | ASP.NET Core MVC | .NET 9.0 |
| 🗄️ **Database** | SQL Server + Entity Framework Core | Code First |
| 🔐 **Authentication** | ASP.NET Core Identity + Google Auth | - |
| 💳 **Payment** | MoMo + VNPay | Sandbox |
| 📧 **Email** | SMTP | Gmail |
| 🎨 **Frontend** | HTML5 + CSS3 + JavaScript + Bootstrap | 5.0 |
| 📱 **QR Code** | QR Code Generation | - |
| 📁 **File Upload** | File Management System | - |

</div>

## 📁 Cấu trúc dự án

```
CinemaBooking/
├── 🎮 Controllers/          # Logic xử lý của ứng dụng
├── 📊 Models/               # Entity classes và models
├── 🎨 Views/                # Giao diện người dùng
├── 🗄️ Data/                 # ApplicationDbContext
├── 🔄 Migrations/           # Database migrations
├── ⚙️ Services/             # EmailService, MomoService, OtpService
├── 🌐 wwwroot/              # File tĩnh (CSS, JS, images)
└── 📁 uploads/              # File upload (poster, trailer)
```

## 🚀 Cài đặt và chạy dự án

### 📋 Yêu cầu hệ thống
- ✅ .NET 9.0 SDK
- ✅ SQL Server
- ✅ Visual Studio 2022 hoặc Visual Studio Code

### 🔧 Các bước cài đặt

<details>
<summary><b>📥 Bước 1: Clone repository</b></summary>

```bash
git clone <repository-url>
cd CinemaBooking
```
</details>

<details>
<summary><b>⚙️ Bước 2: Cấu hình database</b></summary>

Cấu hình chuỗi kết nối trong file `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=cinema_booking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
</details>

<details>
<summary><b>🗄️ Bước 3: Tạo database</b></summary>

```bash
dotnet ef database update
```
</details>

<details>
<summary><b>▶️ Bước 4: Chạy ứng dụng</b></summary>

```bash
dotnet run
```
</details>

<details>
<summary><b>🌐 Bước 5: Truy cập ứng dụng</b></summary>

Mở trình duyệt và truy cập: `https://localhost:7065`
</details>

## 💳 Cấu hình thanh toán

<table>
<tr>
<td width="50%">

### 🟣 MoMo
```json
{
  "Momo": {
    "PartnerCode": "YOUR_PARTNER_CODE",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "Endpoint": "https://test-payment.momo.vn/v2/gateway/api/create"
  }
}
```

</td>
<td width="50%">

### 🔵 VNPay
```json
{
  "VnPay": {
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "Command": "pay",
    "CurrCode": "VND",
    "Version": "2.1.0",
    "Locale": "vn"
  }
}
```

</td>
</tr>
</table>

## 📧 Cấu hình Email

```json
{
  "EmailSettings": {
    "Email": "YOUR_EMAIL",
    "Password": "YOUR_APP_PASSWORD",
    "SmtpServer": "smtp.gmail.com",
    "Port": 587
  }
}
```

## 🔑 Tài khoản mặc định

<div align="center">

| Vai trò | Username | Password | Mô tả |
|---------|----------|----------|-------|
| 👨‍💼 **Admin** | `admin` | `Admin@123` | Quản trị viên hệ thống |
| 👤 **User** | `user` | `User@123` | Người dùng thông thường |

</div>

---

## 🤝 Đóng góp

<div align="center">

Chúng tôi luôn chào đón mọi đóng góp từ cộng đồng!

[![Contribute](https://img.shields.io/badge/Contribute-Welcome-brightgreen?style=for-the-badge)](CONTRIBUTING.md)
[![Issues](https://img.shields.io/badge/Issues-Report%20Bug-red?style=for-the-badge)](../../issues)
[![Pull Requests](https://img.shields.io/badge/Pull%20Requests-Welcome-blue?style=for-the-badge)](../../pulls)

</div>

### 📝 Cách đóng góp
1. 🍴 Fork dự án
2. 🌿 Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. 💾 Commit thay đổi (`git commit -m 'Add some AmazingFeature'`)
4. 📤 Push lên branch (`git push origin feature/AmazingFeature`)
5. 🔄 Tạo Pull Request

---

## 📄 Giấy phép

<div align="center">

[![MIT License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

Dự án này được phân phối dưới **Giấy phép MIT**. Xem file [LICENSE](LICENSE) để biết thêm chi tiết.

</div>

---

<div align="center">

### 🌟 Nếu dự án hữu ích, hãy cho chúng tôi một ⭐!

**Được phát triển với ❤️ bởi CinemaBooking Team**

</div>
