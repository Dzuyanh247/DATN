# Văn thuyết trình bảo vệ phần mềm WEBSITE KKSHOP

## 1. Mở đầu và giới thiệu tổng quan

Kính thưa quý thầy cô, em xin trình bày phần mềm thực hành của đồ án tốt nghiệp là website KKSHOP. Đây là website bán PC và linh kiện máy tính, được xây dựng bằng ASP.NET Core MVC, Entity Framework Core và SQL Server. Trong phần bảo vệ này em không trình bày theo hướng lý thuyết, mà tập trung vào phần mềm chạy thực tế: người dùng thao tác ở View nào, request đi vào Controller nào, service nào xử lý, DbContext nào lưu dữ liệu và bảng nào trong database nhận dữ liệu.

Solution chính của em là project `Datn.PcStore.csproj`. Điểm khởi động nằm ở `Program.cs`. Tại đây ứng dụng đăng ký MVC, SignalR, Session, Authentication cookie, DbContext `ApplicationDbContext`, các service nghiệp vụ như giỏ hàng, đơn hàng, voucher, đánh giá, chat AI, chat hỗ trợ, vận chuyển GHN, upload Cloudinary và các service bản đồ. File cấu hình chính là `appsettings.json`, trong đó có chuỗi kết nối SQL Server, GHN, chính sách vận chuyển, địa chỉ shop, email SMTP, Cloudinary và AI Gemini.

Về kiến trúc, project được chia theo các nhóm thư mục rõ ràng. `Controllers` nhận request từ người dùng và admin. `Models` chứa entity ánh xạ bảng database. `ViewModels` chứa dữ liệu trung gian đưa ra View. `Services` chứa nghiệp vụ như giỏ hàng, xác thực, vận chuyển, AI, upload ảnh. `Data/ApplicationDbContext.cs` là lớp kết nối Entity Framework Core đến SQL Server. `Views` là giao diện Razor. `Hubs/ChatHub.cs` dùng SignalR cho chat realtime.

Luồng tổng quát của hệ thống là:

User / Admin
↓
Razor View trong thư mục `Views`
↓
Controller trong thư mục `Controllers`
↓
Service trong thư mục `Services` nếu chức năng có nghiệp vụ riêng
↓
`Data/ApplicationDbContext.cs`
↓
SQL Server, các bảng như `Products`, `Orders`, `OrderDetails`, `Users`, `ProductReviews`, `WarrantyRequests`
↓
Controller trả View hoặc JSON.

### Có thể bị hỏi

**Thầy hỏi: Project chạy từ file nào?**  
Em trả lời: Project khởi động từ `Program.cs`. File này đăng ký service, DbContext, authentication, session, SignalR hub và route MVC.

**Thầy hỏi: DbContext nằm ở đâu?**  
Em trả lời: DbContext nằm ở `Data/ApplicationDbContext.cs`, tên lớp là `ApplicationDbContext`, khai báo các `DbSet` như `Products`, `Orders`, `OrderDetails`, `Users`, `Banners`, `Articles`, `ProductReviews`, `WarrantyRequests`.

**Thầy hỏi: Cấu hình database nằm đâu?**  
Em trả lời: Chuỗi kết nối nằm trong `appsettings.json`, phần `ConnectionStrings`. Việc chọn connection string được xử lý qua `Data/DatabaseConfiguration.cs` và đăng ký trong `Program.cs` bằng `UseSqlServer`.

## 2. Program.cs, middleware, authentication và authorization

Ở phần khởi động, `Program.cs` đăng ký `AddControllersWithViews`, `AddSignalR`, `AddMemoryCache`, `AddDbContext<ApplicationDbContext>`, `AddAuthentication().AddCookie()`, `AddSession()` và toàn bộ service nghiệp vụ. Authentication của hệ thống dùng cookie scheme trong `Constants/AuthSchemes.cs`. Nếu người dùng chưa đăng nhập, route mặc định chuyển về `/Account/Login`. Nếu thiếu quyền, chuyển về `/Account/AccessDenied`. Với request JSON, middleware trả mã 401 hoặc 403 dạng JSON thay vì redirect.

Middleware chạy theo thứ tự: xử lý exception riêng cho chat, `UseStaticFiles`, `UseRouting`, `UseSession`, `UseAuthentication`, `UseAuthorization`, map SignalR hub `/hubs/support-chat`, map controller attribute route và route MVC mặc định. Thứ tự này quan trọng vì session phải có trước khi controller dùng giỏ hàng session, authentication phải chạy trước authorization để `[Authorize]` đọc được user và role.

