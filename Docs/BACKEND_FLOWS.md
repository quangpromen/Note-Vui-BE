# NoteVui Backend Flows

Tài liệu này mô tả các luồng xử lý chính của backend NoteVui, từ lúc API nhận request đến khi trả response. Nội dung bám theo mã nguồn hiện tại của `NoteVui.API`, `NoteVui.Application`, `NoteVui.Infrastructure` và `NoteVui.Domain`.

## 1. Tổng Quan Kiến Trúc

- `NoteVui.API`: chứa controllers, middleware pipeline, CORS, Swagger, JWT Bearer, rate limiting và đăng ký DI.
- `NoteVui.Application`: chứa DTOs, interfaces, services nghiệp vụ như Notes, Sync, Admin, SubscriptionRequest, Vip, UserProfile.
- `NoteVui.Infrastructure`: chứa EF Core `ApplicationDbContext`, Identity, CurrentUserService, OTP, Mail, Gemini AI.
- `NoteVui.Domain`: chứa entity và enum lõi như `Note`, `NoteContent`, `AppUser`, `UserSubscription`, `SubscriptionRequest`, `AiUsageLog`.

## 2. Middleware Và Security Pipeline

Pipeline chính trong `Program.cs`:

1. Swagger chỉ bật trong môi trường Development.
2. CORS dùng policy `AllowAll` ở Development và `ProductionLimit` ở môi trường khác.
3. `UseHttpsRedirection()`.
4. `UseRouting()`.
5. `UseRateLimiter()`.
6. `UseAuthentication()`.
7. `UseAuthorization()`.
8. `MapControllers()`.

Các lớp bảo vệ hiện có:

- JWT Bearer validation: validate issuer, audience, lifetime và signing key.
- OTP registration token có claim `purpose=registration` bị chặn khi dùng như access token.
- Rate limiting:
  - `AuthLimiter`: áp dụng cho `AuthController`, tối đa 5 requests/phút theo IP.
  - `ApiLimiter`: áp dụng cho Notes, Sync, UserProfile, dùng UserId nếu đã đăng nhập, fallback IP.
  - `GlobalLimiter`: token bucket toàn ứng dụng để giảm spam/DDoS thô.
- Account lockout:
  - User mới được áp dụng lockout.
  - Sai mật khẩu 5 lần sẽ bị khóa 15 phút.
  - Login dùng `lockoutOnFailure: true`.

Khi bị rate limit, API trả `429 Too Many Requests`, có header `Retry-After` và body `ProblemDetails`.

## 3. Authentication Và Registration Flow

### 3.1 Register Trực Tiếp

Endpoint: `POST /api/auth/register`

Luồng xử lý:

1. Client gửi `email`, `password`, `fullName`.
2. Backend kiểm tra email đã tồn tại chưa.
3. Nếu chưa tồn tại, tạo `AppUser` bằng ASP.NET Core Identity.
4. Nếu tạo thành công, backend sinh access token và refresh token.

Ghi chú: luồng OTP registration hiện cũng tồn tại và nên là luồng ưu tiên cho client production.

### 3.2 Register Bằng OTP 3 Bước

Endpoints:

- `POST /api/auth/register/send-otp`
- `POST /api/auth/register/verify-otp`
- `POST /api/auth/register/complete`

Luồng xử lý:

1. `send-otp`: backend chuẩn hóa email, nếu email đã tồn tại thì trả OK giả để hạn chế email enumeration. Nếu email hợp lệ, tạo OTP 6 số bằng RNG bảo mật, hash SHA-256 và lưu trong RAM.
2. `verify-otp`: backend kiểm tra OTP, thời hạn 5 phút và số lần nhập sai tối đa 5. Nếu đúng, backend cấp `registrationToken` JWT sống 10 phút với claim `purpose=registration`.
3. `complete`: client gửi `registrationToken`, password và fullName. Backend validate token, tạo user và trả auth response.

### 3.3 Login

