# ✅ DANH SÁCH KIỂM THỬ TÍNH NĂNG (CHECKLIST)

Hãy sử dụng file này để kiểm tra từng tính năng của dự án `ShopOnlineCore`. Đánh dấu `[x]` vào các ô trống nếu tính năng hoạt động tốt.

## 1. 🛒 CỬA HÀNG (Front-end)

### A. Trang chủ (Home)
- [x] **Hiển thị sản phẩm**: Danh sách sản phẩm tải lên đầy đủ, có ảnh, giá.
- [x] **Load More**: Nút "Xem thêm" hoặc scroll xuống hoạt động, tải thêm sản phẩm mới.
- [x] **Sản phẩm mới/Sale**: Badge "New" và "Sale" hiển thị đúng logic.

### B. Duyệt & Tìm kiếm Sản phẩm
- [x] **Phân trang**: Chuyển trang 1, 2, 3... mượt mà.
- [x] **Lọc danh mục**: Click vào danh mục (ví dụ: "Laptop") chỉ hiện sản phẩm thuộc danh mục đó.
- [x] **Tìm kiếm**: Gõ từ khóa vào ô search -> trả về kết quả đúng.
- [x] **Lọc theo giá**: Kéo thanh trượt giá hoặc nhập khoảng giá -> danh sách cập nhật đúng.
- [x] **Sắp xếp**:
    - [x] Giá tăng dần
    - [x] Giá giảm dần
    - [x] Ngẫu nhiên (Mặc định) - Thử F5 xem thứ tự có đổi không.

### C. Chi tiết & Giỏ hàng
- [x] **Chi tiết sản phẩm**: Hiển thị đầy đủ thông tin, ảnh lớn, sản phẩm liên quan.
- [x] **Thêm vào giỏ**: Click "Thêm vào giỏ" -> số lượng trên icon giỏ hàng tăng lên.
- [x] **Xem giỏ hàng**: Hiển thị đúng các món đã chọn.
- [x] **Cập nhật số lượng**: Tăng/giảm số lượng trong giỏ -> Tổng tiền cập nhật đúng.
- [x] **Xóa sản phẩm**: Xóa món hàng khỏi giỏ hoạt động.

### D. Đặt hàng (Checkout)
- [x] **Thông tin**: Tự động điền nếu đã đăng nhập.
- [x] **Validation**: Báo lỗi nếu thiếu tên, địa chỉ, sđt.
- [x] **Đặt hàng thành công**: Thông báo thành công, giỏ hàng được làm trống.
- [x] **Check tồn kho**: Thử đặt quá số lượng tồn kho -> Báo lỗi cụ thể (ví dụ: "Chỉ còn 5 sản phẩm").

## 2. 👤 TÀI KHOẢN (Account)

- [x] **Đăng ký**: Tạo tài khoản mới thành công.
- [x] **Đăng nhập**: Đăng nhập bằng tài khoản vừa tạo.
- [x] **Đăng nhập Google**: Click Login Google -> Chuyển hướng và đăng nhập được (hoặc báo lỗi nếu chưa cấu hình).
- [x] **Đăng xuất**: Thoát tài khoản tài khoản.

## 3. 🛠️ QUẢN TRỊ (Admin Area)
*Truy cập: `/Admin/Products` hoặc `/Admin/Orders` (cần user có Role Admin)*

### A. Quản lý Sản phẩm
- [x] **Xem danh sách**: Hiển thị bảng danh sách sản phẩm.
- [x] **Thêm mới**: Upload ảnh, nhập thông tin -> Lưu thành công.
- [x] **Sửa**: Thay đổi giá, tên, tồn kho -> Cập nhật đúng.
- [x] **Xóa**: Xóa sản phẩm khỏi danh sách.

### B. Quản lý Đơn hàng
- [x] **Xem danh sách**: Hiển thị các đơn hàng mới đặt.
- [x] **Chi tiết**: Xem được chi tiết ai mua, mua gì.
- [x] **Cập nhật trạng thái**: Chuyển trạng thái (Pending -> Shipped...).

### C. Hệ thống
- [x] **Quản lý User**: Xem danh sách người dùng.
- [x] **Phân quyền**: Gán quyền Admin cho user.

---
**Ghi chú:**
- Nếu gặp lỗi, hãy copy lỗi và báo lại cho AI để sửa.
- Một số tính năng như Thanh toán Online (MoMo) chưa hoàn thiện là bình thường.
