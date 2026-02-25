# 🛠 NoteVui Admin Portal - API Technical Specification

Tài liệu này cung cấp chi tiết kỹ thuật về các API dành cho hệ thống Admin của NoteVui. Tài liệu được thiết kế đặc biệt để các AI (như Claude, GPT) có thể đọc và tạo mã nguồn Front-end (React/Vite/Tailwind) một cách chính xác nhất.

---

## 1. Thông tin chung (General Information)

- **Base URL**: `http://localhost:5000` (Môi trường Development)
- **Content-Type**: `application/json`
- **Authentication**: JWT Bearer Token.
- **Role Required**: `Admin`

### Luồng xác thực (Auth Flow):
1. **Login**: Gửi thông tin tới `POST /api/auth/login`.
2. **Token**: Nhận về `accessToken` và lưu vào `localStorage`.
3. **Authorization**: Gắn Header `Authorization: Bearer {token}` vào mọi yêu cầu tới module Admin.
4. **Role Check**: Giải mã JWT (Sử dụng `jwt-decode`) để kiểm tra claim `role` có chứa `Admin` không trước khi cho vào Dashboard.

---

## 2. Chi tiết các Endpoint (Endpoint Deep Dive)

### 2.1. Thống kê tổng quan (Dashboard Stats)
Cung cấp các con số "Key Metrics" để hiển thị trên các thẻ (Cards) ở trang chủ Admin.

- **Endpoint**: `GET /api/admin/stats`
- **Visual Mapping**: Hiển thị thành 4 Stats Cards với icon khác nhau.
- **Success Response (200 OK)**:
```json
{
  "totalRevenue": 1500000.00,  // Decimal: Hiển thị với format tiền tệ (VND)
  "totalUsers": 120,           // Integer: Tổng user đăng ký
  "activePremiumUsers": 15,    // Integer: User đang có gói VIP
  "totalAiRequests": 450       // Integer: Tổng số lần gọi AI Note
}
```

### 2.2. Quản lý người dùng (User Management)
Danh sách người dùng hỗ trợ tìm kiếm và phân trang theo chuẩn Server-side.

- **Endpoint**: `GET /api/admin/users`
- **Query Parameters**:
  - `search`: `string` (Optional) - Tìm theo Email hoặc Tên.
  - `page`: `int` (Default: 1) - Số trang hiện tại.
  - `pageSize`: `int` (Default: 10) - Số bản ghi mỗi trang.
- **Visual Mapping**: Hiển thị dạng Table với Pagination và Search bar.
- **Success Response (200 OK)**:
```json
{
  "items": [
    {
      "id": "string (guid)",
      "email": "user@example.com",
      "fullName": "Nguyễn Văn A",
      "planName": "Free | Premium (Month) | Premium (Year)",
      "joinDate": "2024-02-01T10:00:00Z",
      "isLocked": false
    }
  ],
  "totalCount": 120,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 12
}
```

### 2.3. Hành động: Khóa/Mở khóa tài khoản
- **Endpoint**: `POST /api/admin/users/{id}/lock`
- **Visual Mapping**: Nút bấm (Switch hoặc Button) trong cột "Thao tác" của bảng User. Màu đỏ cho "Khóa", Màu xanh cho "Mở".
- **Request Body**:
```json
{
  "lock": true // true để khóa, false để mở khóa
}
```
- **Response**:
```json
{
  "success": true,
  "message": "Đã khóa tài khoản người dùng thành công."
}
```

### 2.4. Xem thông tin VIP của người dùng
- **Endpoint**: `GET /api/admin/users/{id}/subscription`
- **Visual Mapping**: Hiển thị trong Modal hoặc Panel chi tiết khi click vào user.
- **Success Response (200 OK)**:
```json
{
  "id": 1,
  "userId": "user-guid-string",
  "email": "user@example.com",
  "fullName": "Nguyễn Văn A",
  "planType": "Free | PremiumMonthly | PremiumYearly",
  "status": "Active | Cancelled | Expired",
  "startDate": "2024-02-01T10:00:00Z",
  "endDate": "2024-03-01T10:00:00Z",
  "isAutoRenew": false,
  "isActive": true
}
```
- **Error Response (404 Not Found)**:
```json
{
  "message": "Không tìm thấy người dùng."
}
```

