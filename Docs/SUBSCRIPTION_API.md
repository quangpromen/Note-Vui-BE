# NoteVui Subscription API

Base URL: `/api/subscription`

Tất cả endpoint yêu cầu JWT Bearer token.

```http
Authorization: Bearer {accessToken}
Content-Type: application/json
```

## 1. Tổng Quan

Subscription API xử lý:

- Kiểm tra trạng thái VIP.
- Lấy chi tiết subscription.
- Tạo yêu cầu nâng cấp để Admin duyệt.
- Xem và hủy yêu cầu nâng cấp của user.
- Kích hoạt subscription test cho development.

## 2. Enum

### PlanType

| Giá trị | Tên | Ý nghĩa |
| --- | --- | --- |
| 0 | Free | Gói miễn phí |
| 1 | PremiumMonthly | Premium theo tháng |
| 2 | PremiumYearly | Premium theo năm |

### SubscriptionStatus

| Giá trị | Tên | Ý nghĩa |
| --- | --- | --- |
| 0 | Active | Đang hoạt động |
| 1 | Cancelled | Đã hủy |
| 2 | Expired | Đã hết hạn |

### RequestStatus

| Tên | Ý nghĩa |
| --- | --- |
| Pending | Đang chờ Admin xử lý |
| Approved | Đã được duyệt |
| Rejected | Bị từ chối |
| Cancelled | User đã hủy |

## 3. Check Subscription Status

`GET /api/subscription/status`

Response `200`:

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

Field:

| Field | Type | Mô tả |
| --- | --- | --- |
| `isVip` | boolean | User có VIP hợp lệ không |
| `planType` | string | `Free`, `PremiumMonthly`, `PremiumYearly` |
| `status` | string? | `Active`, `Cancelled`, `Expired` hoặc null |
| `startDate` | datetime? | Ngày bắt đầu |
| `endDate` | datetime? | Ngày hết hạn |
| `daysRemaining` | int? | Số ngày còn lại |
| `isAutoRenew` | boolean | Có tự động gia hạn không |

## 4. Check VIP Nhanh

`GET /api/subscription/is-vip`

Response `200`:

```json
{
  "isVip": false
}
```

## 5. Get Subscription Details

`GET /api/subscription/details`

Response khi có subscription:

```json
{
  "hasSubscription": true,
  "subscription": {
    "id": 1,
    "userId": "user-id",
    "planType": 1,
    "status": 0,
    "startDate": "2026-06-01T00:00:00Z",
    "endDate": "2026-07-01T00:00:00Z",
    "isAutoRenew": false,
    "createdAt": "2026-06-01T00:00:00Z",
    "updatedAt": null
  }
}
```

Response khi chưa có subscription:

```json
{
  "hasSubscription": false,
  "message": "No subscription found. User is on Free plan."
}
```

## 6. Test Activate

`POST /api/subscription/test-activate?durationDays=30&planType=PremiumMonthly`

Mục đích: tạo hoặc cập nhật subscription test cho user hiện tại.

Query:

| Param | Type | Default | Mô tả |
| --- | --- | --- | --- |
| `durationDays` | int | 30 | Số ngày hiệu lực |
| `planType` | enum | PremiumMonthly | `Free`, `PremiumMonthly`, `PremiumYearly` |

Response:

```json
{
  "message": "Test subscription activated for 30 days",
  "planType": "PremiumMonthly",
  "expiresAt": "2026-07-02T00:00:00Z"
}
```

Không nên mở endpoint này trong production nếu chưa có bảo vệ bổ sung.

## 7. Create Upgrade Request

`POST /api/subscription/requests`

Request:

```json
{
  "planType": 1,
  "note": "Đã chuyển khoản, mã giao dịch ABC123"
}
```

Quy tắc:

- `planType` phải là `PremiumMonthly` hoặc `PremiumYearly`.
- User không được tạo request mới nếu đã có request `Pending`.

Response `200`:

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

## 8. Get My Requests

`GET /api/subscription/requests/my`

Response:

```json
[
  {
    "id": 10,
    "planType": "PremiumMonthly",
    "planName": "Premium (Tháng)",
    "status": "Pending",
    "note": "Đã chuyển khoản, mã giao dịch ABC123",
    "adminNote": null,
    "createdAt": "2026-06-02T00:00:00Z",
    "processedAt": null
  }
]
```

## 9. Cancel Request

`PUT /api/subscription/requests/{id}/cancel`

Quy tắc:

- Chỉ user sở hữu request được hủy.
- Chỉ hủy được request `Pending`.

Response:

```json
{
  "success": true,
  "message": "Đã hủy yêu cầu nâng cấp thành công."
}
```

## 10. Admin Processing

Admin xử lý request qua Admin API:

- `GET /api/admin/subscription-requests`
- `POST /api/admin/subscription-requests/{id}/approve`
- `POST /api/admin/subscription-requests/{id}/reject`

Khi approve, backend sẽ tạo hoặc cập nhật `UserSubscription`.

## 11. VIP Rules

User được xem là VIP khi:

- Có subscription record.
- `PlanType != Free`.
- `Status == Active`.
- `EndDate > DateTime.UtcNow`.

Last updated: 2026-06-02
