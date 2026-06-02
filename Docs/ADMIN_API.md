# NoteVui Admin API

Tài liệu API cho Admin Portal.

Base URL: `/api/admin`

Yêu cầu chung:

```http
Authorization: Bearer {adminAccessToken}
Content-Type: application/json
```

Tất cả endpoint trong `AdminController` yêu cầu role `Admin`.

## 1. Auth Flow Cho Admin

1. Admin gọi `POST /api/auth/login`.
2. Backend trả `accessToken`, `refreshToken`.
3. Frontend lưu token và gửi header `Authorization: Bearer {accessToken}`.
4. API `/api/admin/*` chỉ cho phép token có role `Admin`.

Lưu ý:

- Auth endpoints bị `AuthLimiter`: 5 requests/phút theo IP.
- Nếu đăng nhập sai 5 lần, account bị lock 15 phút.
- Các API khác vẫn chịu `GlobalLimiter`.

## 2. Dashboard Stats

`GET /api/admin/stats`

Response:

```json
{
  "totalRevenue": 1500000.0,
  "totalUsers": 120,
  "activePremiumUsers": 15,
  "totalAiRequests": 450
}
```

Field:

| Field | Type | Mô tả |
| --- | --- | --- |
| `totalRevenue` | decimal | Tổng doanh thu từ transaction success |
| `totalUsers` | int | Tổng user, không tính Admin |
| `activePremiumUsers` | int | User premium đang active và chưa hết hạn |
| `totalAiRequests` | int | Tổng request AI |

## 3. User Management

### 3.1 Get Users

`GET /api/admin/users?search=user&page=1&pageSize=10`

Query:

| Param | Type | Default | Mô tả |
| --- | --- | --- | --- |
| `search` | string? | null | Tìm theo email hoặc fullName |
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 10 | Tối đa 100 |

Response:

