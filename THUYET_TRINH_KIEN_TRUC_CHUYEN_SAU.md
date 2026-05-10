# TÀI LIỆU PHÂN TÍCH CHUYÊN SÂU WEBSITE DATN.PCSTORE (PHỤC VỤ THUYẾT TRÌNH BẢO VỆ)

> Mục tiêu tài liệu: không chỉ trả lời “dùng công nghệ gì” mà trả lời được “**vì sao thiết kế như vậy**”, “**trade-off là gì**”, “**nếu đổi công nghệ thì được/mất gì**”.

---

## 1. GIỚI THIỆU TỔNG QUAN HỆ THỐNG

Đây là một hệ thống **e-commerce monolith** cho bán linh kiện/PC, có 2 vùng người dùng rõ ràng:

- **Khách hàng (Customer/Guest)**: xem sản phẩm, thêm giỏ hàng, checkout, theo dõi đơn.
- **Quản trị (Admin)**: CRUD danh mục/sản phẩm/banner/người dùng/đơn hàng/cấu hình ship.

### Bài toán nghiệp vụ hệ thống giải quyết

1. Quản lý catalog sản phẩm có phân cấp danh mục, giá bán/giảm giá, tồn kho.
2. Hỗ trợ mua hàng cả cho guest và user đã đăng nhập.
3. Đồng bộ giỏ hàng guest → user khi đăng nhập (merge cart).
4. Đặt hàng có tính ship theo địa chỉ + chính sách nội bộ + fallback external.
5. Theo dõi vòng đời đơn hàng.

### Vì sao mô hình này phù hợp đồ án

- **Business**: mô hình gần sát một shop thật (catalog, cart, order, admin backoffice).
- **Kỹ thuật**: vừa đủ độ sâu để trình bày nhiều chủ đề chuẩn enterprise mini (MVC, DI, ORM, Auth, transaction, external API).
- **Vận hành**: dễ triển khai một process/web app duy nhất cho quy mô đồ án, giảm overhead microservice.

### Vì sao e-commerce cần kiến trúc kiểu này

E-commerce không chỉ là trang hiển thị; nó là hệ thống có:

- **state phức tạp** (cart, session, auth, order status),
- **tính nhất quán dữ liệu cao** (tồn kho, thanh toán, đơn hàng),
- **nhiều luồng nghiệp vụ đồng thời** (user/admin/external shipping API).

Vì vậy cần tách lớp controller-service-data để tránh “fat controller” và kiểm soát rủi ro khi hệ thống phình to.

---

## 2. KIẾN TRÚC TỔNG THỂ HỆ THỐNG

### Text diagram

```text
Browser (Razor SSR + JS/AJAX)
   -> ASP.NET Core Middleware Pipeline
      -> Routing
         -> Controller Action (MVC)
            -> Service Layer (Auth/Cart/Shipping/...)
               -> ApplicationDbContext (EF Core)
                  -> SQL Server
            -> ViewModel mapping
         -> Razor View render (HTML)
   <- Response HTML/JSON

External:
- GHN API (shipping fee, address)
- OpenRoute provider (map/route)
```

### Vai trò từng layer

- **Controller**: nhận request, validate input level HTTP, orchestration.
- **Service**: chứa business logic tái sử dụng (cart merge, shipping strategy).
- **DbContext**: unit-of-work + tracking + query mapping database.
- **ViewModel**: contract riêng cho UI.

### Vì sao phải tách layer

Nếu dồn hết vào controller:
- controller thành God-class, khó test, khó reuse.
- logic bị copy ở nhiều action, dễ lệch behavior.
- khó thay external provider (GHN/OpenRoute).

Service layer đang giúp dự án:
- gom logic giỏ hàng vào `CartService` (DB + session + merge),
- gom logic shipping policy vào `ShippingService`,
- gom hashing/validate user vào `AuthService`.

### Middleware pipeline là gì

Trong `Program.cs`: request đi qua `UseStaticFiles -> UseRouting -> UseSession -> UseAuthentication -> UseAuthorization` rồi mới vào endpoint MVC.

Đây là “đường ống xử lý HTTP”. Thứ tự quan trọng: nếu gọi `UseAuthentication` sai vị trí, `User` claims trong controller sẽ rỗng.

