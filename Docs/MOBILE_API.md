# NoteVui Mobile API

Tài liệu API cho mobile client. Base path mặc định: `/api`.

## 1. Quy Ước Chung

Base URL:

- Production: `https://api.notevui.com/api`
- Android emulator development: `http://10.0.2.2:5000/api`
- Localhost development: `http://localhost:5000/api`

Headers:

```http
Content-Type: application/json
Authorization: Bearer {accessToken}
```

Các endpoint được bảo vệ yêu cầu `Authorization`. Auth endpoints công khai trừ `change-password`, `profile`, `logout`.

## 2. Rate Limit Và Lockout

### Rate limit

Auth endpoints dùng `AuthLimiter`: tối đa 5 requests/phút theo IP.

Notes, Sync và UserProfile dùng `ApiLimiter`: token bucket theo UserId nếu đã đăng nhập, fallback IP nếu chưa đăng nhập.

Khi vượt giới hạn:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/problem+json
```

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Tần suất gửi yêu cầu quá nhanh. Vui lòng thử lại sau.",
  "instance": "/api/auth/login"
}
```

### Account lockout

Nếu login sai mật khẩu 5 lần, tài khoản bị khóa 15 phút. Client nên hiển thị thông báo generic, không nên spam retry vì request login vẫn bị rate limit.

## 3. Auth API

### 3.1 Register trực tiếp

`POST /api/auth/register`

Request:

```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "fullName": "Nguyen Van A"
}
```

Response `200`:

```json
{
  "accessToken": "jwt",
  "refreshToken": "refresh-token",
  "userId": "user-id",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "avatarUrl": null
}
```

### 3.2 Login

`POST /api/auth/login`

Request:

