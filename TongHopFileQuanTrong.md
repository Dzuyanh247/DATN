# Tổng hợp file quan trọng của WEBSITE KKSHOP

## 1. File khởi động và cấu hình

- `Datn.PcStore.csproj` → project ASP.NET Core MVC chính.
- `Program.cs` → đăng ký MVC, SignalR, DbContext, Authentication cookie, Session, service nghiệp vụ, middleware và route.
- `appsettings.json` → cấu hình connection string, GHN, ShippingPolicy, ShopAddress, EmailSettings, Database, Cloudinary, AiChat, Seeding.
- `appsettings.Development.json` → cấu hình môi trường development.
- `Data/DatabaseConfiguration.cs` → chọn connection string, kiểm tra/migrate database.
- `Data/ApplicationDbContext.cs` → DbContext chính, khai báo DbSet và cấu hình mapping bảng.
- `Data/SeedData.cs` → seed dữ liệu nếu bật cấu hình seed.

## 2. Controllers public

- `Controllers/HomeController.cs` → trang chủ, lấy banner, danh mục và các nhóm sản phẩm.
- `Controllers/ProductsController.cs` → danh sách sản phẩm, lọc, tìm kiếm, chi tiết sản phẩm.
- `Controllers/CartController.cs` → giỏ hàng, thêm/sửa/xóa/clear, mua ngay, bundle.
- `Controllers/OrdersController.cs` → checkout, tạo đơn hàng, theo dõi đơn, đơn của tôi, thanh toán/chuyển khoản, báo giá, xuất Excel.
- `Controllers/AccountController.cs` → đăng ký, đăng nhập, đăng xuất, hồ sơ, đổi mật khẩu, quên mật khẩu OTP.
- `Controllers/ArticlesController.cs` → danh sách và chi tiết bài viết public.
- `Controllers/ContactController.cs` → gửi liên hệ và quản lý liên hệ.
- `Controllers/WarrantyController.cs` → tra cứu bảo hành, tạo yêu cầu bảo hành, danh sách yêu cầu, chi tiết yêu cầu.
- `Controllers/ProductReviewsController.cs` → tạo đánh giá sản phẩm sau khi mua.
- `Controllers/BuildPcController.cs` → build PC, chọn linh kiện, kiểm tra tương thích, thêm cấu hình vào giỏ.
- `Controllers/CompareController.cs` → so sánh sản phẩm.
- `Controllers/SupportChatController.cs` → API chat hỗ trợ phía khách.

## 3. Controllers API

- `Controllers/AiChatController.cs` → API `api/chat/ai`, hỏi AI Gemini.
- `Controllers/ShippingController.cs` → API `api/shipping`, lấy tỉnh/huyện/xã GHN và tính phí ship.
- `Controllers/VouchersController.cs` → API `api/vouchers`, validate voucher và lấy voucher khả dụng.
- `Controllers/BrandsApiController.cs` → API `api/brands`, lấy/tạo thương hiệu linh kiện.
- `Controllers/AccessoriesApiController.cs` → API `api/accessories`, lấy phụ kiện/linh kiện theo điều kiện.

## 4. Controllers admin

- `Controllers/AdminDashboardController.cs` → dashboard tổng quan admin.
- `Controllers/AdminProductsController.cs` → quản lý sản phẩm PC.
- `Controllers/AdminComponentsController.cs` → quản lý linh kiện.
- `Controllers/AdminCategoriesController.cs` → quản lý danh mục.
- `Controllers/AdminOrdersController.cs` → quản lý đơn hàng, cập nhật trạng thái, xác nhận chuyển khoản.
- `Controllers/AdminInvoicesController.cs` → danh sách hóa đơn.
- `Controllers/AdminReportsController.cs` → báo cáo doanh thu.
- `Controllers/AdminUsersController.cs` → quản lý user, role, khóa/mở tài khoản.
- `Controllers/AdminBannersController.cs` → quản lý banner và cài đặt site liên quan banner.
- `Controllers/AdminArticlesController.cs` → quản lý bài viết.
- `Controllers/AdminReviewsController.cs` → duyệt/ẩn/xóa/trả lời đánh giá.
- `Controllers/AdminWarrantyController.cs` → xử lý yêu cầu bảo hành.
- `Controllers/AdminVouchersController.cs` → quản lý voucher.
- `Controllers/AdminSearchKeywordsController.cs` → quản lý từ khóa tìm kiếm hot/pinned/visible.
- `Controllers/AdminSettingsController.cs` → cấu hình site settings.
- `Controllers/AdminChatController.cs` → màn hình nhân viên/admin chat hỗ trợ.
- `Controllers/AdminImageUploadsController.cs` → upload ảnh Cloudinary cho admin.

