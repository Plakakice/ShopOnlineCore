# Giải Thích Chi Tiết & Kịch Bản Bảo Vệ Đồ Án

Tài liệu này bao gồm **7 Tính Năng Cốt Lõi** của dự án.
Mỗi tính năng gồm:
1.  **Bảng Tra Cứu**: Để bạn tìm nhanh dòng code.
2.  **Kịch Bản Trả Lời**: Văn phong sinh viên để bạn trình bày.

---

## 1. Hiển Thị, Lọc & Phân Trang (Product Listing)

### 1.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Lọc & Tìm kiếm** | [Controllers/ProductController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs) | **Method [Index](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs#110-145) (Lines 27-128)**: Dùng **LINQ** cộng dồn điều kiện `Where(...)` (theo tên, theo khoảng giá) rồi mới query xuống DB. |
| **Sắp xếp Ngẫu nhiên** | [Controllers/ProductController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs) | **Lines 80-93**: Thay vì `ORDER BY NEWID()` (chậm), em lấy danh sách ID về RAM -> Xáo trộn bằng `Random.Next()` -> Chỉ lấy chi tiết 8 ID của trang hiện tại. |
| **Thanh trượt giá** | [Views/Product/Index.cshtml](file:///f:/Code/Web/ShopOnlineCore/Views/Product/Index.cshtml) | **Script (Lines 251-339)**: Javascript xử lý thanh trượt đôi (Dual Slider), cập nhật giá trị vào input hidden và submit form. |
| **Phân trang** | [Controllers/ProductController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs) | **Lines 28-30 & 86**: Dùng `.Skip((page-1)*size).Take(size)` để tối ưu hiệu năng SQL. |

### 1.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Em làm thế nào để hiển thị, lọc và phân trang sản phẩm?"*
**Trả lời:**
"Dạ thưa thầy, em xử lý chính ở [ProductController](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs#15-23).
Về bộ lọc, em dùng LINQ để nối các điều kiện `Where` (như theo tên, giá bán) lại với nhau rồi mới truy vấn xuống SQL để tối ưu tốc độ.
Đặc biệt về phân trang, em không load tất cả sản phẩm mà chỉ dùng `.Skip` và `.Take` để lấy đúng 8 sản phẩm cần hiển thị mỗi trang thôi ạ.
Ngoài ra, để trang chủ sinh động, em có viết thuật toán 'Sắp xếp ngẫu nhiên' tại bộ nhớ (In-Memory Shuffling) thay vì sort dưới database để tránh câu lệnh sort nặng nề của SQL."

---

## 2. Giỏ Hàng Thông Minh (Smart Cart)

### 2.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Lấy Giỏ hàng** | [Services/CartService.cs](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs) | **Method [GetCartItemsAsync](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs#25-48)**: Check User ID. Nếu `null` (chưa login) thì đọc `Session`. Nếu có ID thì đọc bảng `CartLines` trong DB. |
| **Thêm vào Giỏ** | [Services/CartService.cs](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs) | **Method [AddToCartAsync](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs#72-132)**: Luôn kiểm tra tồn kho (`product.Stock`) trước (Line 77). Sau đó tùy trạng thái login mà lưu vào DB hay Session. |
| **Lưu trữ Lai** | [Services/CartService.cs](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs) | Cơ chế **Hybrid**: Session (Khách vãng lai) + Database (Khách quen). |

### 2.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Thế bây giờ em làm cái shoppingcart như nào?"*
**Trả lời:**
"Dạ thưa thầy, em xử lý theo cơ chế **Hybrid (Lưu trữ lai)** để tối ưu trải nghiệm người dùng ạ.
Cụ thể em chia làm 2 trường hợp trong [CartService](file:///f:/Code/Web/ShopOnlineCore/Services/CartService.cs#14-19):
1. **Khi khách CHƯA đăng nhập:** Em lưu giỏ hàng vào **Session**. Để khách vãng lai có thể thêm hàng nhanh chóng mà không bị bắt ép phải đăng ký ngay.
2. **Khi khách ĐÃ đăng nhập:** Em lưu trực tiếp vào **Database** (bảng `CartLines`). Để đảm bảo dữ liệu đồng bộ, ví dụ khách thêm hàng trên máy tính, về nhà mở điện thoại lên vẫn thấy giỏ hàng đó.
Ngoài ra, trước khi thêm vào giỏ, em luôn **kiểm tra tồn kho (Stock)**. Nếu khách mua nhiều hơn số lượng còn trong kho thì em chặn lại ngay ạ."

---

## 3. Quy Trình Đặt Hàng (Checkout)

### 3.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Auto-fill Thông tin** | [Controllers/CheckoutController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs) | **Method [Index](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs#110-145) (GET)**: Tự động lấy Address/Phone từ đơn hàng **gần nhất** của user (`_orderRepository.GetOrdersByUserIdAsync`) để điền sẵn form. |
| **Giao dịch (Transaction)** | [Controllers/CheckoutController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs) | **Method [Index](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs#110-145) (POST)**: Thực hiện tuần tự: Validate -> Tạo Order -> Trừ Kho ([Stock](file:///f:/Code/Web/ShopOnlineCore/Areas/Admin/Controllers/ProductsController.cs#63-76)) -> Xóa Giỏ ([ClearCart](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs#34-38)) -> Thông báo. |

### 3.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Quy trình đặt hàng diễn ra thế nào?"*
**Trả lời:**
"Dạ tại trang Checkout, em có làm tính năng **Tự động điền (Auto-fill)** thông minh.
Hệ thống sẽ tra cứu đơn hàng gần nhất của khách để lấy địa chỉ cũ và tự điền vào, khách không phải gõ lại.
Khi khách bấm 'Đặt hàng', hệ thống sẽ kiểm tra tồn kho lần cuối, sau đó mới trừ kho và tạo đơn hàng. Nếu sản phẩm hết hàng ngay lúc đó thì hệ thống sẽ báo lỗi ngay ạ."

---

## 4. Hệ Thống Thông Báo (Notifications)

### 4.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Backend** | `Controllers/*.cs` | Dùng `TempData["Success"]` hoặc `TempData["Error"]` để truyền thông điệp qua các lần Redirect (chuyển trang). |
| **Frontend UI** | [Views/Shared/_Toasts.cshtml](file:///f:/Code/Web/ShopOnlineCore/Views/Shared/_Toasts.cshtml) | C# đọc `TempData` -> Render HTML. JS tự động trượt thông báo ra (class `.toast-item.show`) và ẩn sau 3s-6s. |

### 4.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Cái pop-up thông báo xanh đỏ kia em làm thế nào?"*
**Trả lời:**
"Dạ em xây dựng một hệ thống **Toast Notification** tùy chỉnh.
Ở Backend em dùng `TempData` để gửi thông điệp, vì `TempData` tồn tại được qua bước chuyển trang.
Ở Frontend em dùng Javascript trong file [_Toasts.cshtml](file:///f:/Code/Web/ShopOnlineCore/Views/Shared/_Toasts.cshtml) để hứng thông điệp này và hiển thị hiệu ứng trượt ra (slide-in) cho đẹp mắt, thay vì dùng `alert()` mặc định nhìn rất thô sơ ạ."

---

## 5. Đăng Nhập & Bảo Mật (Authentication)

### 5.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Login Google** | [ExternalLogin.cshtml.cs](file:///f:/Code/Web/ShopOnlineCore/Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs) | **Method [OnGetCallbackAsync](file:///f:/Code/Web/ShopOnlineCore/Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs#100-141)**: Nhận token từ Google. Nếu email chưa tồn tại -> Tự động tạo CreateUser -> Link Login. |
| **Code-Behind** | `Areas/Identity/.../Login.cshtml.cs` | Sử dụng **ASP.NET Core Identity** mặc định để xử lý Hash password và chống Brute-force (Lockout). |

### 5.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Trang này bảo mật thế nào? Đăng nhập Google hoạt động ra sao?"*
**Trả lời:**
"Dạ, về bảo mật em dùng chuẩn **ASP.NET Core Identity** của Microsoft, mật khẩu được mã hóa chứ không lưu text thường.
Về Đăng nhập Google, em tích hợp OAuth2. Khi Google xác thực thành công, em kiểm tra Email đó trong database. Nếu là khách mới, hệ thống em **tự động tạo tài khoản** cho họ luôn mà không cần đăng ký thủ công, giúp người dùng vào mua hàng nhanh nhất có thể ạ."

---

## 6. Quản Trị Hệ Thống (Admin Panel)

### 6.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Phân quyền** | `Areas/Admin/Controllers/*.cs` | Dùng Attribute `[Authorize(Roles = "Admin")]` trên đầu Controller. User thường truy cập sẽ bị đẩy ra trang Login hoặc Access Denied. |
| **Quản lý Sản phẩm** | [Controllers/ProductController.cs](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs) | Các hàm [Create](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs#153-162), [Edit](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs#235-247), [Delete](file:///f:/Code/Web/ShopOnlineCore/Controllers/ProductController.cs#319-333) (Lines 156-332) có check quyền Admin. Xử lý upload ảnh vào folder `wwwroot` thay vì lưu vào DB. |
| **Dashboard** | [Areas/Admin/Controllers/ProductsController.cs](file:///f:/Code/Web/ShopOnlineCore/Areas/Admin/Controllers/ProductsController.cs) | Thống kê nhanh: Tổng sản phẩm, Tổng giá trị kho, Sản phẩm hết hàng (Method [Index](file:///f:/Code/Web/ShopOnlineCore/Controllers/CheckoutController.cs#110-145)). |

### 6.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Trang Admin em làm những gì?"*
**Trả lời:**
"Dạ trang Admin em tách biệt hẳn ra một Area riêng.
Em dùng Attribute `[Authorize]` để chặn tuyệt đối người lạ. Trong này em làm các chức năng CRUD sản phẩm, đặc biệt là phần **Upload ảnh**, em lưu ảnh vào thư mục web (`wwwroot`) để giảm tải cho Database.
Ngoài ra em có làm một Dashboard nhỏ để thống kê nhanh số lượng hàng tồn và giá trị kho hàng ạ."

---

## 7. Khởi Tạo Hệ Thống (App Startup)

### 7.1 Bảng Tra Cứu Kỹ Thuật
| Tính năng | File Chịu Trách Nhiệm | Logic Code (Chi tiết kỹ thuật) |
| :--- | :--- | :--- |
| **Tự động tạo Admin** | [Program.cs](file:///f:/Code/Web/ShopOnlineCore/Program.cs) | **Method `CreateAdminRole` (Lines 97-130)**: Check Role "Admin". Check user `admin@shop.com`. Nếu chưa có -> Tự tạo (`UserManager.CreateAsync`). |
| **Cấu hình Service** | [Program.cs](file:///f:/Code/Web/ShopOnlineCore/Program.cs) | Đăng ký Dependency Injection cho `IProductService`, `ICartService`, Session, Google Auth. |

### 7.2 Kịch Bản Trả Lời (Sinh viên)
**GV hỏi:** *"Làm sao thầy có tài khoản Admin để chấm bài?"*
**Trả lời:**
"Dạ thầy yên tâm ạ. Em đã viết code trong [Program.cs](file:///f:/Code/Web/ShopOnlineCore/Program.cs) để **Tự động khởi tạo (Seeding)**.
Chỉ cần thầy chạy project lên lần đầu tiên, hệ thống sẽ tự động tạo tài khoản Admin mặc định là `admin@shop.com` với mật khẩu em ghi trong báo cáo. Thầy không cần phải chọc vào Database để insert dữ liệu thủ công đâu ạ."
