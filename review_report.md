# Báo cáo Đánh giá Dự án ShopOnlineCore

## 1. Tổng quan
- **Loại dự án**: Web Application (E-commerce)
- **Công nghệ**: ASP.NET Core 9.0 (MVC + Razor Pages)
- **Database**: SQL Server (Entity Framework Core 8.0)
- **Authentication**: ASP.NET Core Identity (Cookie + Google)
- **Frontend**: Bootstrap, jQuery, Razor Views

## 2. Kiến trúc & Cấu trúc (Architecture)
- **Mô hình MVC**: Dự án tuân thủ tốt mô hình MVC.
- **Phân chia Areas**: Sử dụng `Areas/Admin` để tách biệt trang quản trị là rất tốt.
- **Service Layer**: Có thư mục `Services` nhưng chưa được tận dụng triệt để. Logic nghiệp vụ vẫn nằm nhiều trong Controller.
- **Repository Pattern**: Có `OrderRepository` nhưng đặt trong `Models` (nên chuyển sang thư mục `Repositories` hoặc `Data`).
- **Dependency Injection**: Được sử dụng đúng cách trong `Program.cs`.

## 3. Tính năng (Features)
### Người dùng (User)
- **Duyệt sản phẩm**: Có phân trang, lọc theo danh mục, tìm kiếm, lọc theo giá.
- **Giỏ hàng**: Hỗ trợ cả khách vãng lai (Session) và thành viên (Database).
- **Thanh toán**: Có chức năng Checkout (tuy chưa xem chi tiết Controller nhưng flow có vẻ đầy đủ).
- **Tài khoản**: Đăng ký, Đăng nhập (Local + Google), Quản lý đơn hàng cá nhân.

### Quản trị (Admin)
- **Quản lý sản phẩm**: Thêm/Sửa/Xóa, Upload nhiều ảnh, Cập nhật tồn kho nhanh, Cập nhật giá hàng loạt.
- **Quản lý đơn hàng**: Có Controller `OrdersController`.
- **Thống kê**: Có thống kê cơ bản (Tổng sản phẩm, Tồn kho, Giá trị kho) trong Dashboard.

## 4. Logic & Chất lượng mã (Logic & Code Quality)
### Điểm tốt
- **Clean Code**: Mã nguồn khá sạch, dễ đọc.
- **Admin Features**: Các tính năng Bulk Update/Delete trong Admin rất tiện lợi.
- **Identity Customization**: Đã tùy chỉnh `ApplicationUser` và `IdentityRole` để tối ưu bảng trong DB.

### Vấn đề cần khắc phục (Critical & Major)
1.  **Lỗi lưu trữ ảnh (Critical)**:
    - Trong `Product.cs`, có thuộc tính `public List<string> ImageGallery { get; set; }`.
    - Trong `ApplicationDbContext`, **không có cấu hình** để map `List<string>` vào Database (ví dụ: `ToJson()` hoặc bảng riêng).
    - **Hậu quả**: Khi lưu sản phẩm, chỉ có `ImageUrl` (ảnh đầu tiên) được lưu vào cột `ImageUrl`. Danh sách `ImageGallery` sẽ **bị mất** khi reload lại từ DB.
    - **Khắc phục**: Cần cấu hình `PrimitiveCollection` (EF Core 8+) hoặc ValueConverter để lưu dưới dạng JSON.

2.  **Logic trong Controller (Major)**:
    - `ProductController.Index` chứa quá nhiều logic lọc và chuẩn bị dữ liệu View. Nên tách ra Service hoặc Query Object.
    - `CartController` lặp lại logic xử lý cho 2 trường hợp (Login vs Anonymous). Nên gộp logic này vào một `CartService`.

3.  **Repository Pattern chưa chuẩn**:
    - `OrderRepository.GetAll()` trả về `List<Order>` thay vì `IQueryable` hoặc phân trang. Điều này sẽ gây lỗi `OutOfMemory` nếu dữ liệu lớn.

## 5. Hiệu suất (Performance)
1.  **N+1 Query**:
    - `OrderRepository` đã dùng `.Include(o => o.OrderItems)`, tránh được lỗi N+1 cơ bản. Tốt.
2.  **Tải dữ liệu lớn**:
    - `ProductController.Index` đã phân trang (PageSize = 6). Tốt.
    - Tuy nhiên, `OrderRepository.GetAll()` tải **toàn bộ** đơn hàng về RAM. Cần sửa gấp.
3.  **Sắp xếp ngẫu nhiên**:
    - `ProductController.Details` dùng `.OrderBy(p => Guid.NewGuid())` để lấy sản phẩm liên quan.
    - **Vấn đề**: Với bảng dữ liệu lớn (hàng chục ngàn dòng), lệnh này rất chậm vì DB phải scan toàn bộ bảng để tạo GUID.
    - **Khắc phục**: Dùng thuật toán random offset hoặc lấy theo ID ngẫu nhiên.
4.  **Session**:
    - Lưu giỏ hàng trong Session là ổn cho quy mô nhỏ. Nếu scale lên nhiều server (Web Farm), cần dùng Redis Distributed Cache.

## 6. Kết luận & Đề xuất
Dự án có nền tảng tốt, cấu trúc rõ ràng và tính năng khá đầy đủ cho một trang TMĐT cơ bản. Tuy nhiên, cần khắc phục ngay lỗi lưu trữ `ImageGallery` và tối ưu lại `OrderRepository` để đảm bảo vận hành ổn định.

### Checklist hành động gợi ý:
- [ ] **Fix Bug**: Cấu hình `ImageGallery` trong `ApplicationDbContext` (dùng `ToJson()` nếu dùng SQL Server 2016+).
- [ ] **Refactor**: Chuyển `OrderRepository` sang thư mục `Repositories` và thêm phân trang.
- [ ] **Refactor**: Tách logic giỏ hàng ra `CartService`.
- [ ] **Optimize**: Thay thế `Guid.NewGuid()` bằng giải pháp random hiệu quả hơn.