Phân quyền được đặt trực tiếp bằng attribute trên controller. Ví dụ `AdminProductsController`, `AdminComponentsController`, `AdminArticlesController` yêu cầu role `Admin,Staff`. `AdminUsersController`, `AdminBannersController`, `AdminSettingsController`, `AdminSearchKeywordsController` yêu cầu `Admin`. `AdminChatController` yêu cầu `Admin,SupportStaff,CustomerSupport`. `ProductReviewsController` yêu cầu đăng nhập bằng `[Authorize]`.

### Có thể bị hỏi

**Thầy hỏi: Authentication xử lý ở file nào?**  
Em trả lời: Cấu hình authentication nằm trong `Program.cs`; luồng đăng nhập/đăng xuất nằm trong `Controllers/AccountController.cs`; kiểm tra mật khẩu nằm trong `Services/AuthService.cs`; hằng số scheme nằm trong `Constants/AuthSchemes.cs`.

**Thầy hỏi: Role kiểm tra ở đâu?**  
Em trả lời: Role được kiểm tra bằng `[Authorize(Roles = "...")]` trên các controller admin, ví dụ `Controllers/AdminProductsController.cs`, `Controllers/AdminUsersController.cs`, `Controllers/AdminOrdersController.cs`.

**Thầy hỏi: Middleware nào chạy trước?**  
Em trả lời: Sau khi build app, thứ tự chính là static files, routing, session, authentication, authorization, rồi map hub và route controller trong `Program.cs`.

## 3. Database, Models và DbContext

Em dùng Entity Framework Core với `ApplicationDbContext`. Trong `Data/ApplicationDbContext.cs`, các bảng chính gồm: `Roles`, `Users`, `Categories`, `Products`, `ComponentBrands`, `ProductImages`, `Banners`, `Carts`, `CartItems`, `Orders`, `OrderDetails`, `WarrantyRequests`, `BuildPcConfigs`, `BuildPcItems`, `Articles`, `Feedbacks`, `SiteSettings`, `ShippingConfigs`, `ShopLocations`, `PasswordResetOtps`, `ChatConversations`, `ChatMessages`, `ProductReviews`, `Vouchers`, `VoucherUsages`, `SearchKeywords`.

Các model chính nằm trong `Models/Entities.cs`. Ví dụ `Product` lưu sản phẩm và linh kiện, có các trường tên, slug, mã sản phẩm, brand, giá, giá khuyến mãi, tồn kho, ảnh, mô tả, thông số, bảo hành, loại linh kiện, socket CPU, loại RAM và category. `Order` và `OrderDetail` lưu đơn hàng và chi tiết đơn hàng. `Banner`, `Article`, `Voucher`, `SearchKeyword`, `WarrantyRequest` cũng nằm trong nhóm entity này. Đánh giá sản phẩm nằm riêng trong `Models/ProductReview.cs`. Chat hỗ trợ nằm trong `Models/SupportChat.cs` với `ChatConversation` và `ChatMessage`.

`ApplicationDbContext` override `SaveChanges` và `SaveChangesAsync` để tự động cập nhật `CreatedAt` và `UpdatedAt` cho các entity kế thừa `BaseEntity`. Trong `OnModelCreating`, hệ thống ánh xạ tên bảng, cấu hình index, quan hệ, khóa ngoại và các default value.

### Có thể bị hỏi

**Thầy hỏi: Database lưu sản phẩm ở bảng nào?**  
Em trả lời: Entity `Product` ánh xạ bảng `Products` trong `Data/ApplicationDbContext.cs`, model nằm ở `Models/Entities.cs`.

**Thầy hỏi: Đánh giá lưu ở bảng nào?**  
Em trả lời: Đánh giá lưu ở bảng `ProductReviews`, entity nằm trong `Models/ProductReview.cs`, DbSet nằm trong `Data/ApplicationDbContext.cs`.

**Thầy hỏi: Chat lưu bảng nào?**  
Em trả lời: Chat lưu ở `ChatConversations` và `ChatMessages`, model nằm trong `Models/SupportChat.cs`, DbSet nằm trong `ApplicationDbContext`.

## 4. Trang chủ, sản phẩm, tìm kiếm và gợi ý tìm kiếm

Khi người dùng vào trang chủ, route mặc định gọi `HomeController.Index`. Controller lấy banner, danh mục, sản phẩm hot sale, daily deal, promotion và các nhóm sản phẩm theo loại linh kiện. Dữ liệu đưa vào `ViewModels/HomeIndexVm.cs` và hiển thị ở `Views/Home/Index.cshtml`. Banner được lấy từ bảng `Banners`; sản phẩm lấy từ bảng `Products`; category lấy từ bảng `Categories`.

