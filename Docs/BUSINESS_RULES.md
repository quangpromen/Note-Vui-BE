# NoteVui Business Rules

Tài liệu này mô tả các quy tắc nghiệp vụ và bảo mật đang áp dụng trong backend NoteVui.

## 1. Authentication Và Account Security

### 1.1 Đăng ký tài khoản

Hệ thống hỗ trợ hai luồng đăng ký:

- `POST /api/auth/register`: đăng ký trực tiếp bằng email/password/fullName.
- OTP flow:
  - `POST /api/auth/register/send-otp`
  - `POST /api/auth/register/verify-otp`
  - `POST /api/auth/register/complete`

Luồng OTP là luồng khuyến nghị cho production vì email được xác thực trước khi tạo tài khoản.

### 1.2 OTP

Quy tắc OTP:

- OTP gồm 6 chữ số.
- OTP được tạo bằng `RandomNumberGenerator`.
- OTP không lưu plaintext, chỉ lưu SHA-256 hash trong RAM.
- OTP hết hạn sau 5 phút.
- Tối đa 5 lần verify sai cho mỗi OTP.
- Tối đa 5 lần gửi OTP cho mỗi email trong cửa sổ 10 phút.
- OTP được xóa sau khi verify thành công.

### 1.3 Account lockout

Quy tắc khóa tài khoản khi đăng nhập sai:

- Áp dụng cho user mới.
- Sai mật khẩu 5 lần sẽ khóa tài khoản.
- Thời gian khóa: 15 phút.
- Login dùng `lockoutOnFailure: true`, nên Identity tự tăng `AccessFailedCount`.

Mục tiêu: giảm brute-force password và credential stuffing.

### 1.4 Token

Access token:

- Dùng JWT Bearer.
- Validate issuer, audience, lifetime và signing key.
- Có claims UserId, Email, JTI, FullName và Role.

Refresh token:

- Lưu trên `AppUser`.
- Refresh token có hạn 7 ngày.
- Khi refresh thành công, backend cấp access token và refresh token mới.
- Khi đổi mật khẩu hoặc logout, refresh token bị revoke.

Registration token và forgot-password token:

- Là JWT ngắn hạn 10 phút.
- Có claim `purpose`.
- Registration token không được phép dùng để gọi API thường.

## 2. Rate Limiting

Hệ thống có 3 lớp rate limiting:

| Policy | Phạm vi | Giới hạn | Partition key |
| --- | --- | --- | --- |
| `AuthLimiter` | `AuthController` | 5 requests/phút | IP thật từ `X-Forwarded-For`, fallback `RemoteIpAddress` |
| `ApiLimiter` | Notes, Sync, UserProfile | token bucket 40 token, refill 10 token/10 giây | UserId nếu authenticated, fallback IP |
| `GlobalLimiter` | Toàn ứng dụng | token bucket 120 token, refill 20 token/10 giây | UserId hoặc IP |

Khi bị giới hạn:

- HTTP status: `429 Too Many Requests`.
- Header: `Retry-After`.
- Body: `ProblemDetails`.

Mục tiêu:

- Chống brute-force login.
- Chống spam OTP.
- Giảm request bất thường vào API sync/notes.
- Có lớp bảo vệ ngoài cùng trước DDoS thô.

## 3. Notes Và Sync

### 3.1 Quyền sở hữu dữ liệu

Mỗi note thuộc một `UserId`. Backend luôn lọc dữ liệu theo user hiện tại.

Quy tắc:

- User chỉ đọc được note của mình.
- User chỉ update/delete/restore note của mình.
- Delete là soft delete.
- Restore chỉ áp dụng cho note đã soft delete.

### 3.2 Sync offline-first

Sync dùng `ClientId` làm định danh chính giữa mobile và server.

Quy tắc:

