# GIAI_THICH_CHI_TIET_DU_AN

> Tài liệu này được viết dựa trên **source code thực tế** trong repo `DATN` (ASP.NET Core MVC).
> Đối tượng đọc: người mới học, chưa rành code, cần dùng để thuyết trình.

---

## 1) Tổng quan dự án

### Website này là gì?
Đây là website bán linh kiện máy tính/PC store (có sản phẩm, giỏ hàng, đặt hàng), kèm khu vực quản trị (admin), build cấu hình PC, quản lý bảo hành, bài viết, banner.

**File/thư mục chứng minh:**
- `Controllers/ProductsController.cs` (xem sản phẩm)
- `Controllers/CartController.cs` (giỏ hàng)
- `Controllers/OrdersController.cs` (checkout, theo dõi đơn)
- `Controllers/AdminProductsController.cs`, `Controllers/AdminOrdersController.cs` (admin)
- `Controllers/BuildPcController.cs` (Build PC)
- `Controllers/WarrantyController.cs`, `Controllers/AdminWarrantyController.cs` (bảo hành)
- `Controllers/ArticlesController.cs`, `Controllers/AdminBannersController.cs` (nội dung/banners)

### Dùng để làm gì?
- Cho khách xem sản phẩm, lọc/tìm kiếm, xem chi tiết.
- Thêm vào giỏ hàng, cập nhật giỏ, checkout tạo đơn.
- Theo dõi đơn hàng.
- Quản trị sản phẩm, danh mục, đơn hàng, người dùng, bảo hành, banner, cài đặt site.
- Build cấu hình PC theo linh kiện, kèm cảnh báo tham khảo khi chọn cấu hình.

### Có những loại người dùng nào?
1. **Guest** (chưa đăng nhập).
2. **Customer** (đăng nhập role Customer).
3. **Admin** (role Admin).

### Mỗi loại người dùng làm được gì?
- **Guest:** xem sản phẩm, thêm giỏ (session), checkout như khách, tra cứu đơn bằng mã + SĐT.
- **Customer:** như Guest + xem đơn của mình (`MyOrders`), gửi bảo hành, lưu Build PC.
- **Admin:** quản trị toàn bộ module admin (được bảo vệ bằng `[Authorize(Roles="Admin")]`).

---

## 2) Công nghệ sử dụng

### Ngôn ngữ lập trình
- **C#** cho backend ASP.NET Core.
- Razor `.cshtml` cho view frontend server-rendered.

### Backend dùng gì?
- **ASP.NET Core MVC** (`AddControllersWithViews`).
- Routing mặc định controller/action.

### Frontend dùng gì?
- Razor Views + HTML/CSS/JS trong `Views/` và `wwwroot/`.

### Database dùng gì?
- **SQL Server** qua Entity Framework Core (`UseSqlServer`).

### Có dùng Entity Framework Core không?
- **Có.** Dùng `ApplicationDbContext`, `DbSet<>`, LINQ, migrations.

### Có dùng JWT hay Cookie Authentication không?
- **Cookie Authentication** (không thấy JWT).

### Có dùng MVC không?
- **Có** đầy đủ `Models`, `Views`, `Controllers`.

### Có dùng service, controller, model, view, migration, seed data không?
- **Service:** `Services/AuthService.cs`, `Services/CartService.cs`, `Services/ProductImageStorageService.cs`.
- **Controller:** thư mục `Controllers/`.
- **Model:** `Models/Entities.cs`.
- **View:** thư mục `Views/`.
- **Migration:** thư mục `Migrations/`.
- **Seed data:** `Data/SeedData.cs` được gọi ở `Program.cs`.

---

## 3) Kiến trúc website (giải thích đơn giản)

Luồng tổng quát đúng mô hình MVC + Service:

1. Người dùng bấm trên giao diện (ví dụ nút “Thêm giỏ hàng”) ở View `.cshtml`.
2. View gửi request (form POST hoặc AJAX) tới route controller.
3. Controller nhận request, validate dữ liệu.
4. Controller gọi Service xử lý nghiệp vụ (ví dụ `CartService`).
5. Service dùng `ApplicationDbContext` để đọc/ghi DB.
6. SQL Server trả dữ liệu lại.
7. Controller trả View (HTML) hoặc JSON.
8. Trình duyệt hiển thị kết quả cho người dùng.

Ví dụ nhanh:
- `Views/Products/Detail.cshtml` gửi add-to-cart → `CartController.Add` → `CartService.AddToCartAsync` → bảng `CartItems` (hoặc Session guest) → trả redirect/JSON.

---

## 4) Cấu trúc thư mục source code