```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

Response `200`: giống `Register`.

Response thường gặp:

- `401`: email/password sai hoặc account bị khóa.
- `429`: vượt rate limit.

### 3.3 Google Login

`POST /api/auth/google-login`

Request:

```json
{
  "idToken": "google-id-token"
}
```

Response `200`: auth response.

Ghi chú: Google account phải đã có user trong hệ thống.

### 3.4 Refresh Token

`POST /api/auth/refresh-token`

Request:

```json
{
  "accessToken": "expired-access-token",
  "refreshToken": "current-refresh-token"
}
```

Response `200`: auth response mới.

### 3.5 Change Password

`POST /api/auth/change-password`

Auth required.

Request:

```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmNewPassword": "NewPassword123!"
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Đổi mật khẩu thành công. Vui lòng đăng nhập lại với mật khẩu mới."
}
```

### 3.6 Update Profile qua Auth

`PUT /api/auth/profile`

Auth required.

Request:

```json
{
  "fullName": "Nguyen Van B",
  "avatarUrl": "https://example.com/avatar.png"
}
```

### 3.7 Logout

`POST /api/auth/logout`

Auth required. Backend revoke refresh token.

## 4. Register OTP Flow

### 4.1 Send Registration OTP

`POST /api/auth/register/send-otp`

Request:

```json
{
  "email": "user@example.com"
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (bao gồm thư rác)."
}
```

### 4.2 Verify Registration OTP

`POST /api/auth/register/verify-otp`

Request:

```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Xác nhận OTP thành công.",
  "registrationToken": "jwt-registration-token"
}
```

### 4.3 Complete Registration

`POST /api/auth/register/complete`

Request:

```json
{
  "registrationToken": "jwt-registration-token",
  "password": "Password123!",
  "fullName": "Nguyen Van A"
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Đăng ký tài khoản thành công!",
  "data": {
    "accessToken": "jwt",
    "refreshToken": "refresh-token",
    "userId": "user-id",
    "email": "user@example.com",
    "fullName": "Nguyen Van A",
    "avatarUrl": null
  }
}
```

## 5. Forgot Password Flow

### 5.1 Send OTP

`POST /api/auth/forgot-password/send-otp`

Request:

```json
{
  "email": "user@example.com"
}
```

### 5.2 Verify OTP

`POST /api/auth/forgot-password/verify-otp`

Request:

```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Xác nhận OTP thành công.",
  "resetToken": "jwt-reset-token"
}
```

### 5.3 Reset Password

`POST /api/auth/forgot-password/reset`

Request:

```json
{
  "resetToken": "jwt-reset-token",
  "newPassword": "NewPassword123!"
}
```

## 6. Notes API

Tất cả endpoint yêu cầu auth và dùng `ApiLimiter`.

### 6.1 List Notes

`GET /api/notes?search=meeting&pageIndex=1&pageSize=10`

Response:

```json
{
  "items": [
    {
      "noteId": 1,
      "clientId": "550e8400-e29b-41d4-a716-446655440000",
      "userId": "user-id",
      "title": "Meeting",
      "shortPreview": "Summary",
      "fullContent": "Full text",
      "isPinned": false,
      "isDeleted": false,
      "createdAt": "2026-06-02T00:00:00Z",
      "updatedAt": "2026-06-02T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### 6.2 Get By Id

`GET /api/notes/{id}`

### 6.3 Create

`POST /api/notes`

Request:

```json
{
  "title": "New Note",
  "shortPreview": "Preview",
  "fullContent": "Full content",
  "isPinned": false
}
```

### 6.4 Update

`PUT /api/notes/{id}`

Request: giống create.

### 6.5 Delete

`DELETE /api/notes/{id}`

Soft delete, trả `204 No Content` nếu thành công.

### 6.6 Restore

`PATCH /api/notes/{id}/restore`

## 7. Sync API

`POST /api/sync`

Auth required, dùng `ApiLimiter`.

Request:

```json
{
  "lastSyncTime": "2026-06-02T00:00:00Z",
  "changes": [
    {
      "clientId": "550e8400-e29b-41d4-a716-446655440000",
      "noteId": null,
      "title": "Offline note",
      "shortPreview": "Preview",
      "fullContent": "Full content",
      "isPinned": false,
      "isDeleted": false,
      "createdAt": "2026-06-02T00:00:00Z",
      "updatedAt": "2026-06-02T00:10:00Z"
    }
  ]
}
```

Response:

```json
{
  "upserts": [],
  "serverTime": "2026-06-02T00:11:00Z",
  "stats": {
    "clientChangesReceived": 1,
    "inserted": 1,
    "updated": 0,
    "conflicts": 0,
    "serverChangesReturned": 0
  }
}
```

Client phải lưu `serverTime` làm `lastSyncTime` cho lần sync kế tiếp.

## 8. AI API

Auth required. Chỉ VIP được dùng AI.

Endpoints:

- `POST /api/ai/summarize`
- `POST /api/ai/grammar`
- `POST /api/ai/translate`
- `POST /api/ai/ideas`
- `GET /api/ai/quota`

Request AI chung:

```json
{
  "content": "Text to process",
  "targetLanguage": "vi",
  "noteId": "550e8400-e29b-41d4-a716-446655440000"
}
```

Response:

```json
{
  "result": "Processed text",
  "isSuccess": true,
  "errorMessage": null,
  "inputTokens": 100,
  "outputTokens": 50,
  "remainingQuota": 2147483646
}
```

Free user sẽ nhận `403 Forbidden`.

## 9. Subscription API

Auth required.

### 9.1 Status

`GET /api/subscription/status`

```json
{
  "isVip": true,
  "planType": "PremiumMonthly",
  "status": "Active",
  "startDate": "2026-06-01T00:00:00Z",
  "endDate": "2026-07-01T00:00:00Z",
  "daysRemaining": 29,
  "isAutoRenew": false
}
```

### 9.2 Is VIP

`GET /api/subscription/is-vip`

```json
{
  "isVip": true
}
```

### 9.3 Details

`GET /api/subscription/details`

### 9.4 Create Upgrade Request

`POST /api/subscription/requests`

Request:

```json
{
  "planType": 1,
  "note": "Đã chuyển khoản, mã giao dịch ABC123"
}
```

Response:

```json
{
  "success": true,
  "message": "Yêu cầu nâng cấp đã được gửi thành công. Vui lòng chờ Admin phê duyệt.",
  "data": {
    "id": 10,
    "planType": "PremiumMonthly",
    "planName": "Premium (Tháng)",
    "status": "Pending",
    "note": "Đã chuyển khoản, mã giao dịch ABC123",
    "adminNote": null,
    "createdAt": "2026-06-02T00:00:00Z",
    "processedAt": null
  }
}
```

### 9.5 My Requests

`GET /api/subscription/requests/my`

### 9.6 Cancel Request

`PUT /api/subscription/requests/{id}/cancel`

Chỉ hủy được request thuộc user hiện tại và còn `Pending`.

### 9.7 Test Activate

`POST /api/subscription/test-activate?durationDays=30&planType=PremiumMonthly`

Endpoint này chỉ nên dùng cho development/testing.

## 10. User Profile API

### 10.1 Get Profile

`GET /api/user/profile`

Auth required, dùng `ApiLimiter`.

Response:

```json
{
  "userId": "user-id",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "avatarUrl": null,
  "subscription": {
    "planName": "Free",
    "planType": "Free",
    "isVip": false,
    "status": null,
    "startDate": null,
    "endDate": null,
    "daysRemaining": null,
    "isAutoRenew": false
  },
  "totalNotesBackedUp": 10,
  "activeNotes": 8,
  "aiUsage": {
    "usedToday": 0,
    "usedThisMonth": 0,
    "usedThisYear": 0,
    "totalUsed": 0,
    "todayByAction": []
  }
}
```

### 10.2 Update Profile

`PUT /api/user/profile`

Request:

```json
{
  "fullName": "Nguyen Van B",
  "avatarUrl": "https://example.com/avatar.png"
}
```

## 11. Common Error Codes

| Status | Ý nghĩa |
| --- | --- |
| 400 | Validation hoặc business rule lỗi |
| 401 | Token thiếu/sai/hết hạn |
| 403 | Không đủ quyền, không phải VIP hoặc vượt quota |
| 404 | Không tìm thấy tài nguyên |
| 429 | Vượt rate limit |
| 500 | Lỗi server |

Last updated: 2026-06-02