Khi người dùng xem danh sách sản phẩm, request đi đến `ProductsController.Index`. Controller nhận các tham số lọc như category, type, brand, giá, sort, keyword. Nếu có keyword tìm kiếm, controller gọi `ISearchKeywordService` triển khai bởi `Services/SearchKeywordService.cs` để lưu hoặc tăng số lần tìm kiếm trong bảng `SearchKeywords`. Kết quả hiển thị ở `Views/Products/Index.cshtml`. Khi xem chi tiết sản phẩm, `ProductsController.Detail` lấy sản phẩm, ảnh, đánh giá và phần review thông qua `IProductReviewService`, rồi trả `Views/Products/Detail.cshtml`.

Luồng tìm kiếm sản phẩm:

User nhập keyword
↓
`Views/Products/Index.cshtml` hoặc ô search ở layout
↓
`Controllers/ProductsController.cs` action `Index`
↓
`Services/SearchKeywordService.cs` ghi nhận từ khóa
↓
`Data/ApplicationDbContext.cs`
↓
Bảng `Products` và `SearchKeywords`
↓
`Views/Products/Index.cshtml` hiển thị kết quả.

### Có thể bị hỏi

**Thầy hỏi: Search xử lý ở đâu?**  
Em trả lời: Search chính nằm trong `Controllers/ProductsController.cs` action `Index`; lịch sử/gợi ý từ khóa nằm ở `Services/SearchKeywordService.cs`, `Services/SearchSuggestionService.cs` và bảng `SearchKeywords`.

**Thầy hỏi: View chi tiết sản phẩm là file nào?**  
Em trả lời: `Views/Products/Detail.cshtml`.

**Thầy hỏi: Banner trang chủ lấy ở đâu?**  
Em trả lời: Controller trang chủ là `Controllers/HomeController.cs`; dữ liệu banner nằm bảng `Banners`; quản trị banner nằm ở `Controllers/AdminBannersController.cs` và `Views/AdminBanners`.

## 5. Đăng ký, đăng nhập, hồ sơ và quên mật khẩu

Chức năng tài khoản nằm trong `Controllers/AccountController.cs`. Khi người dùng đăng ký, View `Views/Account/Register.cshtml` gửi POST đến `AccountController.Register`. Controller kiểm tra dữ liệu, dùng `IAuthService` trong `Services/AuthService.cs` để hash password, lưu user vào bảng `Users` với role phù hợp.

Khi đăng nhập, View `Views/Account/Login.cshtml` gửi POST đến `AccountController.Login`. Controller gọi `AuthService.ValidateUserAsync` để kiểm tra email/username và mật khẩu. Nếu hợp lệ, controller tạo claims gồm id, username, full name, email, role rồi gọi `HttpContext.SignInAsync` với cookie scheme. Sau đó giỏ hàng khách có thể được merge qua `CartService`.

Quên mật khẩu sử dụng OTP. View gồm `ForgotPassword.cshtml` và `VerifyResetCode.cshtml`. Controller tạo mã OTP, hash mã và lưu vào bảng `PasswordResetOtps`, gửi email bằng `Services/SmtpEmailSender.cs`. Khi người dùng nhập OTP và mật khẩu mới, `Services/AccountPasswordResetService.cs` xác thực token và đổi mật khẩu.

Luồng đăng nhập:

User nhập tài khoản
↓
`Views/Account/Login.cshtml`
↓
`Controllers/AccountController.cs` action `Login`
↓
`Services/AuthService.cs`
↓
`ApplicationDbContext.Users`
↓
Tạo cookie authentication
↓
Redirect về trang chủ hoặc returnUrl.

### Có thể bị hỏi

**Thầy hỏi: Mật khẩu lưu plain text không?**  
Em trả lời: Không. Mật khẩu được hash trong `Services/AuthService.cs`, trường lưu là `PasswordHash` của bảng `Users`.

**Thầy hỏi: OTP quên mật khẩu lưu ở đâu?**  
Em trả lời: Lưu ở bảng `PasswordResetOtps`, model trong `Models/Entities.cs`, DbSet trong `ApplicationDbContext`, xử lý ở `AccountController` và `AccountPasswordResetService`.

## 6. Giỏ hàng, đặt hàng, thanh toán, voucher và đơn hàng