### `Controllers/`
- Chứa lớp nhận request HTTP, điều hướng luồng.
- File quan trọng:
  - `AccountController.cs`: đăng ký/đăng nhập/đăng xuất.
  - `ProductsController.cs`: danh sách + chi tiết sản phẩm.
  - `CartController.cs`: add/update/remove/clear giỏ.
  - `OrdersController.cs`: checkout, tracking, đơn của tôi.
  - `AdminProductsController.cs`: CRUD sản phẩm admin.
  - `AdminOrdersController.cs`: quản lý đơn + trạng thái.
  - `BuildPcController.cs`: chọn linh kiện + tương thích + lưu cấu hình.
  - `WarrantyController.cs`: khách gửi bảo hành.

### `Models/`
- Chứa entity ánh xạ bảng DB: `User`, `Role`, `Product`, `Order`, `Cart`...
- File chính: `Models/Entities.cs`.

### `Views/`
- Chứa giao diện Razor.
- Ví dụ:
  - `Views/Home/Index.cshtml`: trang chủ.
  - `Views/Products/Index.cshtml`, `Detail.cshtml`: sản phẩm.
  - `Views/Cart/Index.cshtml`: giỏ hàng.
  - `Views/Orders/Checkout.cshtml`: checkout.
  - `Views/Account/Login.cshtml`, `Register.cshtml`.
  - Các thư mục `Views/Admin*`: khu vực admin.

### `Services/`
- Chứa business logic dùng lại nhiều nơi.
- `CartService.cs`: logic giỏ guest/user, merge cart.
- `AuthService.cs`: hash/validate password.

### `Data/`
- Chứa `ApplicationDbContext.cs` và `SeedData.cs`.
- `DbContext` khai báo `DbSet<>`, quan hệ, precision.

### `Migrations/`
- Lịch sử thay đổi schema DB theo thời gian.

### `ViewModels/`
- Model dành cho màn hình/form cụ thể, tách khỏi entity DB.

### `wwwroot/`
- Static files: CSS, JS, images, uploads.

### `Program.cs`
- Điểm vào app: cấu hình DI, DB, auth cookie, session, middleware, route, seed data.

---

## 5) Giải thích frontend

- Giao diện code bằng Razor (`.cshtml`) + CSS + JavaScript.
- Trang chính:
  - Layout chung: `Views/Shared/_Layout.cshtml`
  - Trang chủ: `Views/Home/Index.cshtml`
  - Sản phẩm: `Views/Products/Index.cshtml`
  - Chi tiết sản phẩm: `Views/Products/Detail.cshtml`
  - Giỏ hàng: `Views/Cart/Index.cshtml`
  - Đăng nhập/đăng ký: `Views/Account/Login.cshtml`, `Register.cshtml`
  - Admin: các `Views/Admin.../*`
- JS/CSS:
  - CSS: `wwwroot/css/*`
  - JS: `wwwroot/js/*`
- Frontend gọi backend bằng:
  - **Form submit** (đa số).
  - **AJAX** cho thêm giỏ trong vài trường hợp (`CartController.Add` có check `X-Requested-With` / `Accept: application/json`).

---

## 6) Giải thích backend

### Khái niệm
- **Controller:** “lễ tân” nhận request và trả response.
- **Service:** nơi để logic nghiệp vụ tái sử dụng.
- **Model/Entity:** lớp đại diện dữ liệu DB.
- **DbContext:** cầu nối code ↔ database.
- **Middleware:** chuỗi xử lý request (`UseStaticFiles`, `UseRouting`, `UseSession`, `UseAuthentication`, `UseAuthorization`).
- **Route:** map URL tới action theo mẫu `{controller=Home}/{action=Index}/{id?}`.

### Controller theo chức năng
- Đăng nhập/đăng ký: `AccountController`.
- Sản phẩm: `ProductsController`.
- Giỏ hàng: `CartController`.
- Đơn hàng: `OrdersController`.
- Admin: `AdminProductsController`, `AdminOrdersController`, `AdminUsersController`, `AdminDashboardController`, `AdminSettingsController`, `AdminCategoriesController`, `AdminBannersController`, `AdminWarrantyController`.
- Build PC: `BuildPcController`.
- Bảo hành: `WarrantyController`.

---

## 7) Giải thích database (bảng/model chính)

> Nguồn chính: `Models/Entities.cs` + `Data/ApplicationDbContext.cs`.