### DI là gì và vì sao dùng

Dependency Injection được dùng xuyên suốt qua constructor injection cho controller/service. Lợi ích:
- loose coupling,
- dễ thay implementation,
- lifecycle rõ (`Scoped` cho service gắn request).

---

## 3. PHÂN TÍCH FRONTEND CHI TIẾT

Frontend là **Razor SSR + Bootstrap + JS nhẹ (AJAX cho cart/shipping)**.

### SSR và Razor hoạt động ra sao

- Action trả `View(model)`.
- Razor engine render HTML phía server.
- Browser nhận HTML hoàn chỉnh, tăng khả năng SEO/indexing.

### Vì sao chọn Razor thay React/Vue (cho đồ án này)

**Lý do hợp lý cho scope đồ án**:
- team tập trung backend nghiệp vụ và dữ liệu.
- SSR giảm complexity state client.
- 1 codebase thống nhất C# end-to-end.

**Trade-off**:
- Ưu: tốc độ dựng MVP nhanh, SEO tốt, ít build pipeline JS phức tạp.
- Nhược: UX dynamic kém SPA, page reload nhiều hơn.

#### Nếu giảng viên hỏi “Vì sao không React?”

Trả lời gợi ý:
> Với phạm vi đồ án và mục tiêu chứng minh năng lực kiến trúc backend + nghiệp vụ e-commerce, Razor SSR cho tốc độ phát triển và độ ổn định cao hơn. React mạnh khi UI realtime/state phức tạp phía client; còn dự án này trọng tâm là transaction checkout, auth, dữ liệu và admin CRUD nên SSR là lựa chọn tối ưu trade-off.

### Layout / Partial / Form / Validation / ViewModel

- `_Layout.cshtml` là shell dùng chung, render topbar/admin shell theo route.
- Partial `_GlobalNotifications`, `_ProductCard` giúp tái sử dụng UI.
- Form submit dùng anti-forgery cho POST quan trọng.
- ViewModel (`RegisterVm`, `LoginVm`, `CheckoutVm`) tách contract UI khỏi Entity.

### Vì sao không đẩy Entity trực tiếp ra View

- tránh over-posting (user submit field không mong muốn).
- tránh lộ field nhạy cảm (VD `PasswordHash`, cờ nội bộ).
- giữ ổn định UI contract khi DB schema đổi.

---

## 4. PHÂN TÍCH BACKEND CHI TIẾT

### ASP.NET Core MVC trong dự án

- Routing map theo convention trong `Program.cs`.
- Controller chứa action GET/POST.
- `[Authorize]` / `[Authorize(Roles="Admin")]` bảo vệ tài nguyên.

### Authentication + Authorization flow

- Login: validate user qua `IAuthService`.
- Tạo `ClaimsIdentity` (NameIdentifier, Email, Role...).
- `SignInAsync` ghi auth cookie.
- Request sau middleware auth đọc cookie => dựng `User` principal.

### Cookie Auth vs JWT

- **Cookie** phù hợp app web SSR truyền thống: browser tự gửi cookie, tích hợp tốt MVC.
- **JWT** phù hợp API stateless/mobile/multi-client.

#### Nếu hỏi “JWT và Cookie khác nhau thế nào? Vì sao không JWT?”

- Cookie: server-oriented web session auth, dễ dùng với Razor + anti-forgery.
- JWT: self-contained token, tiện cho SPA/mobile, nhưng cần xử lý refresh/revoke/XSS lưu token cẩn thận.
- Dự án hiện tại là MVC SSR, không phải public API-first platform → cookie là trade-off hợp lý.

### Session

Session được bật `AddSession/UseSession`, dùng cho:
- guest cart,
- `LastOrderId` để gate tracking/success.

---

## 5. PHÂN TÍCH DATABASE & EF CORE

### SQL Server + EF Core + ORM

- SQL Server làm persistent store quan hệ.
- EF Core mapping entity class <-> table.
- `ApplicationDbContext` chứa `DbSet<T>` cho aggregate chính.

### Mapping, migration, LINQ

- `OnModelCreating` cấu hình index unique (`Email`, `Username`, `Slug`, `ProductCode`), precision tiền tệ, relationship.
- LINQ translate SQL giúp query an toàn tham số hóa.
- Startup có `db.Database.Migrate()` + patch SQL runtime cho cột mới.