Giỏ hàng được xử lý bởi `Controllers/CartController.cs` và `Services/CartService.cs`. Người dùng bấm thêm vào giỏ từ trang sản phẩm hoặc chi tiết sản phẩm, request POST đến `CartController.Add`. Controller gọi `CartService.AddToCartAsync`. Nếu đã đăng nhập, giỏ hàng lưu database qua bảng `Carts` và `CartItems`. Nếu chưa đăng nhập, service lưu giỏ hàng trong Session. View giỏ hàng là `Views/Cart/Index.cshtml`.

Đặt hàng nằm trong `Controllers/OrdersController.cs`. Người dùng vào `/Checkout`, action GET `Checkout` dựng `CheckoutVm` từ giỏ hàng. Khi nhấn đặt hàng, form POST `/Checkout` đi vào action `Checkout(CheckoutRequestVm vm)`. Controller kiểm tra thông tin nhận hàng, sản phẩm, tồn kho, voucher, phí ship, phương thức thanh toán. Sau đó tạo `Order` và `OrderDetail`, trừ tồn kho, lưu xuống `Orders` và `OrderDetails`. Nếu dùng voucher, hệ thống gọi `Services/VoucherService.cs` và ghi `VoucherUsage`.

Thanh toán trong source hiện xử lý các trạng thái như COD/chuyển khoản ngân hàng, chưa thấy tích hợp cổng VNPay/Momo/Stripe độc lập. File liên quan là `Controllers/OrdersController.cs`, `Views/Orders/BankTransfer.cshtml`, `Views/Orders/Success.cshtml` và `Services/OrderExpirationService.cs`. Đơn chuyển khoản có trạng thái chờ thanh toán, có thể xác nhận đã chuyển ở `OrdersController.ConfirmTransferred`, admin xác nhận ở `AdminOrdersController.ConfirmBankTransfer`.

Luồng đặt hàng:

User bấm Đặt hàng
↓
`Views/Orders/Checkout.cshtml`
↓
POST `/Checkout`
↓
`Controllers/OrdersController.cs` action `Checkout`
↓
`Services/CartService.cs`, `Services/VoucherService.cs`, `Services/ShippingService.cs`, `Services/OrderExpirationService.cs`
↓
`Data/ApplicationDbContext.cs`
↓
Bảng `Orders`, `OrderDetails`, `Products`, `VoucherUsages`
↓
Redirect `Orders/Success.cshtml` hoặc `Orders/BankTransfer.cshtml`.

### Có thể bị hỏi

**Thầy hỏi: Controller nào xử lý đặt hàng?**  
Em trả lời: `Controllers/OrdersController.cs`, route GET và POST là `/Checkout`.

**Thầy hỏi: Đơn hàng lưu bảng nào?**  
Em trả lời: Bảng `Orders` lưu thông tin đơn; bảng `OrderDetails` lưu từng sản phẩm trong đơn.

**Thầy hỏi: Thanh toán online qua VNPay/Momo nằm đâu?**  
Em trả lời: Trong source hiện tại em thấy xử lý COD và chuyển khoản ngân hàng trong `OrdersController`, chưa thấy service riêng cho VNPay/Momo/Stripe. Nếu được hỏi, em sẽ nói đúng là phần thanh toán hiện tại là COD/chuyển khoản và trạng thái xác nhận thanh toán.

## 7. Vận chuyển, GHN, bản đồ và phí ship

Phần vận chuyển có API riêng `Controllers/ShippingController.cs` với route `api/shipping`. API có các endpoint lấy tỉnh, huyện, xã qua GHN và tính phí ship. Service chính là `Services/ShippingService.cs`. Nếu địa chỉ trong bán kính miễn phí hoặc nội thành theo chính sách, `ShippingFeeCalculator` tính theo cấu hình `ShippingPolicy`. Nếu ngoài bán kính và bật GHN, hệ thống gọi `Services/GhnShippingService.cs` tới API GHN. Địa chỉ GHN lấy qua `Services/GhnAddressService.cs`. Cấu hình GHN nằm trong `appsettings.json` phần `GHN`.

Ngoài ra, bản đồ và tính khoảng cách dùng `OpenRouteServiceProvider`, `GeocodingService`, `RouteService`, cấu hình key nằm ở `appsettings.json` phần `OpenRouteService`. Địa chỉ shop nằm ở `ShopAddress`.

Luồng tính phí ship:

Checkout View chọn tỉnh/huyện/xã
↓
JavaScript gọi `/api/shipping/provinces`, `/districts`, `/wards`
↓
`Controllers/ShippingController.cs`
↓
`GhnAddressService`
↓
GHN API

Khi tính phí:

Checkout View gửi địa chỉ và giỏ hàng
↓
POST `/api/shipping/calculate`
↓
`ShippingController.Calculate`
↓
`ShippingService.CalculateAsync`
↓
`ShippingFeeCalculator` hoặc `GhnShippingService`
↓
Trả JSON phí ship.

### Có thể bị hỏi

**Thầy hỏi: GHN cấu hình ở đâu?**  
Em trả lời: `appsettings.json`, section `GHN`. Service gọi GHN là `GhnAddressService` và `GhnShippingService`.

**Thầy hỏi: API tính phí ship nằm đâu?**  
Em trả lời: `Controllers/ShippingController.cs`, endpoint POST `api/shipping/calculate`.

## 8. Build PC

Chức năng build PC nằm trong `Controllers/BuildPcController.cs`, route gốc là `/buildpc`, View là `Views/BuildPc/Index.cshtml`, ViewModel là `ViewModels/BuildPcVm.cs`. Người dùng chọn từng linh kiện như CPU, Mainboard, RAM, VGA, Storage, PSU, Case, Cooler. API `buildpc/products` trả danh sách sản phẩm theo loại linh kiện. Khi chọn linh kiện, POST `buildpc/select` lưu vào session. Khi xóa, POST `buildpc/remove`; reset toàn bộ là POST `buildpc/reset`; thêm cấu hình vào giỏ là POST `buildpc/add-to-cart`.

Tương thích linh kiện được kiểm tra bởi `Services/BuildCompatibilityService.cs`, hiện tập trung vào các trường như `CpuSocket` và `RamType`. Dữ liệu linh kiện nằm trong bảng `Products`, phân biệt bằng `ProductType = Component` và `ComponentType`.

Luồng build PC:

User mở `/buildpc`
↓
`Views/BuildPc/Index.cshtml`
↓
`BuildPcController.Index`
↓
`ApplicationDbContext.Products`
↓
User chọn linh kiện
↓
POST `buildpc/select`
↓
`BuildCompatibilityService.IsCompatible`
↓
Session lưu cấu hình
↓
POST `buildpc/add-to-cart`
↓
`CartService.AddToCartAsync`
↓
Giỏ hàng.

### Có thể bị hỏi

**Thầy hỏi: Build PC có lưu database không?**  
Em trả lời: Entity có `BuildPcConfigs` và `BuildPcItems` trong DbContext, nhưng luồng chọn hiện tại trong `BuildPcController` chủ yếu lưu cấu hình đang chọn vào Session và khi thêm vào giỏ thì dùng `CartService`.

**Thầy hỏi: Tương thích linh kiện kiểm tra ở đâu?**  
Em trả lời: `Services/BuildCompatibilityService.cs`.

## 9. Chat AI và chat hỗ trợ

Website có hai nhóm chat. Nhóm thứ nhất là AI tư vấn sản phẩm qua `Controllers/AiChatController.cs`, route `api/chat/ai`. Controller nhận `AiChatRequest` từ `ViewModels/Ai/AiChatModels.cs`, gọi `IAiChatService` được triển khai bởi `Services/GeminiChatService` trong file `Services/AiChatService.cs`. Service này gọi Gemini theo cấu hình `AiChat` trong `appsettings.json`, đồng thời dùng `Services/ProductSearchForAiService.cs` để lấy ngữ cảnh sản phẩm từ bảng `Products`.

Nhóm thứ hai là chat hỗ trợ khách hàng realtime. Controller khách là `Controllers/SupportChatController.cs`, controller admin là `Controllers/AdminChatController.cs`, SignalR hub là `Hubs/ChatHub.cs`, view admin là `Views/AdminChat/Index.cshtml`, partial chat khách là `Views/Shared/_SupportChatBox.cshtml`. Dữ liệu lưu ở `ChatConversations` và `ChatMessages`. `Services/SupportChatAutomationService.cs` xử lý quick action như hỏi trạng thái đơn, bảo hành, thanh toán, tư vấn PC và chuyển nhân viên.

Luồng AI:

User nhập câu hỏi
↓
JavaScript trên giao diện gửi POST `api/chat/ai`
↓
`AiChatController.Ask`
↓
`GeminiChatService.AskAsync`
↓
`ProductSearchForAiService` lấy sản phẩm liên quan
↓
Gemini API
↓
Trả JSON câu trả lời.

Luồng chat hỗ trợ:

User mở hộp chat
↓
`Views/Shared/_SupportChatBox.cshtml`
↓
POST `support-chat/conversations`
↓
`SupportChatController.CreateConversation`
↓
Bảng `ChatConversations`
↓
Gửi tin nhắn POST `support-chat/conversations/{id}/messages`
↓
Bảng `ChatMessages`
↓
SignalR `ChatHub` thông báo cho admin.

