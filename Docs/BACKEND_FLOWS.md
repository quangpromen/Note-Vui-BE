# 🌊 Luồng Hoạt Động Cốt Lõi (Core Backend Flows)

Tài liệu này mô tả chi tiết các luồng xử lý (flows) chính bên trong hệ thống Backend (BE) của dự án NoteVui. Nó giúp các nhà phát triển và ban quản trị hiểu rõ bức tranh luồng dữ liệu, thao tác logic từ lúc tiếp nhận request đến khi trả về response.

---

## 1. 🔐 Luồng Đăng ký & Xác thực (Authentication & Registration Flow)
Luồng này đảm bảo an toàn tối đa cho hệ thống bằng cách xác thực danh tính người dùng thật qua Email, chống spam, giả mạo và các cuộc tấn công Brute-force.

**A. Quy trình Đăng ký 3 bước an toàn (OTP Flow)**
1. **Gửi Request (Step 1):** Client gọi API Gửi OTP cùng Email. BE kiểm tra _Rate Limit_ (không quá 5 yêu cầu/10 phút). Nếu Email đã tồn tại, BE giữ im lặng (trả về OK giả nhưng không gửi mail) để chống dò tìm (Email Enumeration). Nếu Email hợp lệ, hệ thống tạo OTP 6 số, băm `SHA-256`, lưu vào RAM (`ConcurrentDictionary`) và gửi qua dịch vụ `MailKit`.
2. **Xác thực OTP (Step 2):** Client gửi mã OTP người dùng nhập. BE kiểm tra thời hạn (5 phút), chặn nhập sai quá lần cho phép (tối đa 5 lần). Nếu đúng, BE sinh ra một token tạm chứa Claim `purpose=registration` có thời hạn 10 phút, tự động hủy bỏ OTP trong RAM để dọn rác, trả token này về Client.
3. **Hoàn tất Đăng ký (Step 3):** Client gửi Token đăng ký + Mật khẩu + Họ tên. BE kiểm tra tính hợp lệ của Token, nếu mọi thứ hợp lệ (cùng thuật toán HMAC, đúng mục đích, còn hạn), tài khoản người dùng (`AppUser`) sẽ chính thức được khởi tạo trong Database (`SQL Server`). 

**B. Quy trình Cấp đổi Token (Refresh Token Flow)**
- Khi Access Token hế hạn (thường là 1-2 tiếng), Client kích hoạt cơ chế cấp lại bằng cách gửi cặp `AccessToken` (có thể đã hết hạn) và `RefreshToken` còn hạn nạp vào DB.
- BE kiểm tra cặp khóa này trùng khớp hoàn toàn trong Database. Nếu thỏa mãn, BE sinh lại Access Token + Refresh Token mới giúp người dùng không bị văng khỏi hệ thống. Vòng quay này lặp lại liên tục cho đến khi Refresh Token quá hạn (Ví dụ: sau 7 ngày không vào app).

**C. Quy trình Quên Mật Khẩu (Forgot Password Flow)**
- **Gửi Request (Step 1):** Client gọi API Gửi OTP cùng Email. BE kiểm tra và ẩn thông báo lỗi nếu email không tồn tại (chống Email Enumeration attack). Tạo và gửi OTP 6 số qua MailKit nếu hợp lệ. Giới hạn tần suất gọi.
- **Xác thực OTP (Step 2):** Client nhập Email + OTP. BE xác minh vòng đời OTP và giới hạn nhập sai. Nếu trùng khớp, cấp đoạn mã `ResetToken` (JWT sống 10 phút mang claim `forgot_password`) đồng thời xóa OTP RAM. Server chạy Stateless, không lưu state trong DB.
- **Thay đổi Mật khẩu (Step 3):** Client gửi `ResetToken` cùng Mật khẩu mới. BE giải mã token kiểm tra Claim và lấy Email. Tiến hành đổi mật khẩu mới trong DB. Tự động bắn một Job chạy nền (Fire-and-forget) báo qua Email: "Đổi mật khẩu thành công ✅" cho User.

---

## 2. 🔄 Luồng Đồng bộ hóa Dữ liệu (Cloud Sync Flow)
Đây là "trái tim" của hệ thống NoteVui, giúp dữ liệu ghi chú giữa Mobile App (Local SQLite) và Server (SQL) luôn được hợp nhất, kể cả khi dùng app ngoại tuyến.