- `ClientId` không được rỗng.
- Nếu request có nhiều thay đổi cùng `ClientId`, backend giữ bản có `UpdatedAt` mới nhất.
- Conflict dùng Last Write Wins.
- Pull response bao gồm note đã xóa để mobile đồng bộ trạng thái xóa.
- `serverTime` phải được mobile lưu làm `lastSyncTime` kế tiếp.

### 3.3 Giới hạn số lượng note

| Gói | Giới hạn cloud notes | Hành vi khi vượt |
| --- | --- | --- |
| Free | 50 active notes | Chặn insert/restore mới, trả `403 Forbidden` |
| VIP | Không giới hạn | Cho phép lưu thêm |

Update hoặc delete note cũ vẫn được phép nếu không làm tăng số note active.

## 4. AI Features

AI là tính năng chỉ dành cho VIP.

| Gói | Quyền dùng AI | Quota |
| --- | --- | --- |
| Free | Bị chặn | 0 |
| VIP | Được phép | Không giới hạn theo code hiện tại |

Endpoint AI:

- `POST /api/ai/summarize`
- `POST /api/ai/grammar`
- `POST /api/ai/translate`
- `POST /api/ai/ideas`
- `GET /api/ai/quota`

Quy tắc:

- User phải đăng nhập.
- Backend kiểm tra `UserSubscription`.
- VIP hợp lệ khi `Status == Active`, `PlanType != Free`, `EndDate > DateTime.UtcNow`.
- Mọi request AI được ghi vào `AiUsageLogs`.
- Nếu Gemini API lỗi, API trả thông báo generic cho client.

## 5. Subscription Và VIP

### 5.1 PlanType

| Giá trị | Tên |
| --- | --- |
| 0 | Free |
| 1 | PremiumMonthly |
| 2 | PremiumYearly |

### 5.2 SubscriptionStatus

| Giá trị | Tên | Ý nghĩa |
| --- | --- | --- |
| 0 | Active | Đang hoạt động |
| 1 | Cancelled | Đã hủy nhưng có thể còn hạn |
| 2 | Expired | Đã hết hạn |

### 5.3 VIP check

User là VIP khi:

- Có subscription record.
- `PlanType != Free`.
- `Status == Active`.
- `EndDate > DateTime.UtcNow`.

### 5.4 Subscription request

User gửi yêu cầu nâng cấp để Admin duyệt.

Quy tắc:

- User không được tạo request mới nếu đang có request `Pending`.
- Request phải là plan premium, không phải `Free`.
- User chỉ xem và hủy request của chính mình.
- Chỉ request `Pending` mới được hủy.
- Admin approve sẽ kích hoạt hoặc cập nhật subscription.
- Admin reject có thể kèm lý do.

### 5.5 Dev test subscription

Endpoint `POST /api/subscription/test-activate` tồn tại để test. Endpoint này không nên mở tự do trong production.

## 6. Admin Rules

Admin API yêu cầu role `Admin`.

Admin có quyền:

- Xem dashboard stats.
- Xem danh sách user.
- Khóa/mở khóa user.
- Xem chi tiết user.
- Sửa profile user, bao gồm email.
- Tạo user.
- Set subscription trực tiếp.
- Duyệt/từ chối subscription request.

Khóa user bằng Admin:

- Lock: set `LockoutEnd` rất xa trong tương lai.
- Unlock: xóa `LockoutEnd`.

## 7. Error Rules

| Status | Ý nghĩa |
| --- | --- |
| 400 | Input sai hoặc business rule không thỏa |
| 401 | Chưa đăng nhập hoặc token sai |
| 403 | Không đủ quyền, không phải VIP, hoặc vượt quota |
| 404 | Không tìm thấy tài nguyên |
| 429 | Vượt rate limit |
| 500 | Lỗi server |

## 8. Production Notes

- Không lưu secret thật trong `appsettings.json`.
- Không log access token hoặc refresh token.
- Nên bảo vệ hoặc tắt `/api/subscription/test-activate` ở production.
- Nên cấu hình CORS production bằng domain thật.
- Nên dùng HTTPS ở mọi môi trường public.

Last updated: 2026-06-02