### Có thể bị hỏi

**Thầy hỏi: AI gọi service nào?**  
Em trả lời: Controller là `Controllers/AiChatController.cs`, service là `GeminiChatService` trong `Services/AiChatService.cs`, service tìm sản phẩm là `Services/ProductSearchForAiService.cs`.

**Thầy hỏi: Chat realtime dùng gì?**  
Em trả lời: Dùng SignalR, hub nằm ở `Hubs/ChatHub.cs`, map route `/hubs/support-chat` trong `Program.cs`.

## 10. Upload ảnh và Cloudinary

Upload ảnh admin có controller riêng `Controllers/AdminImageUploadsController.cs`, route `AdminImageUploads/Upload`, nhận `IFormFile`. Controller gọi `ICloudinaryImageUploadService`, triển khai bởi `Services/CloudinaryImageUploadService.cs`. Cấu hình Cloudinary nằm trong `appsettings.json` section `Cloudinary` gồm `CloudName`, `ApiKey`, `ApiSecret`, `Folder`, `MaxFileSizeMb`.

Ngoài Cloudinary, một số chức năng admin bài viết và bảo hành có xử lý upload file vào wwwroot. Ví dụ `AdminArticlesController` nhận `coverImageFile`, `WarrantyController.Create` nhận ảnh bằng chứng và lưu đường dẫn evidence. Riêng sản phẩm có `ProductImageStorageService.cs` hỗ trợ validate và lưu ảnh sản phẩm.

Luồng upload Cloudinary:

Admin chọn ảnh
↓
Partial `Views/Shared/_AdminImageUploader.cshtml`
↓
POST `AdminImageUploads/Upload`
↓
`AdminImageUploadsController.Upload`
↓
`CloudinaryImageUploadService.UploadAsync`
↓
Cloudinary
↓
Trả URL ảnh về form admin.

### Có thể bị hỏi

**Thầy hỏi: Cloudinary cấu hình đâu?**  
Em trả lời: `appsettings.json`, section `Cloudinary`; service upload là `Services/CloudinaryImageUploadService.cs`; controller upload là `Controllers/AdminImageUploadsController.cs`.

## 11. Đánh giá sản phẩm

Chức năng đánh giá bắt buộc đăng nhập, controller là `Controllers/ProductReviewsController.cs` có `[Authorize]`. Người dùng chỉ đánh giá sản phẩm đã mua thông qua đơn hàng hợp lệ. Service `Services/ProductReviewService.cs` có hàm `FindEligibleOrderDetailAsync` để tìm order detail đủ điều kiện và `GetSectionAsync` để lấy phần review hiển thị ở chi tiết sản phẩm. View tạo đánh giá là `Views/ProductReviews/Create.cshtml`, partial hiển thị review là `Views/Products/_ProductReviews.cshtml`. Admin quản lý đánh giá qua `Controllers/AdminReviewsController.cs` và views `Views/AdminReviews`.

Luồng đánh giá:

User vào đơn đã mua hoặc sản phẩm
↓
`ProductReviewsController.Create` GET
↓
`ProductReviewService.FindEligibleOrderDetailAsync`
↓
`Views/ProductReviews/Create.cshtml`
↓
POST tạo đánh giá
↓
Bảng `ProductReviews`
↓
Chi tiết sản phẩm hiển thị qua `_ProductReviews.cshtml`.

### Có thể bị hỏi

**Thầy hỏi: Đánh giá lưu bảng nào?**  
Em trả lời: `ProductReviews`.

**Thầy hỏi: Ai được đánh giá?**  
Em trả lời: Người dùng đã đăng nhập và có đơn hàng/order detail đủ điều kiện, kiểm tra trong `ProductReviewService.FindEligibleOrderDetailAsync`.

## 12. Bảo hành

Chức năng bảo hành nằm trong `Controllers/WarrantyController.cs`. Người dùng có thể tra cứu bảo hành qua `/Warranty` hoặc `/Warranty/Check`, tạo yêu cầu qua `/Warranty/Create`, xem danh sách qua `/Warranty/MyRequests`, xem chi tiết qua `/Warranty/Detail/{id}`. View tương ứng nằm trong `Views/Warranty`. Dữ liệu lưu bảng `WarrantyRequests`, liên kết với `Orders`, `OrderDetails`, `Products`, `Users` nếu có. Admin xử lý bảo hành ở `Controllers/AdminWarrantyController.cs` và `Views/AdminWarranty`.

