<div align="center">
  <h1>🌟 NoteVui Backend API</h1>
  <p>
    <strong>Hệ thống API Backend mạnh mẽ, kiến trúc sạch (Clean Architecture) dành cho dự án NoteVui</strong>
  </p>
  <p>
    <img src="https://img.shields.io/badge/.NETCore-8.0-blue.svg" alt=".NET 8">
    <img src="https://img.shields.io/badge/EFCore-8.0-blueviolet.svg" alt="EF Core">
    <img src="https://img.shields.io/badge/CleanArchitecture-Solid-green.svg" alt="Clean Architecture">
    <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License">
    <img src="https://img.shields.io/badge/Swagger-API_Docs-85ea2d.svg" alt="Swagger">
  </p>
</div>

<hr />

## 📖 Giới thiệu (Overview)

**NoteVui Backend** là hệ thống API (Application Programming Interface) phát triển bằng **.NET 8** dành cho ứng dụng ghi chú **NoteVui**. Dự án được xây dựng dựa trên nguyên lý thiết kế **Clean Architecture**, giúp source code dễ dàng bảo trì, mở rộng và kiểm thử (testing-friendly).

Hệ thống hỗ trợ nhiều tính năng nâng cao như xác thực qua JWT, quản lý gói cước (VIP / Subscriptions), đồng bộ hóa dữ liệu (Sync), gửi Email, tích hợp AI (Google Gemini) để cung cấp các tính năng hỗ trợ người dùng thông minh.

---

## ✨ Tính năng nổi bật (Features)

🔒 **Định danh & Xác thực (Identity & Authentication)**
* Đăng ký, Đăng nhập và xác thực sử dụng JWT (JSON Web Tokens).
* Hỗ trợ xác thực hai yếu tố (2FA) thông qua mã OTP.
* Quản lý phân quyền người dùng (User & Admin Roles) qua ASP.NET Core Identity.
* Quản lý mật khẩu (Đổi mật khẩu, quên mật khẩu với xác thực qua Email).

📝 **Quản lý Ghi chú (Note Management)**
* CRUD cho Ghi chú.
* Đồng bộ hóa dữ liệu ghi chú (Sync Service).
* Tích hợp AI (Google Gemini) hỗ trợ soạn thảo, tóm tắt và cải thiện ghi chú.

👑 **Quản lý Gói cước (Subscriptions & VIP)**
* Đăng ký nâng cấp tài khoản VIP.
* Quản lý trạng thái và xét duyệt các yêu cầu đăng ký VIP (Admin).
* Notification & Email Workflow khi duyệt/từ chối yêu cầu.

🛡️ **Admin Panel & Dashboard**
* Dành riêng cho quyền Admin quản lý hệ thống.
* Xét duyệt yêu cầu nâng cấp gói cước.

---

## 🛠 Công nghệ sử dụng (Tech Stack)

* **Framework:** `.NET 8.0`
* **Kiến trúc:** `Clean Architecture`
* **Cơ sở dữ liệu:** `SQL Server`, ORM qua `Entity Framework Core 8`.
* **Xác thực:** `ASP.NET Core Identity`, `JWTBearer`.
* **API Documentation:** `Swagger / OpenAPI`.
* **Services tích hợp:**
  * Thư viện gửi Email (MailService).
  * Tích hợp Google Gemini (GeminiAiService).

---

## 📁 Kiến trúc Source Code (Architecture)

Dự án áp dụng chặt chẽ **Clean Architecture**, chia thành 4 lớp (layers) chính, tuân thủ nguyên lý Dependency Inversion:

```text
NoteVui_Backend/
├── 📂 NoteVui.Domain         # Domain Layer (Core): Chứa các Entities, Enums, Interfaces cốt lõi. Không phụ thuộc vào bất kỳ layer nào.
├── 📂 NoteVui.Application    # Application Layer: Chứa Use Cases, DTOs, Business Logic, Application Interfaces (Services). Chỉ phụ thuộc vào Domain.
├── 📂 NoteVui.Infrastructure # Infrastructure Layer: Triển khai EF Core DbContext, Email Provider, AI Service, JWT/Identity logic,... Phụ thuộc vào Application.
└── 📂 NoteVui.API            # Presentation Layer: Chứa Controller, cấu hình Middleware, Swagger, DI Setup (Program.cs). Phụ thuộc vào Application & Infrastructure.
```

---

## 🚀 Hướng dẫn cài đặt (Getting Started)

### Yêu cầu hệ thống (Prerequisites)
1. [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (hoặc Docker chạy SQL Server image).
3. Visual Studio 2022 / JetBrains Rider / VS Code.

### Cài đặt chạy dự án nội bộ (Local Setup)

**1. Clone Repo**
```bash
git clone <repository_url>
cd NoteVui_Backend
```

**2. Cấu hình Connection String và Settings**

Vào thư mục `NoteVui.API`, mở file `appsettings.json` (hoặc `appsettings.Development.json`) và thiết lập các thông số:

* `ConnectionStrings:DefaultConnection`: Trỏ tới instance SQL Server của bạn.
* `JwtSettings`: Secret Key, Issuer, Audience cho Token.
* `EmailSettings`: Cấu hình máy chủ SMTP (Gmail, SendGrid...).
* API Key cho Gemini (nếu cấu hình AI).

*(Vui lòng không commit file `appsettings.json` có chứa key thật lên Git)*

**3. Khởi tạo Cơ sở dữ liệu (Migrations)**

Chạy Entity Framework Migrations để tạo cấu trúc bảng trong Database:
```bash
# Di chuyển vào thư mục API
cd NoteVui.API

# Chạy lệnh cập nhật database
dotnet ef database update --project ../NoteVui.Infrastructure --startup-project .
```
*(Lưu ý: Source code trong Program.cs đã được thiết lập `context.Database.MigrateAsync()`, hệ thống cũng có thể tự động chạy Migration khi khởi động server).*

**4. Chạy dự án**
```bash
dotnet run --project NoteVui.API
```

Sau khi chạy thành công, mở trình duyệt truy cập tài liệu API: 
👉 **Swagger UI:** `https://localhost:<port>/swagger` (Port thường là 5001 hoặc 7xxx tuỳ cấu hình của bạn).

---

## 🔐 Cấu hình bảo mật (CORS & Auth)

Trong file `Program.cs`, dự án đã được cấu hình hai môi trường khác nhau cho CORS:

1. **Development (`AllowAll`)**: Chấp nhận API calls từ tất cả các URLs.
2. **Production (`ProductionLimit`)**: Chỉ chấp nhận Origin được chỉ định rõ để đảm bảo an toàn truy cập gốc (ví dụ: `https://admin.notevui.com`).


---

## 🤝 Đóng góp (Contributing)

Vì dự án đi theo hướng **Clean Architecture**, vui lòng tuân thủ chặt chẽ việc tham chiếu chéo giữa các tầng.
* Các logic nghiệp vụ lớn **không** được xử lý ở `Controller` mà phải được xử lý tại thư mục `Services` trong lớp `NoteVui.Application`.
* Khi thêm service mới, hãy tạo interface tại tầng `Application`, và implement chi tiết tại tầng `Infrastructure` (hoặc `Application` nếu là logic thuần tuý). Khai báo DI trong `Program.cs`.

---

<div align="center">
  <p>Phát triển bởi <b>Quangpromen</b> ❤️</p>
</div>