### 2.5. Kích hoạt/Điều chỉnh VIP cho người dùng
- **Endpoint**: `PUT /api/admin/users/{id}/subscription`
- **Visual Mapping**: Form Modal với dropdown chọn Plan và Date picker cho ngày hết hạn.
- **Request Body**:
```json
{
  "planType": 1,           // 0: Free, 1: PremiumMonthly, 2: PremiumYearly
  "endDate": "2024-12-01T00:00:00Z",  // Optional: Ngày hết hạn tùy chỉnh
  "isAutoRenew": false     // Optional: Tự động gia hạn
}
```
- **Lưu ý về `endDate`**: Nếu không truyền, hệ thống sẽ tự tính:
  - `Free`: 100 năm
  - `PremiumMonthly`: 1 tháng từ hiện tại
  - `PremiumYearly`: 1 năm từ hiện tại
- **Email Notification**: Khi người dùng được cấp hoặc gia hạn gói Premium (VIP), hệ thống sẽ tự động gửi một email thông báo chuyên nghiệp (HTML, Tiếng Việt) tới địa chỉ email của người dùng.

- **Success Response (200 OK)**:
```json
{
  "success": true,
  "message": "Đã cập nhật gói Premium (Tháng) cho người dùng thành công.",
  "data": {
    "id": 1,
    "userId": "user-guid-string",
    "email": "user@example.com",
    "fullName": "Nguyễn Văn A",
    "planType": "PremiumMonthly",
    "status": "Active",
    "startDate": "2024-02-07T10:00:00Z",
    "endDate": "2024-03-07T10:00:00Z",
    "isAutoRenew": false,
    "isActive": true
  }
}
```
- **Error Response (400 Bad Request)**:
```json
{
  "message": "PlanType không hợp lệ. Giá trị hợp lệ: 0 (Free), 1 (PremiumMonthly), 2 (PremiumYearly)."
}
```

### 2.6. Xem chi tiết người dùng (User Detail)
Xem toàn bộ thông tin chi tiết của một user, bao gồm subscription, notes stats, AI usage, và trạng thái tài khoản.

- **Endpoint**: `GET /api/admin/users/{id}/detail`
- **Visual Mapping**: Trang chi tiết user hoặc Modal lớn khi click vào user trong bảng.
- **Success Response (200 OK)**:
```json
{
  "userId": "user-guid-string",
  "email": "user@example.com",
  "fullName": "Nguyễn Văn A",
  "avatarUrl": "https://image-url.com/avatar.jpg",
  "isLocked": false,
  "lockoutEnd": null,
  "subscription": {
    "subscriptionId": 1,
    "planName": "Premium (Tháng)",
    "planType": "PremiumMonthly",
    "planTypeValue": 1,
    "isVip": true,
    "status": "Active",
    "startDate": "2024-02-01T10:00:00Z",
    "endDate": "2024-03-01T10:00:00Z",
    "daysRemaining": 22,
    "isAutoRenew": false
  },
  "totalNotes": 50,
  "activeNotes": 42,
  "deletedNotes": 8,
  "pinnedNotes": 5,
  "aiUsage": {
    "usedToday": 3,
    "usedThisMonth": 28,
    "usedThisYear": 95,
    "totalUsed": 95
  }
}
```

### 2.7. Sửa thông tin người dùng (Edit User Profile)
Admin có thể sửa FullName, Email, AvatarUrl của user. Admin có quyền đổi email - quyền mà user thường không có.

- **Endpoint**: `PUT /api/admin/users/{id}/profile`
- **Visual Mapping**: Form Modal chỉnh sửa thông tin.
- **Request Body**:
```json
{
  "fullName": "Nguyễn Văn B",
  "email": "newemail@example.com",
  "avatarUrl": "https://image-url.com/avatar.jpg"
}
```
- **Request Validation**:
  - `fullName`: Required, max 100 ký tự
  - `email`: Required, phải đúng định dạng email
  - `avatarUrl`: Optional (nullable)

- **Success Response (200 OK)**:
```json
{
  "success": true,
  "message": "Đã cập nhật thông tin người dùng thành công.",
  "data": {
    "userId": "user-guid-string",
    "email": "newemail@example.com",
    "fullName": "Nguyễn Văn B",
    "avatarUrl": "https://image-url.com/avatar.jpg",
    "isLocked": false,
    "lockoutEnd": null,
    "subscription": { ... },
    "totalNotes": 50,
    "activeNotes": 42,
    "deletedNotes": 8,
    "pinnedNotes": 5,
    "aiUsage": { ... }
  }
}
```
- **Error Response (400 Bad Request)** - Email trùng:
```json
{
  "message": "Email đã được sử dụng bởi tài khoản khác."
}
```

