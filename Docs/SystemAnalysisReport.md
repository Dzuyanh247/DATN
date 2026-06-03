# Báo cáo phân tích hệ thống PC Store - KKSHOP

## Mục lục

1. [Tổng quan đề tài](#1-tổng-quan-đề-tài)
2. [Mục tiêu hệ thống](#2-mục-tiêu-hệ-thống)
3. [Kiến trúc hệ thống](#3-kiến-trúc-hệ-thống)
4. [Công nghệ sử dụng](#4-công-nghệ-sử-dụng)
5. [Lý do lựa chọn công nghệ](#5-lý-do-lựa-chọn-công-nghệ)
6. [Database](#6-database)
7. [Các bảng dữ liệu](#7-các-bảng-dữ-liệu)
8. [Chức năng người dùng](#8-chức-năng-người-dùng)
9. [Chức năng quản trị](#9-chức-năng-quản-trị)
10. [API và tích hợp](#10-api-và-tích-hợp)
11. [Bảo mật](#11-bảo-mật)
12. [Hiệu năng](#12-hiệu-năng)
13. [Ưu điểm](#13-ưu-điểm)
14. [Hạn chế](#14-hạn-chế)
15. [Hướng phát triển](#15-hướng-phát-triển)
16. [Kết luận](#16-kết-luận)
17. [Phụ lục: file source code đã phân tích](#17-phụ-lục-file-source-code-đã-phân-tích)

---

## 1. Tổng quan đề tài

Dự án PC Store là một ứng dụng web thương mại điện tử phục vụ bán máy tính, laptop, linh kiện và các sản phẩm công nghệ dưới tên hiển thị KKSHOP. Source code thể hiện hệ thống được xây dựng theo mô hình ASP.NET Core MVC, có phần giao diện khách hàng, phần quản trị, cơ chế đăng nhập theo cookie, giỏ hàng, đặt hàng, quản lý sản phẩm, quản lý đơn hàng, bảo hành, bài viết, banner, cài đặt website và tính phí vận chuyển.

Các bằng chứng chính trong source code:

- `Datn.PcStore.csproj` sử dụng `Microsoft.NET.Sdk.Web`, target framework `net8.0` và các gói Entity Framework Core cho SQL Server.
- `Program.cs` đăng ký MVC, `ApplicationDbContext`, cookie authentication, session, memory cache, các service nghiệp vụ và các HTTP client tích hợp bên ngoài.
- `Data/ApplicationDbContext.cs` khai báo DbSet cho các thực thể nghiệp vụ như người dùng, vai trò, danh mục, sản phẩm, giỏ hàng, đơn hàng, bảo hành, cấu hình build PC, bài viết, phản hồi, cấu hình website, vận chuyển và OTP đặt lại mật khẩu.
- Thư mục `Controllers` có các controller cho luồng khách hàng và quản trị.
- Thư mục `Views` chứa Razor Views tương ứng với các màn hình MVC.
- Thư mục `Services` chứa các service xử lý xác thực, giỏ hàng, so sánh sản phẩm, build PC, email, vận chuyển, bản đồ và GHN.

Phạm vi báo cáo chỉ dựa trên các file source code hiện có trong repository. Những nội dung không có trong source code được ghi rõ là chưa thấy hoặc chưa triển khai đầy đủ.

---

## 2. Mục tiêu hệ thống

Dựa trên chức năng được triển khai trong source code, hệ thống hướng đến các mục tiêu sau:

1. **Cung cấp kênh bán hàng trực tuyến cho cửa hàng PC/laptop/linh kiện**
   - Người dùng có thể xem danh mục, xem sản phẩm, lọc và tìm kiếm sản phẩm.
   - Người dùng có thể xem chi tiết sản phẩm, ảnh sản phẩm, thông số và gợi ý nâng cấp.

2. **Hỗ trợ quy trình mua hàng**
   - Cho phép thêm sản phẩm vào giỏ hàng.
   - Hỗ trợ giỏ hàng cho khách chưa đăng nhập bằng session và giỏ hàng trong database cho người dùng đã đăng nhập.
   - Cho phép checkout, tạo đơn hàng, ghi nhận chi tiết đơn hàng và trừ tồn kho.

3. **Hỗ trợ thanh toán COD và chuyển khoản ngân hàng**
   - Đơn COD được tạo với trạng thái chờ xác nhận.
   - Đơn chuyển khoản có trạng thái chờ thanh toán, thời hạn thanh toán 2 giờ và nội dung chuyển khoản theo mã đơn.
   - Admin có chức năng xác nhận chuyển khoản.

4. **Hỗ trợ vận chuyển**
   - Có API lấy tỉnh/thành, quận/huyện, phường/xã từ GHN.
   - Có API tính phí vận chuyển dựa trên địa chỉ và giỏ hàng.
   - Có cấu hình chính sách vận chuyển và địa chỉ cửa hàng.

5. **Hỗ trợ tư vấn cấu hình PC**
   - Người dùng chọn linh kiện theo nhóm CPU, mainboard, RAM, GPU, storage, PSU, cooler, case, monitor.
   - Hệ thống lưu cấu hình build PC trong session.
   - Có thể thêm toàn bộ cấu hình vào giỏ hàng và xuất cấu hình CSV.

6. **Hỗ trợ so sánh sản phẩm**
   - Người dùng thêm tối đa 2 sản phẩm vào danh sách so sánh.
   - Dữ liệu so sánh được lưu trong session.
   - Màn hình so sánh hiển thị các dòng thông số như CPU, RAM, GPU, SSD, mainboard, nguồn, case và tản nhiệt.

7. **Hỗ trợ quản trị cửa hàng**
   - Admin quản lý sản phẩm, danh mục, đơn hàng, người dùng, banner, cài đặt site và yêu cầu bảo hành.
   - Admin dashboard thống kê số lượng sản phẩm, đơn hàng, người dùng và yêu cầu bảo hành.

8. **Hỗ trợ tài khoản người dùng**
   - Đăng ký, đăng nhập, đăng xuất.
   - Cập nhật hồ sơ cá nhân.
   - Đổi mật khẩu.
   - Quên mật khẩu bằng OTP gửi email.

---

## 3. Kiến trúc hệ thống

### 3.1 Mô hình kiến trúc tổng quát

Dự án sử dụng kiến trúc web MVC truyền thống của ASP.NET Core. Các thành phần chính gồm:

- **Razor Views**: hiển thị HTML cho người dùng và admin.
- **Controllers**: nhận request, kiểm tra dữ liệu, gọi service hoặc truy vấn database, trả về view/json/redirect.
- **Services**: đóng gói nghiệp vụ dùng lại như xác thực, giỏ hàng, so sánh, vận chuyển, email, bản đồ.
- **Entity Framework Core DbContext**: ánh xạ model C# sang database SQL Server.
- **Models/Entities**: mô tả cấu trúc dữ liệu nghiệp vụ.
- **ViewModels**: mô hình dữ liệu dành cho giao diện.
- **Session**: lưu dữ liệu tạm như giỏ hàng khách, danh sách so sánh, cấu hình build PC, đơn hàng gần nhất.
- **Cookie Authentication**: lưu trạng thái đăng nhập bằng cookie nội bộ `PcStoreCookie`.

### 3.2 Sơ đồ kiến trúc bằng text

```text
+-----------------------------+
|         Web Browser         |
|  HTML / CSS / JavaScript    |
|  Bootstrap / Razor output   |
+--------------+--------------+
               |
               | HTTP Request/Response
               v
+--------------+--------------+
|      ASP.NET Core MVC       |
| Program.cs Middleware       |
| - Static files              |
| - Routing                   |
| - Session                   |
| - Authentication            |
| - Authorization             |
+--------------+--------------+
               |
               v
+-----------------------------+
|          Controllers        |
| Account, Products, Cart,    |
| Orders, BuildPc, Compare,   |
| Shipping, Admin*            |
+-----+-----------------+-----+
      |                 |
      v                 v
+-----+--------+   +----+----------------+
| Services     |   | Razor Views         |
| Auth, Cart,  |   | Views/*.cshtml      |
| Email, GHN,  |   | Shared Layout       |
| Shipping,    |   | Partial Views       |
| Compare      |   +---------------------+
+-----+--------+
      |
      v
+-----+-----------------------+
| Entity Framework Core       |
| ApplicationDbContext        |
| DbSet<Model>                |
+-----+-----------------------+
      |
      v
+-----+-----------------------+
| SQL Server Database         |
| Roles, Users, Products,     |
| Orders, OrderDetails, ...   |
+-----------------------------+

Tích hợp ngoài:
- GHN API: địa chỉ và phí giao hàng.
- OpenRouteService: geocoding/route.
- SMTP: gửi email OTP đặt lại mật khẩu.
```

### 3.3 Luồng request điển hình

Ví dụ luồng đặt hàng:

```text
Người dùng -> /Checkout -> OrdersController.Checkout(GET)
           -> Razor View Checkout hiển thị thông tin giỏ hàng
           -> JavaScript gọi /api/shipping/calculate để tính phí ship
           -> OrdersController.Checkout(POST)
           -> CartService lấy giỏ hàng
           -> EF Core kiểm tra Products và tồn kho
           -> Tạo Orders + OrderDetails trong transaction
           -> Trừ StockQuantity của Products
           -> Lưu SQL Server
           -> Redirect sang Success hoặc BankTransfer
```

Ví dụ luồng đăng nhập:

```text
Người dùng -> /Account/Login
           -> AccountController.Login(POST)
           -> AuthService.ValidateUserAsync
           -> kiểm tra Users + Role trong database
           -> tạo ClaimsPrincipal gồm NameIdentifier, Name, Email, username, Role
           -> SignInAsync bằng cookie scheme PcStoreCookie
           -> CartService.MergeGuestCartAsync để gộp giỏ session vào giỏ database
           -> Redirect Home/Index
```

---

## 4. Công nghệ sử dụng

### 4.1 ASP.NET Core MVC

- **Vai trò trong dự án**: framework chính để xây dựng web application. `Program.cs` gọi `AddControllersWithViews()` và map route mặc định `{controller=Home}/{action=Index}/{id?}`.
- **Nơi sử dụng**: toàn bộ các controller trong thư mục `Controllers`, các view trong `Views`.
- **Ưu điểm**:
  - Phù hợp ứng dụng web thương mại điện tử dạng server-side rendering.
  - Tách biệt controller, view, model giúp dễ bảo trì.
  - Hỗ trợ middleware, routing, dependency injection, authentication/authorization.
- **Nhược điểm**:
  - Khi giao diện nhiều tương tác real-time, MVC server-side có thể kém linh hoạt hơn SPA.
  - Một số trang cần JavaScript riêng để tăng trải nghiệm người dùng.
- **Lý do lựa chọn theo source code**: dự án cần các trang web truyền thống như sản phẩm, giỏ hàng, checkout, quản trị và Razor View; MVC đáp ứng trực tiếp mô hình này.

### 4.2 Razor Views

- **Vai trò trong dự án**: sinh HTML phía server cho giao diện khách hàng và admin.
- **Nơi sử dụng**: `Views/Account`, `Views/Products`, `Views/Cart`, `Views/Orders`, `Views/Admin*`, `Views/Shared`.
- **Ưu điểm**:
  - Kết hợp dữ liệu C# và HTML thuận tiện.
  - Hỗ trợ tag helper như `asp-controller`, `asp-action`, `asp-for`.
  - Dễ copy nội dung giao diện từ model/viewmodel.
- **Nhược điểm**:
  - Logic hiển thị nếu viết quá nhiều trong view có thể làm view phức tạp.
  - Ít phù hợp với giao diện SPA lớn.
- **Lý do lựa chọn theo source code**: hệ thống đã chia view theo controller, sử dụng shared layout và partial view cho header, sản phẩm, so sánh, notification.

### 4.3 HTML/CSS/JavaScript

- **Vai trò trong dự án**:
  - HTML/Razor tạo markup.
  - CSS trong `wwwroot/css` tùy biến giao diện khách hàng, admin, giỏ hàng, build PC, báo giá.
  - JavaScript trong `wwwroot/js` hỗ trợ checkout, build PC, banner admin và chat.
- **File tiêu biểu**:
  - `wwwroot/css/site.css`
  - `wwwroot/css/admin.css`
  - `wwwroot/css/cart.css`
  - `wwwroot/css/buildpc.css`
  - `wwwroot/js/checkout.js`
  - `wwwroot/js/buildpc.js`
  - `wwwroot/js/admin-banner.js`
- **Ưu điểm**:
  - Không phụ thuộc frontend framework lớn.
  - Dễ triển khai cùng ASP.NET Core static files.
- **Nhược điểm**:
  - JavaScript thuần có thể khó mở rộng nếu nhiều trạng thái phức tạp.
  - Cần tự tổ chức code client-side để tránh trùng lặp.
- **Lý do lựa chọn theo source code**: các tính năng động như tính ship, chọn cấu hình build PC, banner preview có thể xử lý bằng JavaScript nhỏ gọn.

### 4.4 Bootstrap và Bootstrap Icons

- **Vai trò trong dự án**: framework CSS và icon cho giao diện responsive, button, table, form, grid, layout admin và icon menu.
- **Nơi sử dụng**: `Views/Shared/_Layout.cshtml` nạp Bootstrap 5.3.3 và Bootstrap Icons 1.11.3 từ CDN.
- **Ưu điểm**:
  - Tăng tốc xây dựng UI.
  - Hỗ trợ responsive trên nhiều kích thước màn hình.
  - Có sẵn component form, table, navbar, button, alert.
- **Nhược điểm**:
  - Giao diện có thể giống template phổ biến nếu không tùy biến CSS.
  - Phụ thuộc CDN nếu không bundle local.
- **Lý do lựa chọn theo source code**: nhiều view dùng class Bootstrap như `container`, `row`, `col`, `btn`, `table`, `alert`, `form-control`, đồng thời danh mục sử dụng `IconClass` mặc định `bi bi-grid`.

### 4.5 SQL Server

- **Vai trò trong dự án**: hệ quản trị cơ sở dữ liệu lưu users, roles, products, orders, carts, shipping configs, articles và các bảng nghiệp vụ khác.
- **Nơi cấu hình**: `appsettings.json` và `appsettings.Development.json` có connection string `DefaultConnection` dùng SQL Server Express database `DATN_PCStore`.
- **Ưu điểm**:
  - Phù hợp với .NET và Entity Framework Core.
  - Hỗ trợ transaction, index, constraint, kiểu dữ liệu decimal/datetime2/nvarchar.
- **Nhược điểm**:
  - Cần cài đặt SQL Server/SQL Server Express trong môi trường chạy.
  - Connection string trong source hiện trỏ tới máy local cụ thể.
- **Lý do lựa chọn theo source code**: `UseSqlServer()` được cấu hình trong `Program.cs`, csproj tham chiếu `Microsoft.EntityFrameworkCore.SqlServer`.

### 4.6 Entity Framework Core

- **Vai trò trong dự án**: ORM ánh xạ các entity C# sang SQL Server, truy vấn dữ liệu bằng LINQ, migration và cập nhật schema.
- **Nơi sử dụng**:
  - `Data/ApplicationDbContext.cs`: DbSet, cấu hình model, index, precision, quan hệ.
  - `Controllers/*`: truy vấn `DbContext` bằng LINQ.
  - `Migrations/*`: bổ sung cột khuyến mãi, thời hạn thanh toán, trường gateway thanh toán, bảng OTP.
- **Ưu điểm**:
  - Giảm lượng SQL thủ công trong nghiệp vụ.
  - Dễ định nghĩa quan hệ bằng navigation property và Fluent API.
  - Hỗ trợ migration và transaction.
- **Nhược điểm**:
  - Cần chú ý hiệu năng truy vấn, Include, tracking.
  - Migration hiện chỉ có các migration bổ sung, chưa thấy migration khởi tạo đầy đủ trong thư mục `Migrations`.
- **Lý do lựa chọn theo source code**: DbContext là trung tâm truy cập database; mọi controller/service đều dùng EF Core để truy vấn/lưu dữ liệu.

### 4.7 ASP.NET Identity

- **Vai trò thực tế trong dự án**: source code không dùng đầy đủ hệ bảng Identity mặc định như `AspNetUsers`, `AspNetRoles`. Dự án tự định nghĩa bảng `Users` và `Roles`, nhưng có sử dụng thành phần của ASP.NET Identity:
  - `IdentityOptions` để cấu hình rule mật khẩu.
  - `PasswordHasher<User>` để xác thực các hash bắt đầu bằng `AQAAAA` và tạo hash trong luồng reset password.
  - `IdentityResult` và `IdentityError` trong service đặt lại mật khẩu.
- **Ưu điểm**:
  - Tận dụng chuẩn hash mật khẩu của Identity cho các luồng mới.
  - Có thể mở rộng sang Identity đầy đủ trong tương lai.
- **Nhược điểm**:
  - Vì không dùng Identity đầy đủ, hệ thống phải tự quản lý user, role, claim, khóa tài khoản, reset password.
  - Trong source hiện có song song SHA256 + salt demo và PasswordHasher, cần thống nhất để tăng an toàn.
- **Lý do lựa chọn theo source code**: dự án có mô hình user/role riêng nhưng cần một số tiện ích password policy và hashing từ ASP.NET Identity.

### 4.8 Session

- **Vai trò trong dự án**:
  - Lưu giỏ hàng khách chưa đăng nhập bằng key `guest_cart`.
  - Lưu danh sách so sánh bằng key `CompareProductIds`.
  - Lưu cấu hình build PC bằng key `buildpc_selected`.
  - Lưu `LastOrderId` và `LastPendingPaymentOrderId` cho luồng đơn khách/đơn chuyển khoản.
- **Ưu điểm**:
  - Phù hợp dữ liệu tạm, chưa cần đăng nhập.
  - Giúp trải nghiệm mua hàng nhanh hơn.
- **Nhược điểm**:
  - Dữ liệu session phụ thuộc vòng đời session trên server/browser.
  - Nếu scale nhiều server cần cấu hình distributed session.
- **Lý do lựa chọn theo source code**: nhiều tính năng khách vãng lai cần trạng thái tạm trước khi tạo tài khoản hoặc đơn hàng.

### 4.9 Các thư viện và dịch vụ ngoài phát hiện được

#### NuGet packages trong project chính

| Package | Version | Vai trò |
|---|---:|---|
| `Microsoft.EntityFrameworkCore` | 8.0.8 | ORM và LINQ provider nền tảng |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.8 | Hỗ trợ design-time/migration |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.8 | Provider SQL Server |

#### Thư viện frontend qua CDN

| Thư viện | Nơi phát hiện | Vai trò |
|---|---|---|
| Bootstrap 5.3.3 | `_Layout.cshtml` | UI responsive, form, table, button |
| Bootstrap Icons 1.11.3 | `_Layout.cshtml` | Icon menu/danh mục/action |
| Swiper 11 | `_Layout.cshtml`, `Views/Home/Index.cshtml` | Slider/banner/trang chủ |
| html2canvas 1.4.1 | `Views/BuildPc/Index.cshtml` | Hỗ trợ chức năng phía client của build PC |
| jQuery Validate / Unobtrusive | `_ValidationScriptsPartial.cshtml` | Validation form phía client |

#### Tích hợp backend

| Tích hợp | File cấu hình/source | Vai trò |
|---|---|---|
| GHN API | `appsettings.json`, `GhnShippingService`, `GhnAddressService`, `ShippingController` | Lấy địa chỉ và tính phí giao hàng |
| OpenRouteService | `appsettings.json`, `OpenRouteServiceProvider`, `RouteService`, `GeocodingService` | Geocode/route phục vụ vận chuyển |
| SMTP Gmail | `appsettings.json`, `SmtpEmailSender` | Gửi OTP đặt lại mật khẩu |

---

## 5. Lý do lựa chọn công nghệ

Dựa trên cấu trúc source code, lựa chọn công nghệ của dự án phù hợp với bài toán website bán hàng quy mô đồ án:

1. **ASP.NET Core MVC + Razor Views** phù hợp vì hệ thống chủ yếu là các màn hình CRUD, danh sách, chi tiết, form đặt hàng và quản trị. Các controller và view đã được tổ chức rõ theo nghiệp vụ.
2. **Entity Framework Core + SQL Server** phù hợp với hệ sinh thái .NET, giúp thao tác dữ liệu bằng model C# và LINQ, đồng thời vẫn hỗ trợ migration/schema update.
3. **Session** phù hợp với giỏ hàng khách, so sánh sản phẩm và build PC vì đây là dữ liệu tạm thời không bắt buộc lưu lâu dài.
4. **Cookie Authentication + Role Authorization** phù hợp với hệ thống có phân quyền Admin/Staff/Customer nhưng không cần token API phức tạp.
5. **Bootstrap** giúp xây dựng giao diện nhanh, hỗ trợ responsive và đồng bộ style cho cả khách hàng và admin.
6. **GHN/OpenRouteService/SMTP** giúp hệ thống có các tính năng thực tế hơn: vận chuyển, bản đồ và email OTP.

---

## 6. Database

### 6.1 DbContext

`ApplicationDbContext` kế thừa `DbContext` và khai báo các DbSet:

- `Roles`
- `Users`
- `Categories`
- `Products`
- `ProductImages`
- `Banners`
- `Carts`
- `CartItems`
- `Orders`
- `OrderDetails`
- `Warranties`
- `WarrantyRequests`
- `BuildPcConfigs`
- `BuildPcItems`
- `Articles`
- `Feedbacks`
- `SiteSettings`
- `ShippingConfigs`
- `ShopLocations`
- `PasswordResetOtps`

### 6.2 Audit fields

Các entity kế thừa `BaseEntity` có trường:

- `Id`
- `CreatedAt`
- `UpdatedAt`

`ApplicationDbContext` override `SaveChanges` và `SaveChangesAsync` để tự cập nhật `CreatedAt`/`UpdatedAt`. Trong `OnModelCreating`, các entity kế thừa `BaseEntity` được cấu hình default value SQL `GETUTCDATE()` cho hai trường này.

### 6.3 Index và constraint quan trọng

Source code cấu hình các index sau:

- `Users.Email` unique.
- `Users.Username` unique.
- `Products.ProductCode` unique.
- `Products.Slug` unique.
- `Products.SourceUrl` có index.
- `ProductImages(ProductId, SortOrder)` có index.
- `PasswordResetOtps(UserId, IsUsed, ExpiresAt)` có index.
- `PasswordResetOtps(Email, CodeHash)` có index.

### 6.4 Precision tiền tệ và số đo

Các trường tiền tệ được cấu hình precision `decimal(18,2)`:

- `Product.Price`
- `Product.SalePrice`
- `Product.DiscountPrice`
- `Order.SubtotalAmount`
- `Order.DiscountAmount`
- `Order.TotalAmount`
- `Order.ShippingFee`
- `OrderDetail.UnitPrice`
- `OrderDetail.TotalPrice`
- `BuildPcConfig.TotalPrice`
- `ShippingConfig.BaseFee`
- `ShippingConfig.ExtraFeePerKm`

Các trường khoảng cách trong `ShippingConfig` dùng precision `decimal(8,2)`.

### 6.5 Migration và cập nhật schema runtime

Thư mục `Migrations` hiện có các migration bổ sung:

- `202605240001_AddPaymentExpireAtToOrders`: thêm `Orders.PaymentExpireAt`.
- `202605240002_AddProductPromotionFields`: thêm các trường khuyến mãi cho `Products`.
- `202605240003_RepairProductPromotionColumns`: bổ sung SQL kiểm tra và thêm cột khuyến mãi nếu thiếu.
- `202605240004_AddPaymentGatewayFieldsToOrders`: thêm `Orders.PaymentUrl`, `Orders.PaymentTransactionId`.
- `202606030001_AddPasswordResetOtp`: tạo bảng `PasswordResetOtps`, index OTP, cập nhật độ dài `Users.PasswordHash`.

Ngoài migration, `Program.cs` còn gọi `db.Database.Migrate()` khi khởi động và chạy một số SQL đảm bảo tồn tại các cột/bảng như shipping configs, shop locations, cột shipping trong orders, background site settings và product promotion columns.

---

## 7. Các bảng dữ liệu

Ghi chú: PK mặc định là `Id` đối với các entity. Các FK được xác định từ property `...Id` và navigation property trong model, hoặc từ Fluent API trong `ApplicationDbContext`.

### 7.1 Tổng quan bảng và chức năng

| Bảng | PK | FK chính | Chức năng |
|---|---|---|---|
| `Roles` | `Id` | Không có | Lưu vai trò `Admin`, `Staff`, `Customer`. |
| `Users` | `Id` | `RoleId -> Roles.Id` | Lưu tài khoản, thông tin cá nhân, mật khẩu hash, trạng thái hoạt động. |
| `PasswordResetOtps` | `Id` | `UserId -> Users.Id` | Lưu OTP đặt lại mật khẩu, hash OTP, hạn dùng, trạng thái đã dùng. |
| `Categories` | `Id` | `ParentCategoryId -> Categories.Id` | Lưu danh mục cha/con và icon Bootstrap. |
| `Products` | `Id` | `CategoryId -> Categories.Id` | Lưu sản phẩm, giá, tồn kho, mô tả, thông số, bảo hành, flags khuyến mãi. |
| `ProductImages` | `Id` | `ProductId -> Products.Id` | Lưu nhiều ảnh cho sản phẩm, sort order và ảnh chính. |
| `Banners` | `Id` | Không có | Lưu banner trang chủ hoặc vị trí khác. |
| `Carts` | `Id` | `UserId -> Users.Id` | Lưu giỏ hàng database của người dùng đã đăng nhập. |
| `CartItems` | `Id` | `CartId -> Carts.Id`, `ProductId -> Products.Id` | Lưu dòng sản phẩm trong giỏ hàng. |
| `Orders` | `Id` | `UserId -> Users.Id` nullable | Lưu đơn hàng, người nhận, địa chỉ, trạng thái, thanh toán, phí vận chuyển. |
| `OrderDetails` | `Id` | `OrderId -> Orders.Id`, `ProductId -> Products.Id` | Lưu chi tiết từng sản phẩm trong đơn hàng. |
| `Warranties` | `Id` | `ProductId -> Products.Id` | Lưu thông tin phạm vi bảo hành của sản phẩm. |
| `WarrantyRequests` | `Id` | `UserId -> Users.Id`, `ProductId -> Products.Id` | Lưu yêu cầu bảo hành từ người dùng. |
| `BuildPcConfigs` | `Id` | `UserId -> Users.Id` | Lưu cấu hình build PC của người dùng theo model, tuy nhiên luồng controller hiện đang dùng session cho build PC. |
| `BuildPcItems` | `Id` | `BuildPcConfigId -> BuildPcConfigs.Id`, `ProductId -> Products.Id` | Lưu linh kiện trong cấu hình build PC theo model. |
| `Articles` | `Id` | Không có | Lưu bài viết/tin công nghệ/khuyến mãi. |
| `Feedbacks` | `Id` | Không có | Lưu phản hồi liên hệ của khách. |
| `SiteSettings` | `Id` | Không có | Lưu tên site, logo và background section. |
| `ShippingConfigs` | `Id` | Không có | Lưu công thức phí vận chuyển nội bộ. |
| `ShopLocations` | `Id` | Không có | Lưu địa chỉ, tọa độ cửa hàng mặc định. |

### 7.2 Chi tiết bảng `Roles`

- **Trường chính**: `Id`, `Name`, `CreatedAt`, `UpdatedAt`.
- **Quan hệ**: một role có nhiều user.
- **Dữ liệu seed**: `Admin`, `Staff`, `Customer`.
- **Chức năng**: phân quyền controller và menu quản trị.

### 7.3 Chi tiết bảng `Users`

- **Trường chính**: `Username`, `FullName`, `Email`, `PasswordHash`, `Phone`, `Address`, `IsActive`, `RoleId`.
- **PK**: `Id`.
- **FK**: `RoleId -> Roles.Id`.
- **Index**: unique trên `Email` và `Username`.
- **Chức năng**:
  - Đăng ký/đăng nhập.
  - Hồ sơ cá nhân.
  - Admin quản lý user.
  - Liên kết với đơn hàng, giỏ hàng, yêu cầu bảo hành, OTP đặt lại mật khẩu.

### 7.4 Chi tiết bảng `PasswordResetOtps`

- **Trường chính**: `UserId`, `Email`, `CodeHash`, `ExpiresAt`, `IsUsed`, `CreatedAt`, `UsedAt`.
- **PK**: `Id`.
- **FK**: `UserId -> Users.Id`, delete cascade theo Fluent API.
- **Index**:
  - `UserId, IsUsed, ExpiresAt`.
  - `Email, CodeHash`.
- **Chức năng**: hỗ trợ quên mật khẩu bằng mã OTP 6 chữ số gửi email, hiệu lực 10 phút.

### 7.5 Chi tiết bảng `Categories`

- **Trường chính**: `Name`, `IconClass`, `ParentCategoryId`.
- **PK**: `Id`.
- **FK tự tham chiếu**: `ParentCategoryId -> Categories.Id`.
- **Delete behavior**: `Restrict` cho quan hệ cha/con.
- **Chức năng**: phân loại sản phẩm, hiển thị menu danh mục và bộ lọc sản phẩm.

### 7.6 Chi tiết bảng `Products`

- **Trường chính**:
  - Thông tin nhận diện: `Name`, `Slug`, `ProductCode`, `Brand`.
  - Giá: `Price`, `DiscountPrice`, `SalePrice`.
  - Khuyến mãi: `IsHotSale`, `IsDailyDeal`, `IsPromotion`, `PromotionStartDate`, `PromotionEndDate`.
  - Kho: `StockQuantity`, `IsInStock`, `IsActive`.
  - Ảnh/mô tả: `ThumbnailImage`, `SourceUrl`, `ShortDescription`, `Description`, `DetailDescription`, `Specifications`.
  - Bảo hành: `WarrantyMonths`, `WarrantyDuration`.
  - Build PC: `ComponentType`, `CpuSocket`, `RamType`, `HasSoftwareLicense`.
  - Phân loại: `CategoryId`.
- **PK**: `Id`.
- **FK**: `CategoryId -> Categories.Id`.
- **Index**: unique trên `ProductCode`, unique trên `Slug`, index trên `SourceUrl`.
- **Chức năng**: trung tâm của hệ thống bán hàng, được dùng trong danh sách, chi tiết, giỏ hàng, đơn hàng, so sánh, build PC, bảo hành và admin.

### 7.7 Chi tiết bảng `ProductImages`

- **Trường chính**: `ProductId`, `ImageUrl`, `SortOrder`, `IsPrimary`.
- **PK**: `Id`.
- **FK**: `ProductId -> Products.Id`.
- **Delete behavior**: cascade khi xóa product theo Fluent API.
- **Index**: `ProductId, SortOrder`.
- **Chức năng**: lưu gallery ảnh của sản phẩm, ảnh chính và thứ tự hiển thị.

### 7.8 Chi tiết bảng `Banners`

- **Trường chính**: `Title`, `ImageUrl`, `LinkUrl`, `Description`, `Position`, `SortOrder`, `IsActive`.
- **PK**: `Id`.
- **Chức năng**: quản lý banner hiển thị trang chủ/section. Admin có thể tạo, sửa, bật/tắt, xóa.

### 7.9 Chi tiết bảng `Carts` và `CartItems`

#### `Carts`

- **Trường chính**: `UserId`.
- **PK**: `Id`.
- **FK**: `UserId -> Users.Id`.
- **Chức năng**: lưu giỏ hàng của user đã đăng nhập.

#### `CartItems`

- **Trường chính**: `CartId`, `ProductId`, `Quantity`.
- **PK**: `Id`.
- **FK**:
  - `CartId -> Carts.Id`.
  - `ProductId -> Products.Id`.
- **Chức năng**: lưu từng dòng sản phẩm trong giỏ.

### 7.10 Chi tiết bảng `Orders` và `OrderDetails`

#### `Orders`

- **Trường chính**:
  - Người dùng/khách: `UserId`, `CustomerEmail`.
  - Người nhận: `ReceiverName`, `ReceiverPhone`.
  - Địa chỉ: `ShippingAddress`, `CustomerProvince`, `CustomerDistrict`, `ProvinceCode`, `ProvinceName`, `WardCode`, `WardName`, `AddressDetail`, `FullAddress`, `Note`.
  - Tài chính: `SubtotalAmount`, `DiscountAmount`, `ShippingFee`, `TotalAmount`, `VoucherCode`.
  - Vận chuyển: `ShippingDistanceKm`, `ShippingDurationMinutes`, `ShippingProvider`, `ShippingFormulaSnapshot`.
  - Thanh toán: `PaymentMethod`, `PaymentStatus`, `TransferContent`, `PaidAt`, `PaymentExpireAt`, `PaymentUrl`, `PaymentTransactionId`.
  - Trạng thái: `Status` kiểu enum `OrderStatus`.
- **PK**: `Id`.
- **FK**: `UserId -> Users.Id` nullable, cho phép đơn hàng khách vãng lai.
- **Chức năng**: lưu đơn hàng tổng, trạng thái xử lý, thanh toán và vận chuyển.

#### `OrderDetails`

- **Trường chính**: `OrderId`, `ProductId`, `Quantity`, `UnitPrice`, `ProductName`, `ProductImage`, `Warranty`, `TotalPrice`.
- **PK**: `Id`.
- **FK**:
  - `OrderId -> Orders.Id`.
  - `ProductId -> Products.Id`.
- **Chức năng**: snapshot từng sản phẩm trong đơn hàng. Có lưu `ProductName`, `ProductImage`, `Warranty` tại thời điểm đặt để phục vụ lịch sử đơn.

### 7.11 Chi tiết bảng bảo hành

#### `Warranties`

- **Trường chính**: `ProductId`, `Coverage`.
- **FK**: `ProductId -> Products.Id`.
- **Chức năng**: lưu thông tin phạm vi bảo hành sản phẩm.

#### `WarrantyRequests`

- **Trường chính**: `UserId`, `ProductId`, `IssueDescription`, `Status`.
- **FK**:
  - `UserId -> Users.Id`.
  - `ProductId -> Products.Id`.
- **Chức năng**: người dùng gửi yêu cầu bảo hành; admin cập nhật trạng thái.

### 7.12 Chi tiết bảng build PC

#### `BuildPcConfigs`

- **Trường chính**: `UserId`, `Name`, `TotalPrice`.
- **FK**: `UserId -> Users.Id`.
- **Chức năng trong model**: lưu cấu hình PC theo user.
- **Ghi nhận từ controller**: `BuildPcController` hiện dùng session `buildpc_selected` cho thao tác chọn linh kiện, chưa thấy action lưu cấu hình xuống `BuildPcConfigs`.

#### `BuildPcItems`

- **Trường chính**: `BuildPcConfigId`, `ComponentType`, `ProductId`.
- **FK**:
  - `BuildPcConfigId -> BuildPcConfigs.Id`.
  - `ProductId -> Products.Id`.
- **Chức năng trong model**: lưu từng linh kiện trong cấu hình PC.

### 7.13 Chi tiết bảng nội dung và phản hồi

#### `Articles`

- **Trường chính**: `Title`, `Slug`, `Type`, `Content`.
- **Chức năng**: lưu bài viết tin công nghệ/khuyến mãi, có CRUD cho Admin/Staff và xóa cho Admin.

#### `Feedbacks`

- **Trường chính**: `Name`, `Email`, `Message`, `IsProcessed`.
- **Chức năng**: lưu liên hệ của khách hàng; Admin/Staff có màn hình quản lý.

### 7.14 Chi tiết bảng cấu hình hệ thống/vận chuyển

#### `SiteSettings`

- **Trường chính**: `SiteName`, `LogoUrl`, `DealSectionBackgroundUrl`, `HotPromotionBackgroundUrl`.
- **Chức năng**: cấu hình nhận diện website và hình nền các section.

#### `ShippingConfigs`

- **Trường chính**: `BaseDistanceKm`, `BaseFee`, `ExtraFeePerKm`, `MaxDistanceKm`, `FreeShippingDistanceKm`, `IsActive`.
- **Chức năng**: cấu hình công thức tính phí vận chuyển nội bộ.

#### `ShopLocations`

- **Trường chính**: `ShopName`, `Address`, `Latitude`, `Longitude`, `IsDefault`.
- **Chức năng**: lưu tọa độ cửa hàng để tính khoảng cách/route.

---

## 8. Chức năng người dùng

### 8.1 Đăng ký

- **Controller/View**: `AccountController.Register`, `Views/Account/Register.cshtml`.
- **Mục đích**: tạo tài khoản khách hàng mới.
- **Luồng xử lý**:
  1. Người dùng nhập họ tên, email, mật khẩu, điện thoại, địa chỉ.
  2. Controller kiểm tra `ModelState`.
  3. Kiểm tra email đã tồn tại trong bảng `Users`.
  4. Lấy role `Customer` từ bảng `Roles`.
  5. Tạo username từ phần trước dấu `@`; nếu trùng thì thêm timestamp.
  6. Hash mật khẩu qua `AuthService.HashPassword`.
  7. Lưu user vào database.
  8. Redirect sang trang đăng nhập.
- **Lợi ích**: khách hàng có tài khoản để quản lý hồ sơ, đơn hàng và giỏ hàng database.

### 8.2 Đăng nhập

- **Controller/View**: `AccountController.Login`, `Views/Account/Login.cshtml`.
- **Mục đích**: xác thực người dùng và tạo phiên đăng nhập.
- **Luồng xử lý**:
  1. Người dùng nhập email và mật khẩu.
  2. `AuthService.ValidateUserAsync` tìm user theo email, include role.
  3. Kiểm tra hash mật khẩu. Service hỗ trợ cả hash Identity (`AQAAAA...`) và SHA256 + salt demo.
  4. Kiểm tra `IsActive` để từ chối tài khoản bị khóa.
  5. Tạo claims: user id, name, email, username, role.
  6. Gọi `SignInAsync` với cookie scheme `PcStoreCookie`.
  7. Gộp giỏ hàng session vào giỏ hàng database bằng `CartService.MergeGuestCartAsync`.
- **Lợi ích**: phân quyền theo role và giữ lại dữ liệu giỏ hàng khách sau khi đăng nhập.

### 8.3 Đăng xuất

- **Controller/View**: `AccountController.Logout`, form trong layout.
- **Mục đích**: kết thúc phiên đăng nhập.
- **Luồng xử lý**:
  1. Người dùng bấm đăng xuất.
  2. Controller gọi `SignOutAsync(AuthSchemes.PcStoreCookie)`.
  3. Redirect về trang chủ.
- **Lợi ích**: bảo vệ tài khoản khi dùng chung thiết bị.

### 8.4 Hồ sơ cá nhân

- **Controller/View**: `AccountController.Profile`, `UpdateProfile`, `ChangePassword`, `Views/Account/Profile.cshtml`.
- **Mục đích**: xem và cập nhật thông tin cá nhân, đổi mật khẩu.
- **Luồng cập nhật hồ sơ**:
  1. Controller lấy user id từ claim `NameIdentifier`.
  2. Load user từ database.
  3. Kiểm tra số điện thoại theo regex Việt Nam.
  4. Kiểm tra email mới không trùng user khác.
  5. Cập nhật họ tên, số điện thoại, email, địa chỉ.
  6. Lưu database và refresh auth claims.
- **Luồng đổi mật khẩu**:
  1. Kiểm tra model hợp lệ.
  2. Xác minh mật khẩu hiện tại bằng `AuthService.VerifyPassword`.
  3. Hash mật khẩu mới bằng `AuthService.HashPassword`.
  4. Lưu database và refresh auth claims.
- **Lợi ích**: người dùng tự quản lý thông tin liên hệ và bảo mật tài khoản.

### 8.5 Quên mật khẩu

- **Controller/View**: `ForgotPassword`, `VerifyResetCode`, `Views/Account/ForgotPassword.cshtml`, `Views/Account/VerifyResetCode.cshtml`.
- **Mục đích**: đặt lại mật khẩu bằng OTP email.
- **Luồng xử lý**:
  1. Người dùng nhập email.
  2. Nếu email tồn tại, hệ thống đánh dấu các OTP cũ chưa dùng thành đã dùng.
  3. Sinh OTP 6 chữ số bằng `RandomNumberGenerator`.
  4. Lưu hash OTP vào `PasswordResetOtps`, hạn dùng 10 phút.
  5. Gửi email qua `IEmailSender`/`SmtpEmailSender`.
  6. Người dùng nhập email, OTP và mật khẩu mới.
  7. Controller kiểm tra OTP tồn tại, chưa dùng, chưa hết hạn.
  8. `AccountPasswordResetService` tạo token bảo vệ bằng Data Protection và reset password bằng `PasswordHasher<User>`.
  9. Đánh dấu OTP đã dùng.
- **Lợi ích**: hỗ trợ khôi phục tài khoản mà không cần admin can thiệp.

### 8.6 Danh mục

- **Controller/View**: layout dùng `DbContext.Categories`, `ProductsController.Index`, `AdminCategoriesController`.
- **Mục đích**: phân loại sản phẩm và hỗ trợ lọc.
- **Luồng xử lý người dùng**:
  1. Layout đọc danh sách category từ database.
  2. Người dùng chọn danh mục hoặc category slug.
  3. `ProductsController.Index` lọc sản phẩm theo `CategoryId`.
- **Lợi ích**: giúp khách hàng tìm sản phẩm theo nhóm nhanh hơn.

### 8.7 Sản phẩm và tìm kiếm

- **Controller/View**: `ProductsController.Index`, `ProductsController.Detail`, `Views/Products/Index.cshtml`, `Views/Products/Detail.cshtml`.
- **Mục đích**: xem danh sách, lọc, tìm kiếm và xem chi tiết sản phẩm.
- **Luồng danh sách**:
  1. Nhận `ProductFilterVm` gồm keyword, category, brand, min price, max price.
  2. Tạo query từ `Products.Include(Category)`.
  3. Áp dụng điều kiện keyword, category slug/category id, brand, khoảng giá.
  4. Load categories và products kèm images.
  5. Trả về view.
- **Luồng chi tiết**:
  1. Tìm product theo id, include category và product images theo sort order.
  2. Nếu không có thì trả `NotFound`.
  3. Lấy tối đa 3 gợi ý nâng cấp cùng category, giá cao hơn.
  4. Trả về view chi tiết.
- **Lợi ích**: hỗ trợ khách hàng đánh giá sản phẩm và tìm lựa chọn phù hợp.

### 8.8 So sánh sản phẩm

- **Controller/View/Service**: `CompareController`, `CompareSessionService`, `Views/Compare/Index.cshtml`, partial `_CompareButton`, `_CompareTray`.
- **Mục đích**: so sánh thông số giữa tối đa 2 sản phẩm.
- **Luồng xử lý**:
  1. Người dùng bấm thêm sản phẩm vào so sánh.
  2. Controller kiểm tra sản phẩm tồn tại và active.
  3. Service kiểm tra danh sách session, giới hạn tối đa 2 sản phẩm.
  4. Trang compare load sản phẩm từ session ids.
  5. Controller parse thông số kỹ thuật thành các nhóm CPU, RAM, GPU, SSD, Mainboard, PSU, Case, Tản nhiệt.
  6. Hiển thị bảng so sánh.
- **Lợi ích**: hỗ trợ quyết định mua hàng khi khách phân vân giữa hai cấu hình.

### 8.9 Giỏ hàng

- **Controller/View/Service**: `CartController`, `CartService`, `Views/Cart/Index.cshtml`.
- **Mục đích**: lưu danh sách sản phẩm khách muốn mua.
- **Luồng xử lý**:
  1. Người dùng thêm sản phẩm vào giỏ từ danh sách/chi tiết/build PC.
  2. `CartService.AddToCartAsync` kiểm tra sản phẩm active và tồn kho.
  3. Nếu đã đăng nhập: lưu vào `Carts`/`CartItems` theo `UserId`.
  4. Nếu chưa đăng nhập: lưu vào session key `guest_cart`.
  5. Người dùng có thể cập nhật số lượng, xóa từng dòng, xóa toàn bộ.
  6. Khi đăng nhập, `MergeGuestCartAsync` gộp giỏ session vào giỏ database.
- **Lợi ích**: hỗ trợ mua hàng cho cả khách vãng lai và thành viên.

### 8.10 Đơn hàng và checkout

- **Controller/View**: `OrdersController`, `Views/Orders/Checkout.cshtml`, `Success`, `Detail`, `MyOrders`, `TrackingLookup`, `BankTransfer`, `Quotation`.
- **Mục đích**: tạo đơn hàng, theo dõi đơn và xem lịch sử.
- **Luồng checkout**:
  1. GET `/Checkout` tự điền tên/email/phone nếu người dùng đã đăng nhập.
  2. View hiển thị giỏ hàng và form địa chỉ/thanh toán.
  3. POST `/Checkout` kiểm tra họ tên, số điện thoại, email, địa chỉ, phí vận chuyển và phương thức thanh toán.
  4. Lấy giỏ hàng bằng `CartService`.
  5. Bắt đầu transaction database.
  6. Kiểm tra từng sản phẩm còn active và đủ tồn kho.
  7. Tạo danh sách `OrderDetail` với snapshot tên, ảnh, bảo hành, giá.
  8. Tạo `Order` với địa chỉ, phí ship, tổng tiền, payment method/status.
  9. Trừ tồn kho sản phẩm.
  10. Lưu đơn hàng, cập nhật nội dung chuyển khoản nếu cần.
  11. Clear cart nếu không phải chuyển khoản.
  12. Commit transaction.
- **Lợi ích**: đảm bảo đơn hàng và tồn kho được xử lý nhất quán.

### 8.11 Thanh toán

- **Phương thức phát hiện được**:
  - `COD`
  - `BANK_TRANSFER`
- **Luồng COD**:
  1. Đơn được tạo với `PaymentStatus = UNPAID`.
  2. `Status = PendingConfirmation`.
  3. Giỏ hàng được clear sau khi đặt thành công.
- **Luồng chuyển khoản**:
  1. Đơn được tạo với `PaymentStatus = WAITING_PAYMENT`.
  2. `Status = PendingPayment`.
  3. `PaymentExpireAt = DateTime.UtcNow + 2 giờ`.
  4. `TransferContent = DH{id}` sau khi có mã đơn.
  5. Người dùng xem trang `BankTransfer` và xác nhận đã chuyển.
  6. Trạng thái chuyển sang `PendingConfirmation` và `WAITING_CONFIRMATION`.
  7. Admin xác nhận thì `PaymentStatus = PAID`, `Status = Processing`, `PaidAt = DateTime.UtcNow`.
- **Lợi ích**: mô phỏng quy trình thanh toán thực tế của cửa hàng có chuyển khoản thủ công.

### 8.12 Theo dõi đơn hàng và báo giá

- **Theo dõi đơn**:
  - Người dùng đăng nhập xem `MyOrders` và `Detail`.
  - Khách vãng lai có thể tra cứu bằng mã đơn và số điện thoại qua `/Order/Lookup`.
  - `Tracking` kiểm tra session `LastOrderId` hoặc số điện thoại để bảo vệ truy cập đơn khách.
- **Báo giá**:
  - `Quotation` tạo view model từ order và order details.
  - Có view `Views/Orders/Quotation.cshtml` và CSS `wwwroot/css/quotation.css`.
- **Lợi ích**: người dùng có thể theo dõi trạng thái và lấy thông tin đơn để in/copy báo giá.

### 8.13 Build PC

- **Controller/View/Service**: `BuildPcController`, `BuildCompatibilityService`, `Views/BuildPc/Index.cshtml`, `wwwroot/js/buildpc.js`.
- **Mục đích**: hỗ trợ chọn linh kiện tạo cấu hình PC.
- **Luồng xử lý**:
  1. Trang build PC hiển thị các nhóm linh kiện theo `ComponentOrder`.
  2. API `GET /buildpc/products` lọc sản phẩm active, còn hàng theo loại linh kiện.
  3. Người dùng chọn sản phẩm cho từng loại; controller lưu vào session `buildpc_selected`.
  4. `BuildCompatibilityService` trả về cảnh báo kiểm tra socket CPU/mainboard, chuẩn RAM/mainboard hoặc công suất PSU.
  5. Có thể xóa linh kiện, reset cấu hình.
  6. Có thể thêm toàn bộ cấu hình vào giỏ hàng.
  7. Có thể xuất CSV cấu hình.
- **Lợi ích**: tăng giá trị tư vấn kỹ thuật cho website PC Store.

### 8.14 Bảo hành

- **Controller/View**: `WarrantyController`, `Views/Warranty/Index.cshtml`.
- **Mục đích**: người dùng đã đăng nhập gửi yêu cầu bảo hành.
- **Luồng xử lý**:
  1. Controller yêu cầu `[Authorize]`.
  2. Người dùng nhập product id và mô tả lỗi.
  3. Controller tạo `WarrantyRequest` với `UserId`, `ProductId`, `IssueDescription`.
  4. Trạng thái mặc định trong model là `Mới tạo`.
  5. Admin cập nhật trạng thái trong `AdminWarrantyController`.
- **Lợi ích**: hỗ trợ hậu mãi sau bán hàng.

### 8.15 Bài viết và liên hệ

- **Bài viết**:
  - `ArticlesController.Index` hiển thị danh sách bài viết.
  - `Detail(slug)` xem chi tiết.
  - Admin/Staff tạo/sửa; Admin xóa.
- **Liên hệ**:
  - `ContactController.Index(GET/POST)` cho khách gửi phản hồi.
  - `ContactController.Manage` cho Admin/Staff xem phản hồi.
- **Lợi ích**: hỗ trợ nội dung marketing, tin công nghệ và kênh phản hồi khách hàng.

---

## 9. Chức năng quản trị

Tất cả controller tên `Admin*` trong source đều dùng `[Authorize(Roles = "Admin")]`, ngoại trừ bài viết/liên hệ có thêm quyền `Staff` cho một số thao tác.

### 9.1 Dashboard

- **Controller/View**: `AdminDashboardController`, `Views/AdminDashboard/Index.cshtml`.
- **Mục đích**: thống kê nhanh dữ liệu.
- **Dữ liệu thống kê**:
  - Số sản phẩm.
  - Số đơn hàng.
  - Số người dùng.
  - Số yêu cầu bảo hành.
- **Lợi ích**: admin nắm tổng quan hoạt động hệ thống.

### 9.2 Quản lý sản phẩm

- **Controller/View**: `AdminProductsController`, `Views/AdminProducts/*`.
- **Chức năng**:
  - Danh sách sản phẩm, lọc theo keyword và category.
  - Tạo sản phẩm.
  - Sửa sản phẩm.
  - Xóa sản phẩm.
  - Quản lý thumbnail, nhiều ảnh, thứ tự ảnh, ảnh chính.
  - Quản lý giá, giá giảm, khuyến mãi, tồn kho, bảo hành, mô tả, thông số.
- **Luồng tạo/sửa chính**:
  1. Load categories vào view model.
  2. Validate URL ảnh thumbnail và gallery.
  3. Tạo/cập nhật product.
  4. Serialize component specs bằng `ProductComponentSpecHelper`.
  5. Thêm/xóa/sắp xếp `ProductImages`.
  6. Đảm bảo ảnh đầu tiên là ảnh chính.
- **Lợi ích**: admin quản lý dữ liệu bán hàng cốt lõi.

### 9.3 Quản lý danh mục

- **Controller/View**: `AdminCategoriesController`, `Views/AdminCategories/*`.
- **Chức năng**:
  - Xem danh sách category.
  - Tạo category.
  - Sửa category.
  - Xóa category.
- **Ràng buộc**: khi xóa category, controller kiểm tra còn sản phẩm thuộc danh mục thì không cho xóa.
- **Lợi ích**: tổ chức catalog sản phẩm rõ ràng.

### 9.4 Quản lý đơn hàng

- **Controller/View**: `AdminOrdersController`, `Views/AdminOrders/*`.
- **Chức năng**:
  - Xem danh sách đơn hàng kèm user.
  - Xem chi tiết đơn hàng kèm order details và product.
  - Cập nhật trạng thái đơn.
  - Tự đánh dấu đơn chuyển khoản hết hạn nếu quá thời gian.
  - Xác nhận chuyển khoản ngân hàng.
- **Luồng xác nhận chuyển khoản**:
  1. Kiểm tra order tồn tại.
  2. Kiểm tra `PaymentMethod == BANK_TRANSFER` và `PaymentStatus == WAITING_CONFIRMATION`.
  3. Chuyển `PaymentStatus` sang `PAID`.
  4. Chuyển `Status` sang `Processing`.
  5. Ghi `PaidAt`.
  6. Xóa cart items của user nếu có.
- **Lợi ích**: quản lý vận hành đơn hàng và thanh toán thủ công.

### 9.5 Quản lý người dùng

- **Controller/View**: `AdminUsersController`, `Views/AdminUsers/*`.
- **Chức năng**:
  - Danh sách user, tìm theo họ tên/email/username.
  - Tạo user mới.
  - Sửa user.
  - Đổi role.
  - Khóa/mở khóa tài khoản bằng `IsActive`.
  - Xóa user với ràng buộc không cho admin tự xóa chính mình.
- **Lợi ích**: phân quyền và kiểm soát tài khoản trong hệ thống.

### 9.6 Quản lý banner

- **Controller/View**: `AdminBannersController`, `Views/AdminBanners/*`.
- **Chức năng**:
  - Danh sách banner theo position và sort order.
  - Tạo/sửa banner.
  - Bật/tắt trạng thái.
  - Xóa banner.
- **Lợi ích**: thay đổi nội dung quảng bá trên giao diện mà không sửa code.

### 9.7 Quản lý cài đặt site

- **Controller/View**: `AdminSettingsController`, `Views/AdminSettings/Index.cshtml`.
- **Chức năng**:
  - Cập nhật `SiteName`.
  - Cập nhật `LogoUrl`.
  - Cập nhật `DealSectionBackgroundUrl`.
  - Cập nhật `HotPromotionBackgroundUrl`.
- **Lợi ích**: tùy chỉnh nhận diện website từ giao diện admin.

### 9.8 Quản lý bảo hành

- **Controller/View**: `AdminWarrantyController`, `Views/AdminWarranty/Index.cshtml`.
- **Chức năng**:
  - Xem danh sách yêu cầu bảo hành kèm user và product.
  - Cập nhật trạng thái yêu cầu.
- **Lợi ích**: hỗ trợ quy trình hậu mãi.

### 9.9 Quản lý bài viết và phản hồi

- **Bài viết**:
  - Admin/Staff tạo và sửa bài viết.
  - Admin xóa bài viết.
- **Phản hồi liên hệ**:
  - Admin/Staff xem danh sách feedback.
- **Lợi ích**: hỗ trợ marketing và chăm sóc khách hàng.

---

## 10. API và tích hợp

### 10.1 API nội bộ `/api/shipping`

`ShippingController` là `ControllerBase` có route `api/shipping` và các endpoint:

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/shipping/provinces` | Lấy danh sách tỉnh/thành từ GHN address service. |
| GET | `/api/shipping/districts?provinceId=...` | Lấy quận/huyện theo province id. |
| GET | `/api/shipping/wards?districtId=...` | Lấy phường/xã theo district id. |
| POST | `/api/shipping/calculate` | Tính phí vận chuyển từ payload địa chỉ và giỏ hàng. |

Luồng tính phí:

1. Validate payload gồm province id, district id, ward code, address detail.
2. Lấy giỏ hàng guest bằng `CartService.GetCartAsync(null)`.
3. Tính số lượng, trọng lượng và kích thước giả lập từ cart.
4. Gọi `IShippingService.CalculateAsync`.
5. Trả JSON gồm `shippingFee`, `isFreeShipping`, `feeSource`, `currency`, `message`, thông tin GHN và công thức.

### 10.2 GHN Address API

- **Service**: `GhnAddressService`.
- **Cấu hình**: section `GHN` trong `appsettings.json` gồm `BaseUrl`, `Token`, `ShopId`.
- **Vai trò**: lấy province/district/ward để form checkout có dữ liệu địa chỉ.

### 10.3 GHN Shipping API

- **Service**: `GhnShippingService`.
- **Vai trò**: tính phí giao hàng ngoài phạm vi hoặc theo chính sách shipping.
- **Cấu hình HttpClient**: base address lấy từ `GHN.BaseUrl`, timeout 10 giây.

### 10.4 OpenRouteService

- **Service**: `OpenRouteServiceProvider`, `GeocodingService`, `RouteService`.
- **Cấu hình**: section `OpenRouteService.ApiKey`.
- **Vai trò**: geocode địa chỉ cửa hàng và hỗ trợ route/khoảng cách cho vận chuyển.

### 10.5 SMTP Email

- **Service**: `SmtpEmailSender`.
- **Cấu hình**: section `EmailSettings` gồm SMTP server, port, sender, username, password, SSL.
- **Vai trò**: gửi OTP đặt lại mật khẩu.

### 10.6 Export CSV build PC

- **Endpoint**: `GET /buildpc/export-csv`.
- **Vai trò**: xuất cấu hình linh kiện từ session thành file CSV text.
- **Ghi chú**: chức năng trả response dạng file CSV cho trình duyệt, nhưng không tạo file nhị phân trong repository.

---

## 11. Bảo mật

### 11.1 Authentication

- Hệ thống sử dụng cookie authentication với scheme `PcStoreCookie`.
- Login path: `/Account/Login`.
- Access denied path: `/Account/AccessDenied`.
- Sau đăng nhập, user có claims gồm id, name, email, username và role.

### 11.2 Authorization

- Admin controllers sử dụng `[Authorize(Roles = "Admin")]`.
- Bảo hành người dùng yêu cầu `[Authorize]`.
- Trang hồ sơ, đổi mật khẩu, lịch sử đơn hàng, chi tiết đơn hàng user yêu cầu `[Authorize]`.
- Bài viết và contact manage cho phép `Admin,Staff` ở một số action.

### 11.3 Anti-forgery

Nhiều POST action dùng `[ValidateAntiForgeryToken]`, ví dụ:

- Forgot password.
- Verify reset code.
- Logout.
- Update profile.
- Change password.
- Checkout.
- Confirm transferred.
- Các form admin như product/banner/user.

### 11.4 Mật khẩu

- `AuthService.HashPassword` hiện dùng SHA256 + salt cố định `pcstore-demo-salt`, comment ghi là demo học tập.
- `AuthService.VerifyPassword` và `ValidateUserAsync` hỗ trợ kiểm tra hash ASP.NET Identity bắt đầu bằng `AQAAAA`.
- `AccountPasswordResetService.ResetPasswordAsync` lưu mật khẩu mới bằng `PasswordHasher<User>`.
- `IdentityOptions` cấu hình mật khẩu tối thiểu 6 ký tự, không bắt buộc chữ hoa/chữ thường/số/ký tự đặc biệt.

### 11.5 OTP quên mật khẩu

- OTP sinh bằng `RandomNumberGenerator`.
- OTP lưu dạng hash SHA256 theo email, code và purpose string.
- OTP có hạn 10 phút.
- OTP cũ chưa dùng bị đánh dấu đã dùng trước khi tạo OTP mới.
- Thông báo quên mật khẩu không tiết lộ email có tồn tại hay không.

### 11.6 Kiểm soát truy cập đơn hàng

- Đơn của user đăng nhập được kiểm tra theo `UserId`.
- Đơn khách vãng lai dùng session `LastOrderId` hoặc tra cứu bằng mã đơn và số điện thoại.
- `PayNow` kiểm tra user, admin hoặc session trước khi cho thanh toán lại.

### 11.7 Validate dữ liệu

- Checkout validate họ tên, số điện thoại, email, địa chỉ, phí ship và phương thức thanh toán.
- Admin product validate URL ảnh phải là HTTP/HTTPS.
- Admin user kiểm tra trùng email/username.
- Product/cart/order kiểm tra active và tồn kho.

### 11.8 Điểm cần cải thiện về bảo mật phát hiện từ source

Các điểm này là nhận xét trực tiếp từ code hiện có:

1. `appsettings.json` đang chứa giá trị API key/token cấu hình GHN/OpenRouteService. Nên chuyển sang secret manager/environment variables khi triển khai thật.
2. `AuthService.HashPassword` dùng SHA256 + salt cố định cho tài khoản tạo bởi đăng ký/admin và seed admin. Nên chuyển toàn bộ sang `PasswordHasher<User>` hoặc ASP.NET Identity đầy đủ.
3. Cookie authentication chưa thấy cấu hình chi tiết như thời hạn cookie, sliding expiration, secure policy, same-site trong `Program.cs`.
4. Login POST trong `AccountController` không thấy `[ValidateAntiForgeryToken]` ở source hiện tại, trong khi nhiều POST khác có dùng.

---

## 12. Hiệu năng

### 12.1 Các điểm hỗ trợ hiệu năng đã có

1. **Asynchronous EF Core**: đa số action dùng `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`.
2. **Include có chọn lọc**: các trang cần category/images/order details dùng `Include` rõ ràng.
3. **Index quan trọng**: email/username/product code/slug/source url/product image sort/OTP được cấu hình index.
4. **Memory cache được đăng ký**: `AddMemoryCache()` có trong `Program.cs`. Một số service có thể tận dụng cache nếu triển khai.
5. **HTTP client timeout**: OpenRouteService timeout 8 giây; GHN timeout 10 giây.
6. **Transaction khi checkout**: tạo đơn và trừ tồn kho nằm trong transaction để tránh trạng thái dở dang.
7. **Session cho dữ liệu tạm**: giỏ hàng khách, compare, build PC không bắt buộc ghi database ngay.

### 12.2 Các điểm có thể ảnh hưởng hiệu năng

1. `ProductsController.Index` load toàn bộ sản phẩm phù hợp filter, chưa thấy phân trang.
2. `BuildPcController.QueryProductsByType` load tất cả sản phẩm active/in stock rồi lọc loại linh kiện trong memory.
3. `CompareController` parse specifications trong controller khi hiển thị.
4. Layout truy vấn categories và site settings mỗi request.
5. `Program.cs` chạy một số SQL kiểm tra/cập nhật schema khi khởi động ứng dụng.

### 12.3 Đề xuất cải thiện hiệu năng dựa trên source

1. Thêm phân trang cho danh sách sản phẩm/admin products/admin orders.
2. Đưa lọc `ComponentType` của build PC xuống SQL nhiều hơn nếu dữ liệu chuẩn hóa tốt.
3. Cache categories/site settings dùng `IMemoryCache` đã được đăng ký.
4. Chuẩn hóa thông số kỹ thuật thành bảng hoặc JSON được parse trước nếu compare/build PC mở rộng.
5. Tách migration/schema repair khỏi runtime startup trong môi trường production.

---

## 13. Ưu điểm

1. **Cấu trúc MVC rõ ràng**: Controllers, Views, Models, ViewModels, Services tách thư mục rõ.
2. **Chức năng thương mại điện tử tương đối đầy đủ**: sản phẩm, danh mục, tìm kiếm/lọc, giỏ hàng, checkout, đơn hàng, thanh toán, admin.
3. **Hỗ trợ khách vãng lai và thành viên**: giỏ session cho khách, giỏ database cho user, gộp giỏ khi đăng nhập.
4. **Có phân quyền quản trị**: Admin controllers được bảo vệ bằng role.
5. **Có quên mật khẩu bằng OTP email**: OTP hash, hết hạn, đánh dấu đã dùng.
6. **Có tích hợp vận chuyển thực tế**: GHN address/shipping, OpenRouteService, cấu hình chính sách ship.
7. **Có tính năng đặc thù PC Store**: build PC, cảnh báo tương thích, so sánh thông số.
8. **Có quản lý hậu mãi**: yêu cầu bảo hành và quản trị trạng thái.
9. **Có seed dữ liệu ban đầu**: roles, admin, categories, banners, products, site settings, articles.
10. **Có audit fields**: `CreatedAt`, `UpdatedAt` tự động cập nhật cho entity kế thừa `BaseEntity`.

---

## 14. Hạn chế

Các hạn chế dưới đây là những điểm nhìn thấy từ source code:

1. **Chưa dùng ASP.NET Identity đầy đủ**: database dùng `Users`/`Roles` tự định nghĩa thay vì hệ `AspNetUsers`; password hashing chưa thống nhất.
2. **SHA256 + salt cố định còn tồn tại**: `AuthService.HashPassword` comment là demo học tập, không nên dùng cho production.
3. **Chưa thấy phân trang** cho danh sách sản phẩm, đơn hàng, admin products.
4. **Một số logic schema repair nằm trong startup**: `Program.cs` chạy nhiều SQL kiểm tra/thêm cột/bảng khi app khởi động, phù hợp demo nhưng production nên quản lý bằng migration chuẩn.
5. **Build PC model có bảng lưu cấu hình nhưng controller hiện dùng session**: chưa thấy chức năng lưu cấu hình PC lâu dài vào `BuildPcConfigs`/`BuildPcItems`.
6. **Thanh toán chuyển khoản là xác nhận thủ công**: có cột `PaymentUrl` và `PaymentTransactionId`, nhưng chưa thấy tích hợp cổng thanh toán tự động trong controller.
7. **Secrets nằm trong appsettings**: API key/token GHN/OpenRouteService có trong cấu hình source.
8. **Cart API shipping hiện lấy `GetCartAsync(null)`** trong `ShippingController.Calculate`, tức tính trên giỏ session guest; nếu checkout user đăng nhập cần kiểm tra thêm phía client/server để đảm bảo đồng bộ user cart.
9. **Một số controller xử lý nhiều trách nhiệm**: ví dụ `OrdersController` chứa validate, tạo order, thanh toán, tracking, quotation trong cùng controller.

---

## 15. Hướng phát triển

Các hướng phát triển phù hợp với code hiện có:

1. **Chuẩn hóa bảo mật tài khoản**
   - Chuyển toàn bộ mật khẩu sang `PasswordHasher<User>`.
   - Hoặc tích hợp ASP.NET Identity đầy đủ với user/role manager.
   - Bổ sung anti-forgery cho login POST.

2. **Bảo mật cấu hình**
   - Di chuyển API key/token/password SMTP sang environment variables hoặc user secrets.
   - Không commit secret thật vào repository.

3. **Tối ưu danh sách và tìm kiếm**
   - Thêm phân trang.
   - Thêm sắp xếp theo giá, tên, mới nhất, khuyến mãi.
   - Thêm full-text search hoặc filter theo thông số.

4. **Hoàn thiện build PC**
   - Lưu cấu hình vào `BuildPcConfigs` và `BuildPcItems` cho user đăng nhập.
   - Kiểm tra tương thích thật dựa trên `CpuSocket`, `RamType`, công suất PSU.
   - Cho phép chia sẻ cấu hình bằng link.

5. **Nâng cấp thanh toán**
   - Tích hợp cổng thanh toán thật sử dụng các trường `PaymentUrl`, `PaymentTransactionId` đã có.
   - Tự động callback cập nhật payment status.

6. **Tối ưu vận chuyển**
   - Cache province/district/ward từ GHN.
   - Lưu log phí ship đã tính.
   - Tách rõ phí nội bộ và phí GHN.

7. **Quản trị nâng cao**
   - Thêm quản lý voucher/khuyến mãi riêng thay vì trường promotion trong product.
   - Thêm báo cáo doanh thu theo ngày/tháng.
   - Thêm export đơn hàng dạng CSV.

8. **Kiểm thử**
   - Bổ sung unit test cho `CartService`, `AuthService`, `ShippingFeeCalculator`, `OrdersController` checkout.
   - Bổ sung integration test cho các luồng đăng ký/đăng nhập/đặt hàng.

---

## 16. Kết luận

Dựa trên source code đã phân tích, PC Store/KKSHOP là một hệ thống web bán PC, laptop và linh kiện được xây dựng bằng ASP.NET Core MVC, Razor Views, Entity Framework Core và SQL Server. Hệ thống có đầy đủ các module chính của một website thương mại điện tử: tài khoản, sản phẩm, danh mục, tìm kiếm/lọc, giỏ hàng, đặt hàng, thanh toán COD/chuyển khoản, theo dõi đơn, admin quản lý, bảo hành, bài viết, banner và cấu hình website.

Điểm nổi bật của dự án là có các chức năng phù hợp đặc thù cửa hàng PC như build PC, cảnh báo tương thích linh kiện và so sánh sản phẩm theo thông số. Ngoài ra, hệ thống có tích hợp vận chuyển qua GHN/OpenRouteService và gửi email OTP qua SMTP, giúp đồ án có tính thực tế cao hơn.

Các hạn chế chính nằm ở bảo mật mật khẩu chưa thống nhất, secrets còn nằm trong appsettings, thiếu phân trang, một phần schema repair chạy lúc startup và thanh toán chuyển khoản chưa tự động hóa qua gateway. Đây là các hướng cải thiện rõ ràng nếu phát triển tiếp sau đồ án.

---

## 17. Phụ lục: file source code đã phân tích

### 17.1 Cấu hình dự án và startup

- `Datn.PcStore.csproj`
- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`
- `Constants/AuthSchemes.cs`

### 17.2 Database và dữ liệu mẫu

- `Data/ApplicationDbContext.cs`
- `Data/SeedData.cs`
- `Models/BaseEntity.cs`
- `Models/Entities.cs`
- `Migrations/202605240001_AddPaymentExpireAtToOrders.cs`
- `Migrations/202605240002_AddProductPromotionFields.cs`
- `Migrations/202605240003_RepairProductPromotionColumns.cs`
- `Migrations/202605240004_AddPaymentGatewayFieldsToOrders.cs`
- `Migrations/202606030001_AddPasswordResetOtp.cs`

### 17.3 Controllers đã phân tích

- `Controllers/AccountController.cs`
- `Controllers/AdminBannersController.cs`
- `Controllers/AdminCategoriesController.cs`
- `Controllers/AdminDashboardController.cs`
- `Controllers/AdminOrdersController.cs`
- `Controllers/AdminProductsController.cs`
- `Controllers/AdminSettingsController.cs`
- `Controllers/AdminUsersController.cs`
- `Controllers/AdminWarrantyController.cs`
- `Controllers/ArticlesController.cs`
- `Controllers/BuildPcController.cs`
- `Controllers/CartController.cs`
- `Controllers/CompareController.cs`
- `Controllers/ContactController.cs`
- `Controllers/HomeController.cs`
- `Controllers/OrdersController.cs`
- `Controllers/ProductsController.cs`
- `Controllers/ShippingController.cs`
- `Controllers/WarrantyController.cs`

### 17.4 Services đã phân tích

- `Services/AccountPasswordResetService.cs`
- `Services/AuthService.cs`
- `Services/BuildCompatibilityService.cs`
- `Services/CartService.cs`
- `Services/CompareSessionService.cs`
- `Services/EmailSettings.cs`
- `Services/GeocodingService.cs`
- `Services/GhnAddressModels.cs`
- `Services/GhnAddressService.cs`
- `Services/GhnShippingService.cs`
- `Services/OpenRouteServiceProvider.cs`
- `Services/ProductImageStorageService.cs`
- `Services/RouteService.cs`
- `Services/ShippingFeeCalculator.cs`
- `Services/ShippingModels.cs`
- `Services/ShippingPolicyOptions.cs`
- `Services/ShippingService.cs`
- `Services/SmtpEmailSender.cs`
- Các interface `I*.cs` trong thư mục `Services`.

### 17.5 ViewModels đã phân tích

- `ViewModels/AccountViewModels.cs`
- `ViewModels/AdminDashboardVm.cs`
- `ViewModels/AdminProductUpsertVm.cs`
- `ViewModels/AdminSiteSettingsVm.cs`
- `ViewModels/AdminUserViewModels.cs`
- `ViewModels/BuildPcVm.cs`
- `ViewModels/CartVm.cs`
- `ViewModels/CheckoutVm.cs`
- `ViewModels/CompareViewModels.cs`
- `ViewModels/HomeIndexVm.cs`
- `ViewModels/ProductFilterVm.cs`
- `ViewModels/QuotationViewModels.cs`

### 17.6 Views và static assets đã phân tích

- `Views/Account/*`
- `Views/AdminBanners/*`
- `Views/AdminCategories/*`
- `Views/AdminDashboard/*`
- `Views/AdminOrders/*`
- `Views/AdminProducts/*`
- `Views/AdminSettings/*`
- `Views/AdminUsers/*`
- `Views/AdminWarranty/*`
- `Views/Articles/*`
- `Views/BuildPc/*`
- `Views/Cart/*`
- `Views/Compare/*`
- `Views/Contact/*`
- `Views/Home/*`
- `Views/Orders/*`
- `Views/Products/*`
- `Views/Shared/*`
- `Views/Warranty/*`
- `wwwroot/css/*.css`
- `wwwroot/js/*.js`

### 17.7 Công nghệ phát hiện được

- ASP.NET Core MVC trên .NET 8.
- Razor Views.
- HTML/CSS/JavaScript.
- Bootstrap 5.3.3.
- Bootstrap Icons 1.11.3.
- Swiper 11.
- html2canvas 1.4.1.
- jQuery Validate / Unobtrusive Validation.
- SQL Server.
- Entity Framework Core 8.0.8.
- Thành phần ASP.NET Identity: `IdentityOptions`, `PasswordHasher<User>`, `IdentityResult`, `IdentityError`.
- Cookie Authentication.
- Session.
- MemoryCache.
- HttpClientFactory.
- GHN API.
- OpenRouteService.
- SMTP email.

### 17.8 Cấu trúc database phát hiện được

Các bảng theo DbSet/model:

1. `Roles`
2. `Users`
3. `PasswordResetOtps`
4. `Categories`
5. `Products`
6. `ProductImages`
7. `Banners`
8. `Carts`
9. `CartItems`
10. `Orders`
11. `OrderDetails`
12. `Warranties`
13. `WarrantyRequests`
14. `BuildPcConfigs`
15. `BuildPcItems`
16. `Articles`
17. `Feedbacks`
18. `SiteSettings`
19. `ShippingConfigs`
20. `ShopLocations`

Các quan hệ chính:

- `Roles 1-n Users`
- `Users 1-n PasswordResetOtps`
- `Categories 1-n Products`
- `Categories 1-n Categories` qua `ParentCategoryId`
- `Products 1-n ProductImages`
- `Users 1-n Carts`
- `Carts 1-n CartItems`
- `Products 1-n CartItems`
- `Users 1-n Orders` nullable
- `Orders 1-n OrderDetails`
- `Products 1-n OrderDetails`
- `Products 1-n Warranties`
- `Users 1-n WarrantyRequests`
- `Products 1-n WarrantyRequests`
- `Users 1-n BuildPcConfigs`
- `BuildPcConfigs 1-n BuildPcItems`
- `Products 1-n BuildPcItems`

### 17.9 Đường dẫn file báo cáo

File Markdown được tạo tại:

```text
Docs/SystemAnalysisReport.md
```