```json
{
  "items": [
    {
      "id": "user-id",
      "email": "user@example.com",
      "fullName": "Nguyen Van A",
      "planName": "Free",
      "joinDate": "2026-06-02T00:00:00Z",
      "isLocked": false
    }
  ],
  "totalCount": 1,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### 3.2 Lock Or Unlock User

`POST /api/admin/users/{id}/lock`

Request:

```json
{
  "lock": true
}
```

Response:

```json
{
  "success": true,
  "message": "Đã khóa tài khoản người dùng thành công."
}
```

Quy tắc:

- `lock = true`: đặt `LockoutEnd` rất xa trong tương lai.
- `lock = false`: xóa lockout.

### 3.3 Get User Detail

`GET /api/admin/users/{id}/detail`

Response:

```json
{
  "userId": "user-id",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "avatarUrl": null,
  "isLocked": false,
  "lockoutEnd": null,
  "subscription": {
    "subscriptionId": 1,
    "planName": "Premium (Tháng)",
    "planType": "PremiumMonthly",
    "planTypeValue": 1,
    "isVip": true,
    "status": "Active",
    "startDate": "2026-06-01T00:00:00Z",
    "endDate": "2026-07-01T00:00:00Z",
    "daysRemaining": 29,
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

### 3.4 Edit User Profile

`PUT /api/admin/users/{id}/profile`

Request:

```json
{
  "fullName": "Nguyen Van B",
  "email": "new-email@example.com",
  "avatarUrl": "https://example.com/avatar.png"
}
```

Validation:

- `fullName`: required, max 100 ký tự.
- `email`: required, email format.
- `avatarUrl`: optional.

Response:

```json
{
  "success": true,
  "message": "Đã cập nhật thông tin người dùng thành công.",
  "data": {
    "userId": "user-id",
    "email": "new-email@example.com",
    "fullName": "Nguyen Van B",
    "avatarUrl": "https://example.com/avatar.png",
    "isLocked": false,
    "lockoutEnd": null,
    "subscription": {},
    "totalNotes": 10,
    "activeNotes": 8,
    "deletedNotes": 2,
    "pinnedNotes": 1,
    "aiUsage": {}
  }
}
```

### 3.5 Create User

`POST /api/admin/users`

Request:

```json
{
  "email": "new-user@example.com",
  "password": "Password123!",
  "fullName": "New User",
  "avatarUrl": null
}
```

Quy tắc:

- Nếu email đã tồn tại, backend không tạo duplicate mà trả detail user hiện có.
- Password tối thiểu 6 ký tự theo DTO hiện tại.

Response:

```json
{
  "success": true,
  "message": "Xử lý người dùng thành công.",
  "data": {
    "userId": "new-user-id",
    "email": "new-user@example.com",
    "fullName": "New User",
    "avatarUrl": null,
    "isLocked": false,
    "lockoutEnd": null,
    "subscription": {},
    "totalNotes": 0,
    "activeNotes": 0,
    "deletedNotes": 0,
    "pinnedNotes": 0,
    "aiUsage": {}
  }
}
```

## 4. User Subscription Management

### 4.1 Get User Subscription

`GET /api/admin/users/{id}/subscription`

Response:

```json
{
  "id": 1,
  "userId": "user-id",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "planType": "PremiumMonthly",
  "status": "Active",
  "startDate": "2026-06-01T00:00:00Z",
  "endDate": "2026-07-01T00:00:00Z",
  "isAutoRenew": false,
  "isActive": true
}
```

### 4.2 Set User Subscription

`PUT /api/admin/users/{id}/subscription`

Request:

```json
{
  "planType": 1,
  "endDate": "2026-07-01T00:00:00Z",
  "isAutoRenew": false
}
```

PlanType:

| Value | Name |
| --- | --- |
| 0 | Free |
| 1 | PremiumMonthly |
| 2 | PremiumYearly |

Nếu `endDate` không truyền:

- Free: cộng 100 năm.
- PremiumMonthly: cộng 1 tháng.
- PremiumYearly: cộng 1 năm.

Response:

```json
{
  "success": true,
  "message": "Đã cập nhật gói Premium (Tháng) cho người dùng thành công.",
  "data": {
    "id": 1,
    "userId": "user-id",
    "email": "user@example.com",
    "fullName": "Nguyen Van A",
    "planType": "PremiumMonthly",
    "status": "Active",
    "startDate": "2026-06-02T00:00:00Z",
    "endDate": "2026-07-02T00:00:00Z",
    "isAutoRenew": false,
    "isActive": true
  }
}
```

## 5. Subscription Request Management

### 5.1 Get Requests

`GET /api/admin/subscription-requests?status=0&search=user&page=1&pageSize=10`

Query:

| Param | Type | Mô tả |
| --- | --- | --- |
| `status` | RequestStatus? | Optional filter |
| `search` | string? | Tìm theo email/fullName |
| `page` | int | Mặc định 1 |
| `pageSize` | int | Mặc định 10, tối đa 100 |

Response:

```json
{
  "items": [
    {
      "id": 10,
      "userId": "user-id",
      "userEmail": "user@example.com",
      "userFullName": "Nguyen Van A",
      "userAvatarUrl": null,
      "planType": "PremiumMonthly",
      "planName": "Premium (Tháng)",
      "status": "Pending",
      "note": "Đã chuyển khoản ABC123",
      "adminNote": null,
      "processedByUserName": null,
      "processedAt": null,
      "createdAt": "2026-06-02T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### 5.2 Approve Request

`POST /api/admin/subscription-requests/{id}/approve`

Response:

```json
{
  "success": true,
  "message": "Đã phê duyệt yêu cầu nâng cấp gói Premium (Tháng) cho Nguyen Van A thành công.",
  "data": {
    "id": 10,
    "userId": "user-id",
    "userEmail": "user@example.com",
    "userFullName": "Nguyen Van A",
    "planType": "PremiumMonthly",
    "planName": "Premium (Tháng)",
    "status": "Approved",
    "note": "Đã chuyển khoản ABC123",
    "adminNote": null,
    "processedByUserName": "Admin",
    "processedAt": "2026-06-02T00:10:00Z",
    "createdAt": "2026-06-02T00:00:00Z"
  }
}
```

Approve sẽ kích hoạt hoặc cập nhật subscription của user.

### 5.3 Reject Request

`POST /api/admin/subscription-requests/{id}/reject`

Request:

```json
{
  "reason": "Chưa nhận được thanh toán."
}
```

Response:

```json
{
  "success": true,
  "message": "Đã từ chối yêu cầu nâng cấp của Nguyen Van A.",
  "data": {
    "id": 10,
    "status": "Rejected",
    "adminNote": "Chưa nhận được thanh toán.",
    "processedAt": "2026-06-02T00:10:00Z"
  }
}
```

## 6. DTO Reference

### AdminDashboardStatsDto

| Field | Type |
| --- | --- |
| `totalRevenue` | decimal |
| `totalUsers` | int |
| `activePremiumUsers` | int |
| `totalAiRequests` | int |

### UserSummaryDto

| Field | Type |
| --- | --- |
| `id` | string |
| `email` | string |
| `fullName` | string |
| `planName` | string |
| `joinDate` | DateTime |
| `isLocked` | bool |

### SetUserSubscriptionRequest

| Field | Type |
| --- | --- |
| `planType` | int |
| `endDate` | DateTime? |
| `isAutoRenew` | bool |

### AdminEditUserRequest

| Field | Type |
| --- | --- |
| `fullName` | string |
| `email` | string |
| `avatarUrl` | string? |

### AdminCreateUserRequest

| Field | Type |
| --- | --- |
| `email` | string |
| `password` | string |
| `fullName` | string |
| `avatarUrl` | string? |

## 7. Error Codes

| Status | Ý nghĩa |
| --- | --- |
| 400 | Validation hoặc business rule lỗi |
| 401 | Thiếu/sai token |
| 403 | Không có role Admin |
| 404 | Không tìm thấy user/request |
| 429 | Vượt rate limit/global limiter |
| 500 | Lỗi server |

Last updated: 2026-06-02