## 5. Services nghiệp vụ

- `Services/AuthService.cs` → hash password, verify password, validate user đăng nhập.
- `Services/AccountPasswordResetService.cs` → token/OTP reset password và đổi mật khẩu.
- `Services/SmtpEmailSender.cs` → gửi email SMTP.
- `Services/CartService.cs` → giỏ hàng database/session, merge guest cart, buy now cart.
- `Services/VoucherService.cs` → validate voucher, tính giảm giá, kiểm tra điều kiện dùng.
- `Services/OrderExpirationService.cs` → đơn chờ thanh toán, hết hạn, hủy, khôi phục tồn kho.
- `Services/ProductReviewService.cs` → kiểm tra quyền đánh giá và lấy section review.
- `Services/SearchKeywordService.cs` → chuẩn hóa và lưu số lần tìm kiếm.
- `Services/SearchSuggestionService.cs` → gợi ý từ khóa header/hot keywords.
- `Services/BuildCompatibilityService.cs` → kiểm tra tương thích linh kiện build PC.
- `Services/CompareSessionService.cs` → lưu danh sách so sánh trong session.
- `Services/AiChatService.cs` → `GeminiChatService`, gọi Gemini và tạo câu trả lời AI.
- `Services/ProductSearchForAiService.cs` → tìm sản phẩm đưa vào ngữ cảnh AI.
- `Services/ShopPolicyService.cs` → trả lời chính sách shop cho AI/chat.
- `Services/SupportChatAutomationService.cs` → quick action chat hỗ trợ: đơn hàng, bảo hành, thanh toán, tư vấn.
- `Services/CloudinaryImageUploadService.cs` → upload ảnh lên Cloudinary.
- `Services/ProductImageStorageService.cs` → validate/lưu/xóa ảnh sản phẩm nội bộ.
- `Services/ShippingService.cs` → điều phối tính phí ship.
- `Services/ShippingFeeCalculator.cs` → tính phí ship nội bộ theo khoảng cách/chính sách.
- `Services/GhnShippingService.cs` → gọi GHN tính phí ship.
- `Services/GhnAddressService.cs` → gọi GHN lấy tỉnh/huyện/xã.
- `Services/OpenRouteServiceProvider.cs` → gọi OpenRouteService cho geocode/route.
- `Services/GeocodingService.cs` → geocode địa chỉ.
- `Services/RouteService.cs` → lấy thông tin tuyến đường.
- `Services/ShowroomService.cs` → lấy showroom hiển thị header.

## 6. Models và bảng database

- `Models/BaseEntity.cs` → Id, CreatedAt, UpdatedAt cho entity.
- `Models/Entities.cs` → phần lớn entity: `Role`, `User`, `Category`, `Product`, `Banner`, `Cart`, `CartItem`, `Order`, `OrderDetail`, `WarrantyRequest`, `BuildPcConfig`, `BuildPcItem`, `Article`, `Feedback`, `SiteSetting`, `Voucher`, `VoucherUsage`, `SearchKeyword`.
- `Models/ProductReview.cs` → entity `ProductReview`, enum `ReviewStatus`.
- `Models/SupportChat.cs` → entity `ChatConversation`, `ChatMessage`, enum chat.

Bảng quan trọng:

- `Users`, `Roles` → tài khoản và phân quyền.
- `Products`, `ProductImages`, `Categories`, `ComponentBrands` → sản phẩm, linh kiện, danh mục, thương hiệu.
- `Carts`, `CartItems` → giỏ hàng user đăng nhập.
- `Orders`, `OrderDetails` → đơn hàng.
- `Vouchers`, `VoucherUsages` → mã giảm giá và lượt dùng.
- `ProductReviews` → đánh giá sản phẩm.
- `WarrantyRequests` → yêu cầu bảo hành.
- `Banners`, `SiteSettings` → banner và cấu hình site.
- `Articles` → bài viết.
- `Feedbacks` → liên hệ.
- `ChatConversations`, `ChatMessages` → chat hỗ trợ.
- `SearchKeywords` → từ khóa tìm kiếm.

## 7. ViewModels

