# 📋 Subscription API Documentation

> **Base URL**: `http://localhost:5100/api/subscription`
> 
> **Authentication**: Tất cả endpoints đều yêu cầu Bearer Token trong Header

---

## 📌 Tổng quan

Subscription API cung cấp các endpoints để quản lý và kiểm tra trạng thái VIP/Premium của user trong hệ thống NoteVui.

### Các Endpoints

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/status` | Lấy trạng thái subscription đầy đủ | ✅ Required |
| `GET` | `/is-vip` | Check nhanh user có VIP không | ✅ Required |
| `GET` | `/details` | Lấy chi tiết subscription record | ✅ Required |
| `POST` | `/test-activate` | [DEV] Tạo subscription test | ✅ Required |

---

## 📖 Chi tiết từng API

### 1️⃣ GET `/api/subscription/status`

**Mục đích**: Lấy trạng thái subscription đầy đủ của user hiện tại.

**Headers**:
```
Authorization: Bearer <your_jwt_token>
```

**Response Success (200)**:
```json
{
  "isVip": true,
  "planType": "PremiumMonthly",
  "status": "Active",
  "startDate": "2026-01-31T00:00:00Z",
  "endDate": "2026-03-02T00:00:00Z",
  "daysRemaining": 30,
  "isAutoRenew": false
}
```

**Response Fields**:
| Field | Type | Mô tả |
|-------|------|-------|
| `isVip` | boolean | User có đang là VIP hay không |
| `planType` | string | Loại gói: `Free`, `PremiumMonthly`, `PremiumYearly` |
| `status` | string | Trạng thái: `Active`, `Cancelled`, `Expired`, hoặc `null` |
| `startDate` | datetime | Ngày bắt đầu subscription |
| `endDate` | datetime | Ngày hết hạn subscription |
| `daysRemaining` | int | Số ngày còn lại (null nếu đã hết hạn) |
| `isAutoRenew` | boolean | Có tự động gia hạn không |

**Use Case**:
- Hiển thị thông tin tài khoản trên màn hình Profile/Settings
- Hiển thị badge VIP trên UI
- Hiển thị countdown "Còn X ngày"

---

### 2️⃣ GET `/api/subscription/is-vip`

**Mục đích**: Check nhanh user có phải VIP không (endpoint đơn giản nhất).

**Headers**:
```
Authorization: Bearer <your_jwt_token>
```

**Response Success (200)**:
```json
{
  "isVip": true
}
```

hoặc

```json
{
  "isVip": false
}
```

**Use Case**:
- Gate tính năng premium (AI không giới hạn, sync không giới hạn)
- Ẩn/hiện quảng cáo
- Quyết định có cho user truy cập tính năng hay show popup "Nâng cấp VIP"

---

### 3️⃣ GET `/api/subscription/details`

**Mục đích**: Lấy chi tiết đầy đủ của subscription record trong database.

**Headers**:
```
Authorization: Bearer <your_jwt_token>
```

**Response khi CÓ subscription (200)**:
```json
{
  "hasSubscription": true,
  "subscription": {
    "id": 1,
    "userId": "abc-123-def-456",
    "planType": 1,
    "status": 0,
    "startDate": "2026-01-31T00:00:00Z",
    "endDate": "2026-03-02T00:00:00Z",
    "isAutoRenew": false,
    "createdAt": "2026-01-31T00:00:00Z",
    "updatedAt": null
  }
}
```

**Response khi CHƯA CÓ subscription (200)**:
```json
{
  "hasSubscription": false,
  "message": "No subscription found. User is on Free plan."
}
```

**Enum Values**:
| PlanType | Value | Mô tả |
|----------|-------|-------|
| Free | 0 | Gói miễn phí |
| PremiumMonthly | 1 | Premium theo tháng |
| PremiumYearly | 2 | Premium theo năm |

| Status | Value | Mô tả |
|--------|-------|-------|
| Active | 0 | Đang hoạt động |
| Cancelled | 1 | Đã hủy |
| Expired | 2 | Đã hết hạn |

**Use Case**:
- Trang quản lý subscription
- Debug/Admin panel

---

### 4️⃣ POST `/api/subscription/test-activate`

> ⚠️ **CHỈ DÙNG CHO DEVELOPMENT/TESTING**

**Mục đích**: Tạo subscription giả để test các tính năng VIP mà không cần tích hợp payment gateway.

**Headers**:
```
Authorization: Bearer <your_jwt_token>
```

**Query Parameters**:
| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `durationDays` | int | 30 | Số ngày subscription |
| `planType` | enum | PremiumMonthly | `Free`, `PremiumMonthly`, `PremiumYearly` |

**Example Request**:
```
POST /api/subscription/test-activate?durationDays=7&planType=PremiumYearly
```

**Response Success (200)**:
```json
{
  "message": "Test subscription activated for 7 days",
  "planType": "PremiumYearly",
  "expiresAt": "2026-02-07T00:42:00Z"
}
```

**Behavior**:
- Nếu user chưa có subscription → Tạo mới
- Nếu user đã có subscription → Update lại (reset ngày)

---

## 🔄 Flow sử dụng thực tế

### Flow 1: Kiểm tra VIP khi mở app

```
┌─────────────────────────────────────────────────────────────────┐
│                        USER MỞ APP                               │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              GET /api/subscription/is-vip                        │
│              Headers: Authorization: Bearer <token>              │
└─────────────────────────────┬───────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              │                               │
              ▼                               ▼