### 2.8. Tạo người dùng mới (Create User)
Dùng để Admin chủ động tạo thêm User vào hệ thống.
Nếu Email đã tồn tại, hệ thống không tạo mới mà trả về thông tin User hiện tại (không báo lỗi).

- **Endpoint**: `POST /api/admin/users`
- **Visual Mapping**: Form Modal (Thêm mới) có các trường Email, Password, FullName, AvatarUrl (Tùy chọn).
- **Request Body**:
```json
{
  "email": "newuser@example.com",
  "password": "Password123!",
  "fullName": "Người Dùng Mới",
  "avatarUrl": "https://image-url.com/avatar.jpg"
}
```
- **Request Validation**:
  - `email`: Required, định dạng email.
  - `password`: Required, ít nhất 6 ký tự.
  - `fullName`: Required, max 100 ký tự.
  - `avatarUrl`: Optional (nullable).

- **Success Response (200 OK)**:
```json
{
  "success": true,
  "message": "Xử lý người dùng thành công.",
  "data": {
    "userId": "user-guid-string",
    "email": "newuser@example.com",
    "fullName": "Người Dùng Mới",
    "avatarUrl": "https://image-url.com/avatar.jpg",
    "isLocked": false,
    "lockoutEnd": null,
    "subscription": null,
    "totalNotes": 0,
    "activeNotes": 0,
    "deletedNotes": 0,
    "pinnedNotes": 0,
    "aiUsage": null
  }
}
```
- **Error Response (400 Bad Request)**: Lỗi Validation.

---

## 3. Cấu trúc Schema DTO (Data Models)

### AdminDashboardStatsDto
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `totalRevenue` | `decimal` | Tổng doanh thu hệ thống |
| `totalUsers` | `int` | Số lượng người dùng |
| `activePremiumUsers`| `int` | Người dùng trả phí đang hoạt động |
| `totalAiRequests` | `int` | Tổng lưu lượng AI |

### UserSummaryDto
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `id` | `string` | ID duy nhất của user |
| `email` | `string` | Địa chỉ email |
| `fullName` | `string` | Tên đầy đủ |
| `planName` | `string` | Các giá trị: `Free`, `Premium (Month)`, `Premium (Year)`, `(Expired)` |
| `joinDate` | `DateTime` | Hiện tại đang trả về giá trị mặc định của server (Sẽ cập nhật sau). |
| `isLocked` | `bool` | Xác định dựa trên `LockoutEnd` > hiện tại. |

### SetUserSubscriptionRequest
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `planType` | `int` | 0: Free, 1: PremiumMonthly, 2: PremiumYearly |
| `endDate` | `DateTime?` | (Optional) Ngày hết hạn tùy chỉnh |
| `isAutoRenew` | `bool` | (Optional, default: false) Tự động gia hạn |

### UserSubscriptionDto
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `id` | `int` | ID của subscription |
| `userId` | `string` | ID của user |
| `email` | `string` | Địa chỉ email |
| `fullName` | `string` | Tên đầy đủ |
| `planType` | `string` | Loại gói: `Free`, `PremiumMonthly`, `PremiumYearly` |
| `status` | `string` | Trạng thái: `Active`, `Cancelled`, `Expired` |
| `startDate` | `DateTime` | Ngày bắt đầu subscription |
| `endDate` | `DateTime` | Ngày hết hạn subscription |
| `isAutoRenew` | `bool` | Có tự động gia hạn không |
| `isActive` | `bool` | Subscription có đang hoạt động (Premium + Chưa hết hạn) |

### AdminUserDetailDto (MỚI)
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `userId` | `string` | ID duy nhất |
| `email` | `string` | Địa chỉ email |
| `fullName` | `string` | Tên đầy đủ |
| `avatarUrl` | `string?` | URL avatar (nullable) |
| `isLocked` | `bool` | Tài khoản đang bị khóa |
| `lockoutEnd` | `DateTimeOffset?` | Thời điểm hết khóa |
| `subscription` | `AdminUserSubscriptionInfo` | Thông tin gói (xem bảng dưới) |
| `totalNotes` | `int` | Tổng notes (gồm đã xóa) |
| `activeNotes` | `int` | Notes đang hoạt động |
| `deletedNotes` | `int` | Notes trong thùng rác |
| `pinnedNotes` | `int` | Notes đã ghim |
| `aiUsage` | `AdminAiUsageStats` | Thống kê AI usage |

