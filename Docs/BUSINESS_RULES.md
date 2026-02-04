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

*Tài liệu được cập nhật tự động theo mã nguồn hiện tại.*
*Last updated: 2026-02-03*