Endpoint: `POST /api/auth/login`

Luồng xử lý:

1. Backend tìm user theo email.
2. Nếu không tìm thấy, trả thông báo generic.
3. Nếu tìm thấy, gọi `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.
4. Nếu sai mật khẩu, Identity tự tăng `AccessFailedCount`.
5. Khi sai đủ 5 lần, Identity set `LockoutEnd` trong 15 phút.
6. Nếu đúng mật khẩu và tài khoản không bị khóa, backend trả access token và refresh token mới.

### 3.4 Google Login

Endpoint: `POST /api/auth/google-login`

Luồng xử lý:

1. Backend validate Google ID token với `GoogleAuth:ClientId`.
2. Lấy email từ payload đã xác thực.
3. Chỉ cho login nếu tài khoản đã tồn tại trong hệ thống.
4. Nếu user bị lockout, backend từ chối.
5. Nếu hợp lệ, backend cập nhật profile thiếu thông tin và trả token.

### 3.5 Refresh Token

Endpoint: `POST /api/auth/refresh-token`

Luồng xử lý:

1. Client gửi access token cũ và refresh token.
2. Backend đọc claims từ access token đã hết hạn bằng `ValidateLifetime = false`.
3. Backend lấy userId từ token, kiểm tra refresh token có khớp DB và chưa hết hạn.
4. Nếu hợp lệ, sinh access token và refresh token mới.

### 3.6 Change Password Và Logout

Endpoints:

- `POST /api/auth/change-password`
- `POST /api/auth/logout`

Change password kiểm tra mật khẩu hiện tại, không cho đặt mật khẩu mới giống mật khẩu cũ, đổi mật khẩu bằng Identity, revoke refresh token và gửi email thông báo.

Logout xóa refresh token hiện tại trong user record.

## 4. Forgot Password Flow

Endpoints:

- `POST /api/auth/forgot-password/send-otp`
- `POST /api/auth/forgot-password/verify-otp`
- `POST /api/auth/forgot-password/reset`

Luồng xử lý:

1. `send-otp`: kiểm tra email tồn tại, kiểm tra rate limit OTP, tạo OTP và gửi email.
2. `verify-otp`: xác thực OTP, nếu đúng thì cấp `resetToken` JWT sống 10 phút với claim `purpose=forgot_password`.
3. `reset`: validate `resetToken`, sinh Identity reset token nội bộ, đổi mật khẩu và gửi email thông báo.

## 5. Notes Flow

Controller: `NotesController`

Endpoints:

- `GET /api/notes`
- `GET /api/notes/{id}`
- `POST /api/notes`
- `PUT /api/notes/{id}`
- `DELETE /api/notes/{id}`
- `PATCH /api/notes/{id}/restore`

Luồng xử lý:

1. Tất cả endpoint yêu cầu JWT.
2. `NoteService` lấy UserId từ `ICurrentUserService`.
3. Mọi truy vấn note đều lọc theo `UserId`, hạn chế IDOR.
4. Delete là soft delete bằng `IsDeleted`, `DeletedAt`.
5. Restore chỉ khôi phục note thuộc user hiện tại.

## 6. Sync Flow

Endpoint: `POST /api/sync`

Mục tiêu: đồng bộ offline-first giữa mobile local DB và server.

Luồng xử lý:

1. Client gửi `lastSyncTime` và danh sách `changes`.
2. Backend từ chối request nếu có `ClientId == Guid.Empty`.
3. Backend nhóm thay đổi theo `ClientId`, giữ bản có `UpdatedAt` mới nhất.
4. Backend lấy các note hiện có theo `UserId` và `ClientId`.
5. Với note mới hoặc note restore, backend kiểm tra quota.
6. Conflict dùng chiến lược Last Write Wins: `UpdatedAt` mới hơn sẽ thắng.
7. Backend trả các thay đổi trên server sau `lastSyncTime`, bao gồm note đã xóa để client xóa local.
8. Client lưu `serverTime` làm mốc sync tiếp theo.

Quota sync:

- Free user: tối đa 50 active notes.
- VIP user: không giới hạn.
- Nếu vượt quota, API trả `403 Forbidden`.

## 7. AI Flow

Controller: `AiController`

Endpoints:

- `POST /api/ai/summarize`
- `POST /api/ai/grammar`
- `POST /api/ai/translate`
- `POST /api/ai/ideas`
- `GET /api/ai/quota`

Luồng xử lý:

1. User phải đăng nhập.
2. Backend kiểm tra VIP bằng `IVipService`.
3. User không phải VIP bị chặn `403 Forbidden`.
4. VIP user được gọi Gemini API qua `GeminiAiService`.
5. Backend ghi log vào `AiUsageLogs` cho cả request thành công và thất bại.
6. API quota hiện trả unlimited cho VIP và 0 cho Free.

## 8. Subscription Flow

Controller: `SubscriptionController`

Endpoints user:

- `GET /api/subscription/status`
- `GET /api/subscription/is-vip`
- `GET /api/subscription/details`
- `POST /api/subscription/test-activate`
- `POST /api/subscription/requests`
- `GET /api/subscription/requests/my`
- `PUT /api/subscription/requests/{id}/cancel`

Luồng subscription request:

1. User gửi yêu cầu nâng cấp bằng `POST /api/subscription/requests`.
2. Backend không cho tạo thêm nếu đang có request `Pending`.
3. User có thể xem request của mình.
4. User chỉ có thể hủy request thuộc chính mình và còn `Pending`.
5. Admin approve/reject request qua Admin API.
6. Khi approve, backend tạo hoặc cập nhật `UserSubscription`.

Ghi chú bảo mật: `/api/subscription/test-activate` là endpoint dev/test. Không nên mở trong production nếu không có bảo vệ bổ sung.

## 9. Admin Flow

Controller: `AdminController`

Tất cả endpoint yêu cầu role `Admin`.

Endpoints:

- `GET /api/admin/stats`
- `GET /api/admin/users`
- `POST /api/admin/users/{id}/lock`
- `GET /api/admin/users/{id}/subscription`
- `PUT /api/admin/users/{id}/subscription`
- `GET /api/admin/users/{id}/detail`
- `PUT /api/admin/users/{id}/profile`
- `POST /api/admin/users`
- `GET /api/admin/subscription-requests`
- `POST /api/admin/subscription-requests/{id}/approve`
- `POST /api/admin/subscription-requests/{id}/reject`

Luồng xử lý chính:

1. JWT phải có role `Admin`.
2. Dashboard stats đọc dữ liệu tổng hợp bằng query read-only.
3. User management hỗ trợ search, pagination, lock/unlock, edit profile, tạo user.
4. Subscription management cho phép admin set plan trực tiếp hoặc duyệt request nâng cấp.
5. Admin lock dùng `LockoutEnd = DateTimeOffset.MaxValue`; unlock đặt `LockoutEnd = null`.

## 10. Common Error Handling

- `400 Bad Request`: dữ liệu không hợp lệ hoặc business rule không thỏa.
- `401 Unauthorized`: thiếu hoặc sai access token.
- `403 Forbidden`: không đủ quyền, không phải VIP hoặc vượt quota.
- `404 Not Found`: tài nguyên không tồn tại hoặc không thuộc user hiện tại.
- `429 Too Many Requests`: vượt rate limit.
- `500 Internal Server Error`: lỗi server không mong muốn.

## 11. Cấu Hình Cần Có

Các nhóm cấu hình quan trọng:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Key`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `JwtSettings:DurationInMinutes`
- `EmailSettings`
- `GoogleAuth:ClientId`
- `AiSettings:GeminiApiKey`
- `AiSettings:GeminiApiUrl`

Không nên lưu secret thật trong `appsettings.json`. Với development nên dùng User Secrets; production nên dùng biến môi trường hoặc secret manager.

Last updated: 2026-06-02