- `ViewModels/HomeIndexVm.cs` → dữ liệu trang chủ.
- `ViewModels/ProductFilterVm.cs` → filter sản phẩm.
- `ViewModels/ProductDetailViewModel.cs` → chi tiết sản phẩm.
- `ViewModels/CartVm.cs` → giỏ hàng và checkout request.
- `ViewModels/CheckoutVm.cs` → dữ liệu trang checkout.
- `ViewModels/MyOrdersViewModel.cs` → danh sách/chi tiết đơn của tôi.
- `ViewModels/AdminOrdersVm.cs` → danh sách/chi tiết đơn admin.
- `ViewModels/AdminDashboardVm.cs` → dashboard admin.
- `ViewModels/AdminProductUpsertVm.cs` → form tạo/sửa sản phẩm.
- `ViewModels/BuildPcVm.cs` → màn hình build PC.
- `ViewModels/ProductReviewViewModels.cs` → tạo và hiển thị đánh giá.
- `ViewModels/WarrantyViewModels.cs` → tra cứu/tạo/danh sách bảo hành.
- `ViewModels/SupportChatViewModels.cs` → request chat hỗ trợ.
- `ViewModels/Ai/AiChatModels.cs` → request/response chat AI.
- `ViewModels/AdminUserViewModels.cs` → quản lý user.
- `ViewModels/AdminBannersIndexVm.cs` → quản lý banner.
- `ViewModels/AdminRevenueReportVm.cs` → báo cáo doanh thu.
- `ViewModels/CompareViewModels.cs` → so sánh sản phẩm.

## 8. Views public

- `Views/Home/Index.cshtml` → trang chủ.
- `Views/Shared/_Layout.cshtml` → layout chính, header/footer, script chung.
- `Views/Shared/_SupportChatBox.cshtml` → hộp chat hỗ trợ.
- `Views/Products/Index.cshtml` → danh sách sản phẩm.
- `Views/Products/Detail.cshtml` → chi tiết sản phẩm.
- `Views/Products/_ProductReviews.cshtml` → partial đánh giá.
- `Views/Cart/Index.cshtml` → giỏ hàng.
- `Views/Orders/Checkout.cshtml` → checkout.
- `Views/Orders/Success.cshtml` → đặt hàng thành công.
- `Views/Orders/BankTransfer.cshtml` → hướng dẫn chuyển khoản.
- `Views/Orders/MyOrders.cshtml` → đơn hàng của tôi.
- `Views/Orders/Detail.cshtml` → chi tiết đơn.
- `Views/Orders/TrackingLookup.cshtml` → tra cứu đơn.
- `Views/Orders/Quotation.cshtml` → báo giá.
- `Views/Account/Login.cshtml`, `Register.cshtml`, `Profile.cshtml`, `ForgotPassword.cshtml`, `VerifyResetCode.cshtml`, `ChangePassword.cshtml`, `AccessDenied.cshtml` → tài khoản.
- `Views/BuildPc/Index.cshtml` → build PC.
- `Views/Warranty/*.cshtml` → bảo hành.
- `Views/Articles/*.cshtml` → bài viết public.
- `Views/Compare/Index.cshtml` → so sánh.
- `Views/Contact/Index.cshtml` → liên hệ.

## 9. Views admin

- `Views/AdminDashboard/Index.cshtml` → dashboard.
- `Views/AdminProducts` → quản lý sản phẩm.
- `Views/AdminComponents` → quản lý linh kiện.
- `Views/AdminOrders` → quản lý đơn.
- `Views/AdminInvoices/Index.cshtml` → hóa đơn.
- `Views/AdminReports/Revenue.cshtml` → doanh thu.
- `Views/AdminUsers` → quản lý user.
- `Views/AdminBanners` → quản lý banner.
- `Views/AdminArticles` → quản lý bài viết.
- `Views/AdminReviews` → quản lý đánh giá.
- `Views/AdminWarranty` → quản lý bảo hành.
- `Views/AdminVouchers` → quản lý voucher.
- `Views/AdminSearchKeywords/Index.cshtml` → quản lý từ khóa tìm kiếm.
- `Views/AdminSettings/Index.cshtml` → cài đặt site.
- `Views/AdminChat/Index.cshtml` → chat hỗ trợ admin.

## 10. API tích hợp bên ngoài

- Gemini AI → `Services/AiChatService.cs`, cấu hình `AiChat` trong `appsettings.json`.
- GHN Shipping → `Services/GhnShippingService.cs`, `Services/GhnAddressService.cs`, cấu hình `GHN` trong `appsettings.json`.
- Cloudinary → `Services/CloudinaryImageUploadService.cs`, cấu hình `Cloudinary` trong `appsettings.json`.
- OpenRouteService → `Services/OpenRouteServiceProvider.cs`, cấu hình `OpenRouteService` trong `appsettings.json`.
- SMTP Gmail → `Services/SmtpEmailSender.cs`, cấu hình `EmailSettings` trong `appsettings.json`.