### AdminUserSubscriptionInfo
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `subscriptionId` | `int?` | ID subscription (null nếu Free) |
| `planName` | `string` | "Free", "Premium (Tháng)", "Premium (Năm)" |
| `planType` | `string` | "Free", "PremiumMonthly", "PremiumYearly" |
| `planTypeValue` | `int` | 0, 1, 2 - dùng cho dropdown select |
| `isVip` | `bool` | Đang có gói premium hoạt động |
| `status` | `string?` | "Active", "Cancelled", "Expired" |
| `startDate` | `DateTime?` | Ngày bắt đầu |
| `endDate` | `DateTime?` | Ngày hết hạn |
| `daysRemaining` | `int?` | Số ngày còn lại |
| `isAutoRenew` | `bool` | Tự động gia hạn |

### AdminEditUserRequest (MỚI)
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `fullName` | `string` | Required, max 100 ký tự |
| `email` | `string` | Required, định dạng email |
| `avatarUrl` | `string?` | Optional (nullable) |

### AdminCreateUserRequest (MỚI)
| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `email` | `string` | Required, định dạng email |
| `password` | `string` | Required, tối thiểu 6 ký tự |
| `fullName` | `string` | Required, max 100 ký tự |
| `avatarUrl` | `string?` | Optional (nullable) |

---

## 4. Giao thức Kỹ thuật (Technical Nuances)

- **Pagination**: Mặc định là trang 1, 10 bản ghi. Tổng số bản ghi nằm trong `totalCount`.
- **Search**: Case-insensitive, tìm kiếm theo Email hoặc FullName.
- **Locking Indefinitely**: Khi khóa một user, hệ thống đặt `LockoutEnd` lên mức tối đa (`9999-12-31`).
- **Revenue Calculation**: Chỉ tính từ các giao dịch có trạng thái `Success`.
- **Email Change**: Chỉ Admin mới có quyền đổi email cho user. User thường chỉ đổi được FullName và AvatarUrl.

---

## 5. Gợi ý giao diện cho AI (UI/UX Specification)

Nếu bạn sử dụng AI để tạo Front-end, hãy cung cấp các yêu cầu sau:
1. **Framework**: React JS with Vite.
2. **Styling**: Tailwind CSS.
3. **Icons**: Lucide React.
4. **State Management**: React Query (TanStack Query) cho việc fetch dữ liệu Admin.
5. **Components**:
   - Sidebar: Chứa điều hướng (Dashboard, Users, Settings).
   - Topbar: Hiển thị Profile Admin và nút Logout.
   - Dashboard: Grid 4 cột cho Stats, kèm theo biểu đồ (nếu cần).
   - User List: Table với Search, Filter Plan, và nút Lock/Unlock táo bạo.
   - User Detail: Trang/Modal chi tiết với tabs (Info, Subscription, Stats).
   - Edit User: Form Modal cho Admin sửa thông tin user.

---

## 6. PROMPT MẪU ĐỂ GỬI CHO AI TẠO FE
*Copy đoạn dưới đây gửi cho AI để tạo giao diện Admin:*

> "Dựa trên tài liệu API đính kèm, hãy xây dựng một Dashboard Admin chuyên nghiệp bằng React JS, Tailwind CSS và Lucide Icons.
> Yêu cầu:
> 1. Dashboard chính có 4 thẻ thống kê: Doanh thu, Tổng User, User Premium, Lượt dùng AI.
> 2. Trang Quản lý User có bảng dữ liệu hỗ trợ phân trang và tìm kiếm.
> 3. Cột 'Trạng thái' trong bảng User hiển thị Badge cho Plan (Free/Premium).
> 4. Có nút 'Khóa/Mở khóa' người dùng bằng Dialog xác nhận.
> 5. Click vào row user mở trang/modal chi tiết hiển thị đầy đủ thông tin (notes stats, AI usage, subscription).
> 6. Có form edit thông tin user (FullName, Email, Avatar) cho Admin.
> 7. Sử dụng Axios để gọi API và quản lý state bằng React Query.
> 8. Code sạch, chia component rõ ràng (StatsCard, UserTable, UserDetail, Layout, Sidebar).
> 9. Tông màu chủ đạo: Slate & Indigo đậm chất SaaS."

---
*Cập nhật lần cuối: 25/02/2026*