- **Users** (`User`): thông tin tài khoản, role, trạng thái active.
- **Roles** (`Role`): phân quyền Admin/Customer.
- **Products** (`Product`): tên, giá, tồn kho, mô tả, loại linh kiện, socket/ram type.
- **Categories** (`Category`): danh mục cha-con.
- **ProductImages** (`ProductImage`): ảnh phụ của sản phẩm.
- **Carts** (`Cart`): giỏ của user đăng nhập.
- **CartItems** (`CartItem`): dòng sản phẩm trong giỏ.
- **Orders** (`Order`): đơn hàng (người nhận, địa chỉ, trạng thái, tổng tiền...).
- **OrderDetails** (`OrderDetail`): chi tiết từng sản phẩm trong đơn.
- **Warranties** (`Warranty`): thông tin bảo hành sản phẩm.
- **WarrantyRequests** (`WarrantyRequest`): yêu cầu bảo hành do user gửi.
- **BuildPcConfigs** (`BuildPcConfig`): cấu hình PC lưu bởi user.
- **BuildPcItems** (`BuildPcItem`): linh kiện trong 1 cấu hình.
- **Articles** (`Article`): bài viết.
- **Banners** (`Banner`): banner hiển thị.
- **SiteSettings** (`SiteSetting`): cấu hình tên site/logo/background.

Quan hệ tiêu biểu:
- Role 1-n User.
- Category cha-con tự tham chiếu.
- Product 1-n ProductImage.
- Cart 1-n CartItem.
- Order 1-n OrderDetail.
- BuildPcConfig 1-n BuildPcItem.

---

## 8) Luồng nghiệp vụ chi tiết

> Format: thao tác UI → View → route/action → Controller → Service → DB → kết quả + ví dụ.

### 8.1 Đăng ký
- UI: nhập họ tên/email/mật khẩu/điện thoại/địa chỉ.
- View: `Views/Account/Register.cshtml`.
- Route: `POST /Account/Register`.
- Controller: `AccountController.Register(RegisterVm)`.
- Service: `AuthService.HashPassword`.
- DB: đọc `Users`, `Roles`; ghi `Users`.
- Kết quả: tạo tài khoản Customer, redirect Login.
- Ví dụ: email đã tồn tại => báo lỗi ModelState.

### 8.2 Đăng nhập
- View: `Views/Account/Login.cshtml`.
- Route: `POST /Account/Login`.
- Controller: `AccountController.Login`.
- Service: `AuthService.ValidateUserAsync`, `CartService.MergeGuestCartAsync`.
- DB: đọc `Users`, ghi/đọc `Carts`, `CartItems` khi merge.
- Kết quả: tạo cookie đăng nhập, chuyển Home.

### 8.3 Đăng xuất
- Route: `POST /Account/Logout`.
- Controller: `AccountController.Logout`.
- Kết quả: xóa cookie auth, về Home.

### 8.4 Xem danh sách sản phẩm
- View: `Views/Products/Index.cshtml`.
- Route: `GET /Products/Index`.
- Controller: `ProductsController.Index(ProductFilterVm)`.
- Service: Chưa thấy service riêng (query trực tiếp DbContext).
- DB: đọc `Products`, `Categories`, `ProductImages`.

### 8.5 Tìm kiếm/lọc sản phẩm
- Controller `ProductsController.Index` lọc theo `Keyword`, `CategoryId`, `Brand`, `MinPrice`, `MaxPrice`.

### 8.6 Xem chi tiết sản phẩm
- View: `Views/Products/Detail.cshtml`.
- Route: `GET /Products/Detail/{id}`.
- Controller: `ProductsController.Detail`.
- DB: đọc `Products`, `Category`, `ProductImages`; gợi ý nâng cấp cùng category.

### 8.7 Thêm vào giỏ
- View: thường từ product list/detail.
- Route: `POST /Cart/Add`.
- Controller: `CartController.Add`.
- Service: `CartService.AddToCartAsync`.
- DB: user login ghi `Carts/CartItems`; guest ghi `Session` key `guest_cart`.
- Kết quả: redirect hoặc JSON success.

### 8.8 Cập nhật số lượng giỏ
- Route: `POST /Cart/Update`.
- Controller: `CartController.Update`.
- Service: `CartService.UpdateQuantityAsync`.

### 8.9 Xóa sản phẩm khỏi giỏ
- Route: `POST /Cart/Remove`.
- Controller: `CartController.Remove`.
- Service: `CartService.RemoveItemAsync`.

### 8.10 Đặt hàng/checkout
- View: `Views/Orders/Checkout.cshtml`.
- Route: `GET/POST /Checkout`.
- Controller: `OrdersController.Checkout`.
- Service: dùng `CartService.GetCartAsync`, `ClearCartAsync`.
- DB ghi: `Orders`, `OrderDetails`; cập nhật trừ `Products.StockQuantity`; xóa giỏ.
- Có transaction: `BeginTransactionAsync`.
- Kết quả: thành công chuyển `Orders/Success`.