1. **Client khởi xướng (Push/Pull):** Mobile App gửi API `/api/sync` chứa _Thời gian đồng bộ cuối cùng_ (`lastSyncTime`) và danh sách các thay đổi (Insert/Update/Delete) mà người dùng thực hiện khi **Offline**.
2. **Tiếp nhận & Xử lý Tập trung (Server Side):** 
   - BE đối chiếu danh sách thay đổi của người dùng với cơ sở dữ liệu.
   - Các thay đổi trùng lặp sẽ bị loại bỏ, ưu tiên bản ghi có `UpdatedAt` mới nhất (Cơ chế chống Race-condition/Bất đồng bộ).
   - Server cập nhật hoặc thêm mới (`Upsert`) các bản ghi chưa được đồng bộ từ Client.
3. **Kiểm tra giới hạn (Quota Check):** Nếu tài khoản ở gói cấu hình `Free`, BE đo lường dung lượng hiện có. Nếu tổng ghi chú chuẩn bị đẩy lên vượt quá `50 Notes`, BE sẽ _Rollback_ giao dịch, trả thông báo `403 Forbidden` bắt nâng cấp VIP.
4. **Trả dữ liệu (Response Cycle):** BE dò tìm Database để tra các ghi chú đã được chỉnh sửa trên nền tảng khác mốc `lastSyncTime` của Client và gửi xuống. Đánh dấu `serverTime` mới kết thúc chu kỳ.

---

## 3. 🤖 Luồng Xử Lý Trí Tuệ Nhân Tạo (AI Features Flow)
Thực thi các tính năng đặc thù (Tóm tắt, Dịch thuật, Sửa lỗi văn phạm) và tính cước.

1. **Gateway Check:** Client gửi nội dung dạng thô cần AI xử lý. BE truy xuất JWT xem Client hiện tại đang là UID nào. 
2. **Validation & Quota (Chốt chặn User):** BE gọi `IVipService` truy vết bảng `UserSubscription`. Trạng thái có đang là `Active` không? `EndDate` lớn hơn giờ hiện tại không?
   - Nếu `False` ❌ → Block API ngay lập tức (`403 Forbidden`).
   - Nếu `True` ✅ → Thông quan.
3. **Tương tác API bên thứ ba (LLM Call):** Nội dung đính kèm Prompt gốc được đóng gói gửi qua HTTP Client ra ngoài `Google Gemini API`.
4. **Log & Tính cước (Audit Log):** Sau khi Response trả về, BE tự động tính đếm số lượng Token đầu vào (Prompt) và đầu ra (Completion), lưu lại _Action Type_ (vd: Summarize, Translate) vào bảng `DailyAiUsages` để quản lý tần suất và chống lạm dụng hệ thống từ tài khoản VIP.

---

## 4. 💎 Luồng Nâng cấp & Quản lý Hội viên (Subscription Lifecycle)
1. **Khởi tạo:** User thực hiện thanh toán. Ở môi trường Dev, Client trực tiếp gọi `/api/subscription/test-activate`. Ở môi trường Prod, đây là luồng _Webhook_ nhận về từ Cổng Thanh Toán (VNPAY/Stripe...).
2. **Kích hoạt (Activation):** 
   - Xác nhận hóa đơn thành công. 
   - BE đẩy record vào bảng `PaymentTransactions`. 
   - Tại bảng phụ `UserSubscription`, cập nhật thông tin `PlanType` gán lên `PremiumMonthly/Yearly`, `StartDate` thành ngày hiện tại, cấp hạn cho `EndDate` + 30 ngày / + 365 ngày tương ứng, `Status = Active`.
3. **Lão hóa & Chặn đặc quyền (Grace Period & Expired):**
   - Hệ thống đánh giá dựa trên Realtime, không tốn resource chạy CronJob liên tục. Sự chuyển trạng thái từ `Active` xuống `Expired` tự động diễn ra theo logic thời gian (`EndDate < Now`).
   - Nếu người dùng hủy đăng ký tự động (Auto-Renew Off), trạng thái chuyển sang `Cancelled` nhưng *vẫn được phép tận hưởng quyền lợi* cho đến khi chạm mốc `EndDate`.

---

## 5. 🛠 Luồng Quản trị (Admin Management Flow)
Dành cho người có Role `Admin` truy cập từ Dashboard `admin.notevui.com`.
1. **Kiểm duyệt Role:** Token mang `Role=Admin` là vé thông hành duy nhất vào `/api/admin/*`.
2. **Truy vấn Thống kê:** Flow này lấy dữ liệu dạng Read-Only (thống kê tổng doanh thu hàng tháng theo transaction thành công, tổng new users). Được tối ưu Query bằng `AsNoTracking()`.
3. **Hành động Khẩn (Mutating Actions):** Khi Admin thực hiện thay pin (Block Account), khóa mật khẩu. Database cập nhật cờ `LockoutEnd` vô thời hạn cho User tương ứng. Quét sạch Refresh Token hiện sống để ép văng phiên làm việc ngay lập tức.