Luồng tạo yêu cầu bảo hành:

User chọn sản phẩm đã mua
↓
`Views/Warranty/Create.cshtml`
↓
POST `/Warranty/Create`
↓
`WarrantyController.Create`
↓
Kiểm tra quyền truy cập order detail và thời hạn bảo hành
↓
Lưu evidence file nếu có
↓
`ApplicationDbContext.WarrantyRequests`
↓
Bảng `WarrantyRequests`
↓
Redirect chi tiết yêu cầu.

### Có thể bị hỏi

**Thầy hỏi: Bảo hành lưu ở bảng nào?**  
Em trả lời: `WarrantyRequests`.

**Thầy hỏi: Admin cập nhật bảo hành ở đâu?**  
Em trả lời: `Controllers/AdminWarrantyController.cs`, views `Views/AdminWarranty/Index.cshtml` và `Detail.cshtml`.

## 13. Banner, bài viết, liên hệ, dashboard và admin

Banner admin nằm ở `Controllers/AdminBannersController.cs`, views `Views/AdminBanners`, model `Banner`, bảng `Banners`. Admin có thể tạo, sửa, bật tắt, xóa banner và lưu cài đặt site qua `SiteSettings`.

Bài viết public nằm ở `Controllers/ArticlesController.cs`, views `Views/Articles`. Admin bài viết nằm ở `Controllers/AdminArticlesController.cs`, views `Views/AdminArticles`, model `Article`, bảng `Articles`. Khi người dùng xem chi tiết bài viết theo slug, route trong `Program.cs` map `Articles/{slug}` hoặc `Articles/Detail/{slug}` đến `ArticlesController.Detail`.

Liên hệ nằm ở `Controllers/ContactController.cs`, view `Views/Contact/Index.cshtml`; thông tin liên hệ lưu bảng `Feedbacks`. Admin/support xem liên hệ qua `ContactController.Manage` và `Views/Contact/Manage.cshtml`.

Admin dashboard nằm ở `Controllers/AdminDashboardController.cs`, view `Views/AdminDashboard/Index.cshtml`, viewmodel `ViewModels/AdminDashboardVm.cs`. Báo cáo doanh thu nằm ở `AdminReportsController` và `Views/AdminReports/Revenue.cshtml`. Hóa đơn ở `AdminInvoicesController` và `Views/AdminInvoices/Index.cshtml`. Quản lý đơn hàng ở `AdminOrdersController` và `Views/AdminOrders`.

### Có thể bị hỏi

**Thầy hỏi: Bài viết public và admin khác nhau thế nào?**  
Em trả lời: Public dùng `ArticlesController` và `Views/Articles`; admin dùng `AdminArticlesController` và `Views/AdminArticles`.

**Thầy hỏi: Dashboard lấy dữ liệu từ đâu?**  
Em trả lời: `AdminDashboardController.Index` query từ `Orders`, `Products`, `Users`, `Feedbacks`, sau đó đưa vào `AdminDashboardVm`.

## 14. Các chức năng và file xử lý

### Đăng nhập
Controller: `Controllers/AccountController.cs`  
View: `Views/Account/Login.cshtml`  
Model/ViewModel: `ViewModels/AccountViewModels.cs`, `Models/Entities.cs` class `User`  
Service: `Services/AuthService.cs`, `Services/CartService.cs`  
Database: `Users`, `Roles`  
Luồng: Login view → `AccountController.Login` → `AuthService.ValidateUserAsync` → `Users` → cookie auth.

### Đăng ký
Controller: `Controllers/AccountController.cs`  
View: `Views/Account/Register.cshtml`  
Service: `AuthService.HashPassword`  
Database: `Users`  
Luồng: Register view → POST Register → hash password → lưu `Users`.

### Sản phẩm
Controller: `Controllers/ProductsController.cs`  
Views: `Views/Products/Index.cshtml`, `Views/Products/Detail.cshtml`  
Model: `Product`, `Category`, `ProductImage`  
Database: `Products`, `Categories`, `ProductImages`  
Service: `ProductReviewService`, `SearchKeywordService`.

### Giỏ hàng
Controller: `Controllers/CartController.cs`  
View: `Views/Cart/Index.cshtml`  
Service: `Services/CartService.cs`  
Database: `Carts`, `CartItems` hoặc Session nếu khách chưa đăng nhập.