┌─────────────────────────┐     ┌─────────────────────────────────┐
│    Response:            │     │    Response:                     │
│    { "isVip": false }   │     │    { "isVip": true }             │
└───────────┬─────────────┘     └───────────────┬─────────────────┘
            │                                   │
            ▼                                   ▼
┌─────────────────────────┐     ┌─────────────────────────────────┐
│  ⚠️ GIỚI HẠN FEATURES   │     │  ✅ FULL FEATURES                │
│  - AI: 5 lần/ngày       │     │  - AI: Không giới hạn            │
│  - Sync: 50 notes       │     │  - Sync: Không giới hạn          │
│  - Hiện quảng cáo       │     │  - Ẩn quảng cáo                  │
│  - Show "Nâng cấp VIP"  │     │  - Hiển thị badge 👑             │
└─────────────────────────┘     └─────────────────────────────────┘
```

---

### Flow 2: Hiển thị thông tin tài khoản

```
┌─────────────────────────────────────────────────────────────────┐
│               USER MỞ MÀN HÌNH PROFILE / SETTINGS                │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              GET /api/subscription/status                        │
│              Headers: Authorization: Bearer <token>              │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Response:                                                       │
│  {                                                               │
│    "isVip": true,                                                │
│    "planType": "PremiumMonthly",                                 │
│    "status": "Active",                                           │
│    "endDate": "2026-03-02T00:00:00Z",                            │
│    "daysRemaining": 30,                                          │
│    "isAutoRenew": false                                          │
│  }                                                               │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      UI HIỂN THỊ                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  👤 Tài khoản của bạn                                   │    │
│  │  ─────────────────────────────────────────────────────  │    │
│  │  👑 Gói: Premium Monthly                                │    │
│  │  📅 Hết hạn: 02/03/2026                                 │    │
│  │  ⏳ Còn lại: 30 ngày                                    │    │
│  │  🔄 Tự động gia hạn: Tắt                                │    │
│  │                                                         │    │
│  │  [Gia hạn ngay]  [Quản lý thanh toán]                   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 3: Gate tính năng AI

```
┌─────────────────────────────────────────────────────────────────┐
│              USER NHẤN NÚT "TẠO NOTE BẰNG AI"                    │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              GET /api/subscription/is-vip                        │
└─────────────────────────────┬───────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              │                               │
              ▼                               ▼
     { "isVip": false }              { "isVip": true }
              │                               │
              ▼                               ▼
┌─────────────────────────┐     ┌─────────────────────────────────┐
│  Kiểm tra quota hôm nay │     │  Cho phép dùng AI               │
│  (5 lần/ngày cho Free)  │     │  không giới hạn                 │
│                         │     │                                  │
│  Nếu hết quota:         │     │  → Gọi API AI                    │
│  → Show popup           │     │  → Tạo note                      │
│    "Nâng cấp VIP để     │     │                                  │
│     dùng không giới hạn"│     │                                  │
└─────────────────────────┘     └─────────────────────────────────┘
```

---

### Flow 4: Developer Testing

```
┌─────────────────────────────────────────────────────────────────┐
│              DEVELOPER MUỐN TEST TÍNH NĂNG VIP                   │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. Login để lấy token                                           │
│     POST /api/auth/login                                         │
│     Body: { "email": "test@notevui.com", "password": "Test@123" }│
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Check status hiện tại (chưa có subscription)                 │
│     GET /api/subscription/status                                 │
│     → Response: { "isVip": false, "planType": "Free", ... }      │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. Kích hoạt test subscription                                  │
│     POST /api/subscription/test-activate?durationDays=7          │
│     → Response: { "message": "Test subscription activated..." }  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  4. Check status lại                                             │
│     GET /api/subscription/status                                 │
│     → Response: { "isVip": true, "planType": "PremiumMonthly" }  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. Test các tính năng VIP                                       │
│     - AI không giới hạn ✅                                       │
│     - Sync không giới hạn ✅                                     │
│     - Badge VIP hiển thị ✅                                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Flutter Integration Example

### Dart Service

```dart
class SubscriptionService {
  final String baseUrl = 'http://localhost:5100/api/subscription';
  
  Future<bool> isVip(String token) async {
    final response = await http.get(
      Uri.parse('$baseUrl/is-vip'),
      headers: {'Authorization': 'Bearer $token'},
    );
    
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return data['isVip'] ?? false;
    }
    return false;
  }
  
  Future<SubscriptionStatus> getStatus(String token) async {
    final response = await http.get(
      Uri.parse('$baseUrl/status'),
      headers: {'Authorization': 'Bearer $token'},
    );
    
    if (response.statusCode == 200) {
      return SubscriptionStatus.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to get subscription status');
  }
}
```

### Gate Feature Example

```dart
void onAiButtonPressed() async {
  final isVip = await subscriptionService.isVip(authToken);
  
  if (isVip) {
    // VIP user - allow unlimited AI usage
    await callAiService();
  } else {
    // Free user - check daily quota
    if (todayAiUsage >= 5) {
      showUpgradeDialog();
    } else {
      await callAiService();
    }
  }
}
```

---

## 📝 Notes

- Tất cả datetime đều ở format **UTC** (`DateTime.UtcNow`)
- `test-activate` endpoint nên được **disable** hoặc **protect** trong production
- Logic `IsVip` check 3 điều kiện:
  1. `UserId` match
  2. `Status == Active`
  3. `EndDate > DateTime.UtcNow`

---

*Last updated: 2026-01-31*