### 8.11 Theo dõi đơn
- Route: `GET /Order/Tracking/{id}`, `GET/POST /Order/Lookup`.
- Controller: `OrdersController.Tracking`, `TrackingLookup`.
- DB: đọc `Orders`, `OrderDetails`.

### 8.12 Admin thêm/sửa/xóa sản phẩm
- View: `Views/AdminProducts/Create.cshtml`, `Edit.cshtml`, `Index.cshtml`.
- Controller: `AdminProductsController`.
- DB: `Products`, `ProductImages`.
- Upload ảnh: **theo URL** (validate http/https), chưa thấy upload file binary trực tiếp trong luồng này.

### 8.13 Admin quản lý đơn + cập nhật trạng thái
- View: `Views/AdminOrders/Index.cshtml`, `Detail.cshtml`.
- Controller: `AdminOrdersController.Index/Detail/UpdateStatus`.
- DB: `Orders`, `OrderDetails`.

### 8.14 Bảo hành
- User: `WarrantyController.Index/Create`.
- Admin: `AdminWarrantyController` (xem/xử lý danh sách yêu cầu).
- DB: `WarrantyRequests`, liên quan `Products`, `Users`.

### 8.15 Build PC
- View: `Views/BuildPc/Index.cshtml`.
- Controller: `BuildPcController`.
- Tương thích cơ bản:
  - CPU socket phải khớp Mainboard socket.
  - RAM type phải khớp Mainboard ram type.
- Có lưu config (`BuildPcConfigs/BuildPcItems`) và thêm cả cấu hình vào giỏ (`AddConfigToCart`).

### 8.16 Quản lý bài viết/banner/site settings
- Bài viết: `ArticlesController` + `Views/Articles/*`.
- Banner: `AdminBannersController` + `Views/AdminBanners/*`.
- Site settings: `AdminSettingsController` + `Views/AdminSettings/Index.cshtml`.

Nếu một chi tiết nhỏ nào đó bạn muốn chắc chắn (ví dụ chính xác từng field của form nào): hãy mở đúng view tương ứng để đọc trực tiếp.

---

## 9) Authentication & Authorization

- Đăng nhập lưu bằng **Cookie Auth Scheme** `PcStoreCookie`.
- Cấu hình trong `Program.cs` qua `.AddAuthentication().AddCookie(...)`.
- Claim role được set lúc login: `ClaimTypes.Role`.
- Bảo vệ quyền:
  - `[Authorize]` dùng cho trang cần đăng nhập (vd BuildPc, Warranty, MyOrders).
  - `[Authorize(Roles = "Admin")]` cho trang admin.
- Trang không đủ quyền chuyển tới `/Account/AccessDenied`.
- **JWT: Chưa thấy trong source code.**

---

## 10) Giải thích giỏ hàng

- Guest cart lưu trong **Session** (`guest_cart`) dạng JSON (`CartService`).
- User login lưu DB bảng `Carts` + `CartItems`.
- Khi đăng nhập có merge guest cart vào DB (`MergeGuestCartAsync`).
- Vì sao cần session?
  - Guest chưa có userId, cần nơi tạm giữ giỏ theo phiên trình duyệt.

Ví dụ:
- Bạn chưa login, thêm 2 món → tắt trang mở lại vẫn còn (nếu session còn).
- Login xong, hệ thống đẩy món guest vào giỏ DB để đồng bộ tài khoản.

---

## 11) Giải thích checkout

Khi đặt hàng:
1. Đọc giỏ hiện tại.
2. Validate thông tin nhận hàng.
3. Duyệt từng item, check tồn kho.
4. Tạo `Order` + danh sách `OrderDetail`.
5. Trừ `Product.StockQuantity`, cập nhật `IsInStock`.
6. Lưu DB, clear giỏ, commit transaction.

- Có tạo Order? **Có**.
- Có tạo OrderDetail? **Có**.
- Có trừ tồn kho? **Có**.
- Có xóa giỏ? **Có**.
- Có transaction? **Có** (`BeginTransactionAsync`).

Vì sao transaction quan trọng?
- Đảm bảo “tất cả cùng thành công hoặc cùng rollback”. Tránh lỗi kiểu đã trừ kho nhưng chưa tạo đơn.

---

## 12) Build PC (nếu có)