### Entity quan trọng và quan hệ

- `User` - `Role` (many-to-one).
- `Category` self-reference parent/children.
- `Product` - `ProductImage` (one-to-many).
- `Cart` - `CartItem`.
- `Order` - `OrderDetail`.

### Vì sao checkout cần transaction

Checkout ghi nhiều thứ cùng lúc: tạo order + order details + trừ stock + clear cart.
Nếu fail giữa chừng mà không transaction => dữ liệu lệch (đơn tạo nhưng tồn kho chưa trừ hoặc ngược lại).

### Race condition

Hai user cùng mua sản phẩm tồn thấp có thể tranh chấp stock. Dự án đã kiểm tra stock trước/sau khi trừ trong transaction, nhưng vẫn có thể cần nâng cấp bằng optimistic concurrency token hoặc locking chiến lược khi scale lớn.

### EF Core ưu/nhược

- Ưu: năng suất cao, type-safe LINQ, migration tiện.
- Nhược: query phức tạp có thể khó tối ưu nếu thiếu profiling.
- Raw SQL tốt hơn khi: báo cáo nặng, batch operation lớn, query đặc thù cần kiểm soát execution plan.

---

## 6. LUỒNG NGHIỆP VỤ QUAN TRỌNG

### 6.1 Đăng ký

- `AccountController.Register(POST)` validate VM, kiểm tra email unique, sinh username, hash password, insert `User`.
- DB thay đổi: bảng `Users`.
- Response: redirect Login + TempData.

**Why**: tách `AuthService.HashPassword` để tập trung logic auth.

### 6.2 Đăng nhập

- Validate qua `ValidateUserAsync`.
- Tạo claims + sign-in cookie.
- Gọi `MergeGuestCartAsync`.

**Why**: giảm friction mua hàng, user không mất cart khi quyết định login.

### 6.3 Thêm giỏ hàng

- `CartController.Add` -> `CartService.AddToCartAsync`.
- User login: lưu DB (`CartItems`), guest: lưu session JSON.
- AJAX trả JSON success + cartCount.

### 6.4 Merge cart guest/user

- Loop toàn bộ session cart, add vào DB cart của user, rồi clear session.
- Ưu: đơn giản, dễ hiểu.
- Rủi ro: merge nhiều item có thể nhiều query; có thể tối ưu batch sau.

### 6.5 Checkout

- Validate dữ liệu nhận hàng + shipping fee + payment method.
- Mở transaction DB.
- Snapshot giá/tồn kho tại thời điểm đặt.
- Tạo order/details, trừ tồn kho, clear cart, commit.

### 6.6 Tracking order

- User login: chỉ xem order thuộc user.
- Guest: cần phone hoặc session `LastOrderId`.
- Đây là lớp bảo vệ tối thiểu chống lộ đơn hàng.

### 6.7 Shipping API

- `ShippingController` có endpoint provinces/districts/wards/calculate.
- `ShippingService` quyết định policy:
  1) nội khu miễn phí,
  2) GHN nếu cấu hình,
  3) fallback công thức local.

### 6.8 Admin CRUD sản phẩm

- `AdminProductsController` protected bởi role Admin.
- Quản lý thumbnail + danh sách ảnh + sort order + validate URL.

---

## 7. PHÂN TÍCH API

API chính: `/api/shipping/*`

- `GET provinces/districts/wards`: load dropdown địa chỉ.
- `POST calculate`: trả shipping fee + provider + formula snapshot.

Ví dụ response rút gọn:
```json
{ "success": true, "shippingFee": 25000, "shippingProvider": "GHN" }
```

### Vì sao shipping tách API riêng trong MVC

- checkout view cần tính phí động theo địa chỉ người dùng nhập.
- nếu full postback mỗi lần đổi tỉnh/huyện sẽ UX kém.
- AJAX/API giúp UX tốt mà vẫn giữ kiến trúc MVC SSR.

### Vì sao HttpClient + async/await

- gọi external API là I/O bound -> async giảm block thread pool.
- `AddHttpClient` cho timeout/baseaddress/pooling tốt hơn new HttpClient thủ công.

---

## 8. PHÂN TÍCH BẢO MẬT

### Đã có

