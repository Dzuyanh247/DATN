# Tổng hợp kiến trúc dự án DATN PC Store

Tài liệu này tóm tắt nhanh mục tiêu, kiến trúc và luồng hoạt động của hệ thống.

## 1) Dự án là gì?
- Đây là website thương mại điện tử bán PC/laptop/linh kiện (KKSHOP/PCStore).
- Có cả khu vực khách hàng (xem sản phẩm, giỏ hàng, checkout, tra cứu đơn) và admin (quản lý sản phẩm, đơn hàng, banner, người dùng, danh mục, bảo hành, cài đặt trang).

## 2) Công nghệ chính
- Nền tảng: ASP.NET Core MVC (.NET) với Razor Views.
- ORM + DB access: Entity Framework Core.
- Database: SQL Server.
- Xác thực: Cookie Authentication tự triển khai (không dùng ASP.NET Identity đầy đủ).
- Session: dùng cho giỏ hàng guest và theo dõi đơn gần nhất.

## 3) Frontend
- Render server-side bằng Razor (`.cshtml`).
- Styling: CSS thuần trong `wwwroot/css`.
- JS: một số script nhỏ (`wwwroot/js`) như admin banner/chat.

## 4) Backend
- Controller MVC xử lý request/response HTML.
- Service layer:
  - `AuthService`: hash/verify password, validate login.
  - `CartService`: nghiệp vụ giỏ hàng cho guest/user.
  - `ProductImageStorageService`: xử lý lưu ảnh sản phẩm.
- `ApplicationDbContext`: định nghĩa DbSet + quan hệ + ràng buộc.

## 5) Dữ liệu
Bảng/model chính:
- Người dùng/quyền: `User`, `Role`.
- Catalog: `Category`, `Product`, `ProductImage`, `Banner`, `SiteSetting`, `Article`.
- Mua hàng: `Cart`, `CartItem`, `Order`, `OrderDetail`.
- Hậu mãi: `Warranty`, `WarrantyRequest`, `Feedback`.
- Build PC: `BuildPcConfig`, `BuildPcItem`.

## 6) Luồng hoạt động chính
1. Người dùng vào trang chủ -> backend lấy category/banner/sản phẩm -> render HTML.
2. Người dùng lọc/xem sản phẩm chi tiết.
3. Thêm giỏ hàng:
   - Guest: lưu session.
   - Đăng nhập: lưu DB bảng `Cart`/`CartItem`.
4. Checkout:
   - Validate form + kiểm tra tồn kho.
   - Tạo `Order` + `OrderDetail` trong transaction.
   - Trừ tồn kho `Product.StockQuantity`.
   - Xóa giỏ hàng.
5. Tra cứu đơn:
   - User đã login xem đơn của mình.
   - Guest tra cứu theo mã đơn + số điện thoại.
6. Admin đăng nhập và quản trị dữ liệu.

## 7) Điểm cần cải thiện
- Bảo mật mật khẩu hiện chỉ SHA256 + salt tĩnh (nên nâng cấp sang ASP.NET Identity + PBKDF2/Bcrypt/Argon2).
- Chưa thấy bộ test tự động (unit/integration).
- Thiếu API REST công khai rõ ràng; chủ yếu MVC trả HTML.
- Một số logic migration/schema đang vá bằng SQL runtime trong `Program.cs` (nên chuẩn hóa migration).