### Đặt hàng
Controller: `Controllers/OrdersController.cs`  
Views: `Views/Orders/Checkout.cshtml`, `Success.cshtml`, `BankTransfer.cshtml`, `MyOrders.cshtml`, `Detail.cshtml`  
Service: `CartService`, `VoucherService`, `ShippingService`, `OrderExpirationService`  
Database: `Orders`, `OrderDetails`, `Products`, `VoucherUsages`.

### Thanh toán
Controller: `Controllers/OrdersController.cs`, `Controllers/AdminOrdersController.cs`  
Views: `Views/Orders/BankTransfer.cshtml`, `Views/Orders/Success.cshtml`  
Service: `Services/OrderExpirationService.cs`  
Database: `Orders` với `PaymentMethod`, `PaymentStatus`, `TransferContent`.  
Ghi chú: source hiện thể hiện COD/chuyển khoản, chưa thấy tích hợp VNPay/Momo/Stripe.

### Voucher
Controller API: `Controllers/VouchersController.cs`  
Admin: `Controllers/AdminVouchersController.cs`, `Views/AdminVouchers`  
Service: `Services/VoucherService.cs`  
Database: `Vouchers`, `VoucherUsages`.

### Chat AI
Controller: `Controllers/AiChatController.cs`  
Service: `Services/AiChatService.cs`, `Services/ProductSearchForAiService.cs`, `Services/ShopPolicyService.cs`  
Config: `appsettings.json` section `AiChat`  
Database: chủ yếu đọc `Products`; không chắc có lưu lịch sử AI riêng vì controller AI trả response trực tiếp.

### Chat hỗ trợ
Controllers: `Controllers/SupportChatController.cs`, `Controllers/AdminChatController.cs`  
Hub: `Hubs/ChatHub.cs`  
Views: `Views/Shared/_SupportChatBox.cshtml`, `Views/AdminChat/Index.cshtml`  
Service: `Services/SupportChatAutomationService.cs`  
Database: `ChatConversations`, `ChatMessages`.

### Banner
Controller: `Controllers/AdminBannersController.cs`  
Views: `Views/AdminBanners`  
Model: `Banner`  
Database: `Banners`, `SiteSettings`.

### Bài viết
Public: `Controllers/ArticlesController.cs`, `Views/Articles`  
Admin: `Controllers/AdminArticlesController.cs`, `Views/AdminArticles`  
Model: `Article`  
Database: `Articles`.

### Build PC
Controller: `Controllers/BuildPcController.cs`  
View: `Views/BuildPc/Index.cshtml`  
ViewModel: `ViewModels/BuildPcVm.cs`  
Service: `Services/BuildCompatibilityService.cs`, `Services/CartService.cs`  
Database: `Products`; entity có `BuildPcConfigs`, `BuildPcItems`; lựa chọn hiện lưu session.

### Upload ảnh
Controller: `Controllers/AdminImageUploadsController.cs`  
Partial: `Views/Shared/_AdminImageUploader.cshtml`  
Service: `Services/CloudinaryImageUploadService.cs`, `Services/ProductImageStorageService.cs`  
Config: `appsettings.json` section `Cloudinary`.

### Review
Controller: `Controllers/ProductReviewsController.cs`  
Admin: `Controllers/AdminReviewsController.cs`  
Views: `Views/ProductReviews/Create.cshtml`, `Views/Products/_ProductReviews.cshtml`, `Views/AdminReviews`  
Service: `Services/ProductReviewService.cs`  
Database: `ProductReviews`.

### Warranty
Controller: `Controllers/WarrantyController.cs`  
Admin: `Controllers/AdminWarrantyController.cs`  
Views: `Views/Warranty`, `Views/AdminWarranty`  
Database: `WarrantyRequests`, liên quan `Orders`, `OrderDetails`, `Products`.

### Admin users
Controller: `Controllers/AdminUsersController.cs`  
Views: `Views/AdminUsers`  
Service: `Services/AuthService.cs`  
Database: `Users`, `Roles`.

## 15. Kết luận

Kính thưa quý thầy cô, phần mềm KKSHOP của em là một hệ thống bán PC và linh kiện có đầy đủ các luồng thực hành: xem sản phẩm, tìm kiếm, giỏ hàng, đặt hàng, thanh toán COD/chuyển khoản, vận chuyển, voucher, đánh giá, bảo hành, banner, bài viết, chat AI, chat hỗ trợ realtime và quản trị. Khi bảo vệ, nếu thầy cô hỏi một nút bấm chạy qua đâu, em sẽ lần theo hướng View → Controller → Service → DbContext → bảng database → View/JSON trả về. Em xin hết phần trình bày và sẵn sàng trả lời câu hỏi về source code.
