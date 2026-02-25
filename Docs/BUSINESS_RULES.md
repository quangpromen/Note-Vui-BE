# 🏢 NoteVui Business Rules

Tài liệu này mô tả các quy tắc nghiệp vụ (Business Rules) hiện đang vận hành trong hệ thống Backend.

## 1. 🤖 Tính năng AI (AI Features)

> **Quy tắc cốt lõi (Core Rule):** Chỉ thành viên VIP mới có quyền sử dụng các tính năng AI.

| Gói Thành Viên | Quyền Truy Cập | Giới hạn (Quota) | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Free User** | ❌ **Bị khóa** | 0 requests/ngày | API trả về lỗi `403 Forbidden` nếu cố tình truy cập. |
| **VIP User** | ✅ **Được phép** | **Không giới hạn** | Được sử dụng mọi tính năng (Tóm tắt, Dịch, Grammar, Ideas). |

### Chi tiết kỹ thuật:
- **API Endpoint**: `/api/ai/*`
- **Cơ chế kiểm soát**: 
  - Hệ thống kiểm tra `UserSubscription` có trạng thái `Active` và còn hạn sử dụng (`EndDate > Now`).
  - Nếu không thỏa mãn, request bị chặn ngay lập tức.
  - API `/get-quota` trả về `dailyLimit: 0` đối với user Free.

---

## 2. 🔄 Đồng bộ dữ liệu (Sync & Storage)

> **Quy tắc cốt lõi (Core Rule):** User Free bị giới hạn số lượng ghi chú (Notes) lưu trữ trên Cloud.

| Gói Thành Viên | Giới hạn lưu trữ (Max Notes) | Hành vi khi vượt quá giới hạn |
| :--- | :--- | :--- |
| **Free User** | **50 Notes** | - Không thể tạo thêm Note mới (Insert bị chặn).<br>- Vẫn có thể chỉnh sửa (Update) hoặc xóa (Delete) Note cũ.<br>- Vẫn có thể tải về (Pull) dữ liệu. |
| **VIP User** | **Không giới hạn** | Lưu trữ thoải mái. |

### Chi tiết kỹ thuật:
- **API Endpoint**: `/api/sync`
- **Cơ chế kiểm soát**: 
  - Khi Client gửi request `PUSH` (đẩy dữ liệu lên), hệ thống đếm tổng số Notes hiện tại trong DB.
  - **Cơ chế chống trùng lặp**: Nếu request chứa nhiều thay đổi cho cùng một `ClientId` (do lỗi mobile/retry), hệ thống sẽ gộp nhóm và chỉ xử lý bản ghi có `UpdatedAt` mới nhất.
  - Nếu `(Tổng hiện tại + Số Note mới) > 50` VÀ User là Free -> Chặn Insert/Restore, trả về lỗi `403 Forbidden`.
  - Message bắt buộc: *"Bạn đã đạt giới hạn 50 ghi chú. Vui lòng nâng cấp VIP để lưu trữ thêm."*

---

## 3. 👑 Hệ thống Hội viên (Membership System)

### 3.1. Các trạng thái (Status)
- **Active (0)**: Đang hoạt động, hưởng đầy đủ quyền lợi VIP.
- **Cancelled (1)**: Đã hủy gia hạn nhưng vẫn còn trong thời gian hiệu lực. Vẫn được hưởng quyền lợi VIP cho đến ngày kết thúc (`EndDate`).
- **Expired (2)**: Đã hết hạn. Mất quyền lợi VIP, trở về trạng thái Free User.

### 3.2. Quy tắc kích hoạt
- **Development**: Có thể dùng API `/api/subscription/test-activate` để tự kích hoạt gói mà không cần thanh toán (chỉ hiệu lực trên môi trường Dev).
- **Production**: Phải thông qua quy trình thanh toán (Payment Gateway) để cập nhật trạng thái đơn hàng -> Kích hoạt Subscription.

### 3.3. Xử lý hết hạn
- Hệ thống **tự động chốt quyền** dựa trên `EndDate` và `Status`. Không cần chạy Job quét định kỳ để khóa tài khoản, việc kiểm tra được thực hiện realtime mỗi khi gọi API.

---

## 4. 🛠 Quản trị hệ thống (Admin Management)

> **Quy tắc cốt lõi (Core Rule):** Chỉ người dùng có Role `Admin` mới có quyền truy cập module quản trị.

### 4.1. Quyền hạn của Admin
- Xem báo cáo doanh thu và chỉ số tăng trưởng người dùng.
- Tra cứu danh sách người dùng và trạng thái gói cước (VIP/Free).
- **Khóa tài khoản (Lockout)**: Có quyền khóa hoặc mở khóa bất kỳ tài khoản nào nếu vi phạm quy định.

### 4.2. Cơ chế bảo mật
- Mọi yêu cầu Admin đều được kiểm tra Role thông qua JWT Token.
- Role `Admin` được cấu hình trực tiếp trong Database (bảng Roles).

---

## 5. 🛡️ Xác thực & Bảo mật (Security & Authentication)

> **Quy tắc cốt lõi (Core Rule):** Hệ thống áp dụng tiêu chuẩn bảo mật mức cao (Enterprise-Grade) cho toàn bộ quy trình định danh người dùng.

### 5.1. Luồng đăng ký 3 bước bằng OTP Email
Hệ thống không cho phép tạo tài khoản trực tiếp. Thay vào đó, quy trình đăng ký bắt buộc phải thông qua xác minh email với mã OTP 6 số:
1. Gửi OTP đến email.
2. Xác minh OTP để cấp một Token cấp phép đặc biệt (`registrationToken`).
3. Dùng token đó để hoàn tất việc đăng ký tạo người dùng.

### 5.2. Các chốt chặn bảo mật (Backend Security Highlight)
Để đáp ứng chuẩn dự án thực tế, Backend đã được áp dụng các biện pháp bảo vệ sâu:
- **Chống Email Enumeration**: API Gửi OTP sẽ không bao giờ báo lỗi "Email đã tồn tại", kẻ xấu không thể lợi dụng API này để dò quét tệp email khách hàng.
- **Hash OTP trên RAM**: OTP không được lưu dưới dạng Plaintext, mà được băm bằng thuật toán `SHA-256` trước khi đưa vào Ram (Sử dụng `ConcurrentDictionary`).
- **Chống Timing Attack**: So sánh OTP hash bằng thuật toán thời gian hằng số `CryptographicOperations.FixedTimeEquals()` thay vì so sánh chuỗi thông thường.
- **Rate-Limiting & Brute-Force Protection**: 
  - Chỉ cho phép gửi tối đa 5 mã OTP / 10 phút. 
  - Chỉ cho phép nhập sai tối đa 5 lần trước khi mã OTP bị hủy hoàn toàn.
- **Phân lập Token (Token Isolation)**: Token được cấp ở bước verify OTP KHÔNG THỂ đem dùng làm Access Token để chọc vào các API khác của hệ thống, Backend sẽ chủ động từ chối.
- **Tự dọn dẹp bộ nhớ (Garbage Collection)**: Timer chạy ngầm dọn các OTP đã quá hạn 5 phút mỗi 15 phút để chống tràn RAM.

---

*Tài liệu được cập nhật tự động theo mã nguồn hiện tại.*
*Last updated: 2026-02-26*
