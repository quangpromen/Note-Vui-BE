# 🛠 Admin Portal API Documentation

Tài liệu này hướng dẫn cách sử dụng các API dành cho Admin để quản trị hệ thống NoteVui.

## 1. Xác thực & Phân quyền (Authentication & Authorization)

Tất cả các API trong module Admin đều yêu cầu:
- **Xác thực**: JWT Bearer Token hợp lệ.
- **Phân quyền**: User phải có Role `Admin` trong hệ thống (được lưu trong bảng `AspNetRoles` và `AspNetUserRoles`).

> **Lưu ý quan trọng**: Khi đăng nhập, Role `Admin` sẽ được nhúng trực tiếp vào Claims của JWT Token. Nếu bạn vừa được cấp quyền Admin, hãy đăng nhập lại để làm mới Token.

---

## 2. Các Endpoint API

### 2.1. Thống kê Dashboard (Overview Stats)

Lấy số liệu tổng quan về hiệu quả hoạt động của hệ thống.

- **URL**: `GET /api/admin/stats`
- **Auth**: `Bearer Token (Role Admin)`
- **Response**: `200 OK`
```json
{
  "totalRevenue": 1500000.00,
  "totalUsers": 120,
  "activePremiumUsers": 15,
  "totalAiRequests": 450
}
```

**Các trường dữ liệu:**
- `totalRevenue`: Tổng doanh thu từ các giao dịch thành công (Status = Success).
- `totalUsers`: Tổng số người dùng đã đăng ký.
- `activePremiumUsers`: Số lượng người dùng đang có gói VIP còn hạn và đang ở trạng thái `Active`.
- `totalAiRequests`: Tổng số lượt sử dụng AI từ trước đến nay.

---

### 2.2. Quản lý người dùng (User Management)

Lấy danh sách người dùng trong hệ thống với khả năng tìm kiếm và phân trang.

- **URL**: `GET /api/admin/users`
- **Params**:
  - `search` (string, optional): Tìm kiếm theo Email hoặc Họ tên.
  - `page` (int, default=1): Trang hiện tại.
  - `pageSize` (int, default=10): Số người dùng mỗi trang.
- **Response**: `200 OK`
```json
{
  "items": [
    {
      "id": "guid-user-id",
      "email": "admin@notevui.com",
      "fullName": "Administrator",
      "planName": "Premium (Year)",
      "joinDate": "2024-01-01T00:00:00Z",
      "isLocked": false
    }
  ],
  "totalCount": 120,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 12
}
```

---

### 2.3. Khóa/Mở khóa tài khoản (Lock/Unlock Account)

Sử dụng cơ chế **Identity Lockout** để chặn hoặc cho phép người dùng truy cập hệ thống.

- **URL**: `POST /api/admin/users/{id}/lock`
- **Body**:
```json
{
  "lock": true
}
```
- **Response**: `200 OK`
```json
{
  "success": true,
  "message": "Đã khóa tài khoản người dùng thành công."
}
```

**Cơ chế hoạt động:**
- **Khi Lock (true)**: Hệ thống đặt `LockoutEnd` thành `DateTimeOffset.MaxValue`. User sẽ bị chặn đăng nhập ngay lập tức.
- **Khi Unlock (false)**: Hệ thống đặt `LockoutEnd` thành `null`. User có thể đăng nhập lại bình thường.

---

## 3. Quy tắc Kỹ thuật (Technical Notes)

1. **Read-only DB**: Các API thống kê sử dụng `.AsNoTracking()` để tối ưu hiệu năng và đảm bảo không thay đổi dữ liệu Database ngoài ý muốn.
2. **Identity Integration**: Hệ thống tận dụng tối đa `UserManager<AppUser>` để xử lý các tác vụ quản lý người dùng, đảm bảo tính bảo mật và đúng chuẩn ASP.NET Core Identity.
3. **No Migration**: Module Admin được thiết kế dựa trên cấu trúc database hiện có, không yêu cầu thêm table hay property mới.

---
*Tài liệu cập nhật ngày: 05/02/2026*