- Cookie auth + claims role.
- `[Authorize]`/`[Authorize(Roles="Admin")]`.
- Anti-forgery token cho nhiều POST critical.
- EF Core LINQ giảm nguy cơ SQL Injection.

### Điểm yếu hiện tại

- Password hash SHA256 + salt tĩnh là **yếu** cho production.
  - Nên thay PBKDF2/bcrypt/Argon2 hoặc ASP.NET Identity.
- Một số endpoint POST cart chưa gắn anti-forgery đầy đủ.
- Chưa thấy security headers/csp/rate limit.

### SQLi/XSS/CSRF

- SQLi: EF tham số hóa query giúp giảm mạnh nguy cơ.
- XSS: Razor encode mặc định, nhưng vẫn phải cẩn trọng với HTML raw.
- CSRF: cần anti-forgery nhất quán cho mọi POST thay đổi trạng thái.

---

## 9. SESSION & GIỎ HÀNG

- Session lưu guest cart key `guest_cart` dạng JSON.
- Lợi ích: guest không cần account vẫn mua được.
- Hạn chế: session mất khi hết hạn/đổi browser/device.

### Concurrency issue

- Multi-tab có thể gửi update quantity chồng nhau.
- Hiện tại là last write wins.
- Có thể tăng độ an toàn bằng row version + check stale cart version.

---

## 10. SHIPPING SERVICE

- GHN: provider phí vận chuyển/địa chỉ.
- OpenRoute/Map provider: định tuyến/địa lý (qua abstraction `IMapProvider`).

### Vì sao cần service layer

Shipping là domain dễ đổi nhà cung cấp; nếu gọi GHN trực tiếp từ controller sẽ chặt coupling.
Service layer đóng vai trò anti-corruption layer.

### Retry/timeout/dependency external

- Timeout đã cấu hình trong HttpClient.
- Nên bổ sung Polly retry/circuit breaker để tăng resilience.

---

## 11. PHÂN TÍCH THƯ MỤC DỰ ÁN

- `Controllers/`: entrypoint HTTP, orchestration.
- `Services/`: business logic, integration.
- `Data/`: DbContext + seed.
- `Models/`: entity domain persistence.
- `ViewModels/`: contract UI.
- `Views/`: Razor SSR templates.
- `wwwroot/`: static assets.
- `Program.cs`: composition root (DI, middleware, routing).
- `appsettings.json`: config runtime.

**Why tách vậy**: phân ranh trách nhiệm, tăng maintainability. Không tách => độ kết dính thấp, khó mở rộng team/codebase.

---

## 12. PHÂN TÍCH KIẾN TRÚC MVC

MVC flow:
1. Request map route.
2. Controller action xử lý.
3. lấy data/service.
4. trả ViewModel.
5. Razor render HTML.

### Ưu/nhược

- Ưu: dễ học, chuẩn enterprise truyền thống, SEO tốt.
- Nhược: UI highly interactive kém linh hoạt hơn SPA.

### So sánh

- MVC vs SPA: MVC nhanh cho CRUD/SSR; SPA mạnh tương tác realtime.
- MVC vs Clean Architecture: Clean sạch hơn về dependency rule nhưng phức tạp hơn cho đồ án nhỏ.
- MVC vs Monolith API: MVC phù hợp khi server render là chính.

---

## 13. ƯU ĐIỂM / NHƯỢC ĐIỂM / HƯỚNG CẢI THIỆN

### Ưu điểm
- Luồng nghiệp vụ e-commerce khá đầy đủ.
- Có transaction checkout.
- Có phân quyền admin/customer.
- Có integration external shipping.

### Nhược điểm
- Hash password chưa production-grade.
- Chưa thấy test tự động rõ ràng.
- Chưa có CI/CD, observability sâu.
- Một số query ở view/layout có thể gây overhead.

### Hướng cải thiện
- Dùng ASP.NET Identity + secure password hasher.
- Bổ sung unit/integration tests cho checkout/cart/shipping.
- Thêm Redis distributed cache + distributed session.
- Add structured logging + tracing + health checks.

---

## 14. PERFORMANCE