- Build PC = chức năng chọn linh kiện theo nhóm (CPU/Mainboard/RAM/SSD/GPU/PSU/Case).
- Chọn linh kiện trên trang Build PC, hệ thống tính tổng tiền.
- Cảnh báo tham khảo về socket CPU/Mainboard và chuẩn RAM/Mainboard; chưa thay thế kiểm tra kỹ thuật chuyên sâu.
- Dữ liệu lưu ở `BuildPcConfigs` và `BuildPcItems`.
- Có thể thêm toàn bộ cấu hình vào giỏ (`AddConfigToCart`).

---

## 13) Bảo hành (nếu có)

- User gửi yêu cầu bảo hành tại trang `Warranty/Index` (chọn sản phẩm + mô tả lỗi).
- Lưu vào bảng `WarrantyRequests` trạng thái ban đầu “Mới tạo”.
- Admin xử lý ở module `AdminWarranty`.

---

## 14) Nội dung thuyết trình

### 14.1 Bản ngắn ~3 phút
“Đây là website bán linh kiện PC viết bằng ASP.NET Core MVC. Người dùng có thể xem sản phẩm, lọc/tìm kiếm, thêm vào giỏ, đặt hàng và theo dõi đơn. Hệ thống hỗ trợ cả khách chưa đăng nhập bằng session cart và tự động merge giỏ khi đăng nhập. Bên admin quản lý sản phẩm, đơn hàng, danh mục, người dùng, banner và cấu hình website. Ngoài ra còn có Build PC để hỗ trợ lựa chọn cấu hình, kèm cảnh báo tham khảo, và module bảo hành để gửi yêu cầu sau mua. Dữ liệu lưu SQL Server qua Entity Framework Core, authentication dùng cookie và phân quyền role Admin/Customer bằng Authorize.”

### 14.2 Bản chi tiết 7–10 phút (dàn ý)
1. Mục tiêu dự án và đối tượng người dùng.
2. Công nghệ: ASP.NET Core MVC + EF Core + SQL Server + Cookie Auth.
3. Kiến trúc MVC + Service + DbContext.
4. Luồng mua hàng: sản phẩm → giỏ → checkout → order.
5. Luồng quản trị: CRUD sản phẩm, quản lý đơn và trạng thái.
6. Chức năng mở rộng: Build PC, Warranty, Articles/Banners.
7. Điểm tốt kỹ thuật: transaction checkout, merge guest cart, role-based auth.
8. Kết luận.

### 14.3 Câu nói dễ nhớ
- Frontend là “mặt tiền cửa hàng”.
- Backend là “bộ não xử lý nghiệp vụ”.
- Database là “kho dữ liệu trung tâm”.
- MVC là “chia việc rõ ràng để dễ bảo trì”.

### 14.4 Câu hỏi giảng viên có thể hỏi + trả lời mẫu
1. **Vì sao dùng MVC?**
   - Vì tách giao diện, xử lý, dữ liệu; code sạch và dễ mở rộng.
2. **Giỏ hàng guest hoạt động sao?**
   - Lưu session JSON; login thì merge vào DB.
3. **Chống lỗi checkout thế nào?**
   - Validate dữ liệu + check tồn kho + transaction rollback khi lỗi.
4. **Phân quyền admin bằng gì?**
   - `[Authorize(Roles="Admin")]` + claim role trong cookie.
5. **Build PC hỗ trợ cảnh báo cấu hình gì?**
   - Cảnh báo tham khảo về socket CPU-mainboard và RAM type-mainboard; chưa thay thế kiểm tra kỹ thuật chuyên sâu.

---

## 15) Kết luận

Dự án là một website thương mại điện tử linh kiện PC đầy đủ nền tảng: sản phẩm, giỏ hàng, checkout, đơn hàng, tài khoản và quản trị. Điểm nổi bật là xử lý thực tế cho guest cart/login cart, transaction khi đặt hàng, và tính năng mở rộng như Build PC + bảo hành. Nếu cần học sâu hơn, nên đọc lần lượt: `Program.cs` → `ApplicationDbContext` → `Controllers` chính (`Account`, `Products`, `Cart`, `Orders`) → `Services/CartService` → các view tương ứng.

---

## Ghi chú trung thực theo source code

- JWT: **Chưa thấy trong source code**.
- Một số chức năng upload ảnh đang đi theo hướng nhập URL ảnh (HTTP/HTTPS) thay vì upload file trực tiếp trong `AdminProductsController`: nếu cần xác nhận upload file binary, **Chưa thấy trong source code** của controller này.
- Nếu bạn muốn bản tài liệu level “siêu chi tiết theo từng action từng dòng”, có thể tách thêm phụ lục theo từng controller.