- SSR: TTFB phụ thuộc server render + DB.
- EF: cần tránh N+1, dùng Include đúng chỗ.
- Indexing: đã có unique index, nhưng cần thêm index cho truy vấn order history/filter.
- Session overhead: memory session tốt cho nhỏ; scale ngang cần distributed session.
- Image upload/url: cần CDN + resize/optimization để giảm bandwidth.

---

## 15. DEPLOYMENT

Hiện source chưa thể hiện pipeline deployment hoàn chỉnh.

Đề xuất:
- **IIS**: phù hợp Windows hosting nội bộ.
- **Docker**: chuẩn hóa môi trường, tiện CI/CD.
- **Azure App Service + Azure SQL**: managed service, scale dễ.
- **VPS**: chi phí thấp, nhưng tự vận hành nhiều.

Lưu ý production:
- tách `appsettings.Production.json`,
- secrets qua environment variables/secret manager,
- không commit token thực vào repo.

---

## 16. VÌ SAO CHỌN CÔNG NGHỆ NÀY

- **ASP.NET Core MVC**: cân bằng tốc độ làm đồ án + chuẩn kiến trúc doanh nghiệp.
- **SQL Server**: mạnh transaction, tooling tốt với .NET.
- **Razor**: SSR nhanh, SEO tốt, giảm độ phức tạp frontend.
- **EF Core**: tăng năng suất CRUD + migration.
- **Cookie Auth**: fit với web SSR session-oriented.

Trade-off tổng quát: ưu tiên time-to-deliver và tính ổn định nghiệp vụ hơn là tối ưu trải nghiệm SPA realtime.

---

## 17. 30+ CÂU HỎI GIẢNG VIÊN CÓ THỂ HỎI (KÈM GỢI Ý TRẢ LỜI)

1. Vì sao chọn MVC thay vì SPA ngay từ đầu?
2. Vì sao không React?
3. Vì sao không JWT?
4. Cookie auth có rủi ro gì?
5. Vì sao cần service layer?
6. Tại sao checkout phải transaction?
7. Nếu transaction fail giữa chừng thì sao?
8. Làm sao chống overselling?
9. EF Core có thể gây vấn đề performance gì?
10. Khi nào nên dùng raw SQL?
11. Vì sao cần ViewModel?
12. Over-posting là gì?
13. Vì sao cart guest lưu session?
14. Session hết hạn thì xử lý UX thế nào?
15. Merge cart có conflict quantity thì sao?
16. Vì sao shipping tách API?
17. Nếu GHN downtime thì hệ thống xử lý sao?
18. Timeout external API chọn bao nhiêu là hợp lý?
19. Vì sao cần logging trong checkout?
20. Role-based auth triển khai thế nào?
21. Claims có lợi gì so với query DB mỗi request?
22. Tại sao không microservice?
23. Khi nào cần tách microservice?
24. Làm sao scale lên 1000 checkout đồng thời?
25. Database index hiện tại đã đủ chưa?
26. Vì sao phải anti-forgery token?
27. SHA256 password hash có ổn không?
28. Nếu đổi sang ASP.NET Identity lợi gì?
29. Làm sao thêm CI/CD cho dự án này?
30. Nếu migrate cloud thì kiến trúc đổi gì?
31. Vì sao cần abstraction `IMapProvider`?
32. Vì sao Program.cs có SQL patch runtime, lợi/hại?

> Cách trả lời mẫu khi bị hỏi vặn: luôn theo khung **(a) mục tiêu nghiệp vụ (b) ràng buộc đồ án (c) trade-off kỹ thuật (d) kế hoạch nâng cấp production**.

---

## 18. KẾT LUẬN THUYẾT TRÌNH

Hệ thống hiện tại là một nền tảng e-commerce MVC monolith được thiết kế đúng hướng cho phạm vi đồ án: đủ sâu về kiến trúc backend, có quản trị, có integration external shipping, có transaction bảo toàn dữ liệu đơn hàng. 

Điểm mạnh cốt lõi là cân bằng giữa **tính thực tế nghiệp vụ** và **độ phức tạp kỹ thuật có kiểm soát**. Khi chuyển sang production scale lớn, lộ trình nâng cấp rõ ràng gồm: tăng cường security chuẩn Identity, bổ sung test/CI-CD/observability, tối ưu hiệu năng DB/caching, và từng bước tách bounded context nếu tải tăng mạnh.

