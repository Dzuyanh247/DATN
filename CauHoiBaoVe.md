# Câu hỏi bảo vệ WEBSITE KKSHOP theo source code

## 1. Project khởi động từ file nào?

**Trả lời:** `Program.cs`.

## 2. DbContext chính tên gì và nằm đâu?

**Trả lời:** `ApplicationDbContext` trong `Data/ApplicationDbContext.cs`.

## 3. Chuỗi kết nối database cấu hình ở đâu?

**Trả lời:** Trong `appsettings.json`, section `ConnectionStrings`; chọn connection qua `Data/DatabaseConfiguration.cs`.

## 4. Controller nào xử lý trang chủ?

**Trả lời:** `Controllers/HomeController.cs`, action `Index`.

## 5. View trang chủ là file nào?

**Trả lời:** `Views/Home/Index.cshtml`.

## 6. Sản phẩm lưu bảng nào?

**Trả lời:** Bảng `Products`, entity `Product` trong `Models/Entities.cs`.

## 7. Danh mục lưu bảng nào?

**Trả lời:** Bảng `Categories`, entity `Category`.

## 8. Ảnh sản phẩm lưu bảng nào?

**Trả lời:** Bảng `ProductImages`, entity `ProductImage`.

## 9. Controller danh sách sản phẩm là gì?

**Trả lời:** `Controllers/ProductsController.cs`, action `Index`.

## 10. View danh sách sản phẩm là file nào?

**Trả lời:** `Views/Products/Index.cshtml`.

## 11. Controller chi tiết sản phẩm là gì?

**Trả lời:** `ProductsController.Detail` trong `Controllers/ProductsController.cs`.

## 12. View chi tiết sản phẩm là gì?

**Trả lời:** `Views/Products/Detail.cshtml`.

## 13. Search xử lý ở đâu?

**Trả lời:** `ProductsController.Index`; từ khóa được ghi qua `Services/SearchKeywordService.cs` vào `SearchKeywords`.

## 14. Gợi ý từ khóa xử lý ở đâu?

**Trả lời:** `Services/SearchSuggestionService.cs`.

## 15. Quản lý từ khóa search admin ở đâu?

**Trả lời:** `Controllers/AdminSearchKeywordsController.cs`, view `Views/AdminSearchKeywords/Index.cshtml`.

## 16. Đăng nhập controller nào?

**Trả lời:** `Controllers/AccountController.cs`, action `Login`.

## 17. View đăng nhập là gì?

**Trả lời:** `Views/Account/Login.cshtml`.

## 18. Mật khẩu kiểm tra ở service nào?

**Trả lời:** `Services/AuthService.cs`.

## 19. User lưu bảng nào?

**Trả lời:** `Users`; role lưu `Roles`.

## 20. Authentication cấu hình ở đâu?

**Trả lời:** `Program.cs` với cookie authentication và `Constants/AuthSchemes.cs`.

## 21. Access denied view ở đâu?

**Trả lời:** `Views/Account/AccessDenied.cshtml`.

## 22. Đăng ký xử lý ở đâu?

**Trả lời:** `AccountController.Register`, view `Views/Account/Register.cshtml`.

## 23. Quên mật khẩu dùng bảng nào?

**Trả lời:** `PasswordResetOtps`.

## 24. Gửi email bằng service nào?

**Trả lời:** `Services/SmtpEmailSender.cs`.

## 25. Giỏ hàng controller nào?

**Trả lời:** `Controllers/CartController.cs`.

## 26. Giỏ hàng service nào?

**Trả lời:** `Services/CartService.cs`.

## 27. View giỏ hàng ở đâu?

**Trả lời:** `Views/Cart/Index.cshtml`.

## 28. Giỏ hàng user đăng nhập lưu bảng nào?

**Trả lời:** `Carts` và `CartItems`.

## 29. Giỏ hàng khách chưa đăng nhập lưu ở đâu?

**Trả lời:** Session, qua `CartService`.

## 30. Đặt hàng controller nào?

**Trả lời:** `Controllers/OrdersController.cs`.

## 31. Route checkout là gì?

**Trả lời:** GET/POST `/Checkout` trong `OrdersController`.

## 32. View checkout là gì?

**Trả lời:** `Views/Orders/Checkout.cshtml`.

## 33. Đơn hàng lưu bảng nào?

**Trả lời:** `Orders` và `OrderDetails`.

## 34. Sau đặt hàng thành công về view nào?

**Trả lời:** `Views/Orders/Success.cshtml` hoặc `Views/Orders/BankTransfer.cshtml` nếu chuyển khoản.

## 35. Thanh toán chuyển khoản view nào?

**Trả lời:** `Views/Orders/BankTransfer.cshtml`.

## 36. Admin xác nhận chuyển khoản ở đâu?

**Trả lời:** `AdminOrdersController.ConfirmBankTransfer`.

## 37. Có VNPay/Momo không?

**Trả lời:** Theo source hiện tại chưa thấy service VNPay/Momo/Stripe; xử lý COD/chuyển khoản trong `OrdersController`.

## 38. Trạng thái đơn xử lý service nào?

**Trả lời:** `Services/OrderExpirationService.cs`.

## 39. Admin quản lý đơn ở đâu?

**Trả lời:** `Controllers/AdminOrdersController.cs`, views `Views/AdminOrders`.

## 40. Đơn của tôi ở đâu?

**Trả lời:** `OrdersController.MyOrders`, view `Views/Orders/MyOrders.cshtml`.

## 41. Chi tiết đơn user ở đâu?

**Trả lời:** `OrdersController.Detail`, view `Views/Orders/Detail.cshtml`.

## 42. Xuất báo giá ở đâu?

**Trả lời:** `OrdersController.Quotation`, view `Views/Orders/Quotation.cshtml`.

## 43. Xuất Excel đơn hàng ở đâu?

**Trả lời:** `OrdersController.ExportExcel`.

## 44. Voucher API ở đâu?

**Trả lời:** `Controllers/VouchersController.cs`, route `api/vouchers`.

## 45. Voucher service nào?

**Trả lời:** `Services/VoucherService.cs`.

## 46. Voucher lưu bảng nào?

**Trả lời:** `Vouchers`, lượt dùng `VoucherUsages`.

## 47. Admin quản lý voucher ở đâu?

**Trả lời:** `Controllers/AdminVouchersController.cs`, views `Views/AdminVouchers`.

## 48. Shipping API ở đâu?

**Trả lời:** `Controllers/ShippingController.cs`, route `api/shipping`.

## 49. GHN cấu hình ở đâu?

**Trả lời:** `appsettings.json` section `GHN`.

## 50. Lấy tỉnh/huyện/xã GHN service nào?

**Trả lời:** `Services/GhnAddressService.cs`.

## 51. Tính phí GHN service nào?

**Trả lời:** `Services/GhnShippingService.cs`.

## 52. Tính phí nội bộ service nào?

**Trả lời:** `Services/ShippingFeeCalculator.cs`.

## 53. Điều phối phí ship service nào?

**Trả lời:** `Services/ShippingService.cs`.

## 54. OpenRouteService dùng file nào?

**Trả lời:** `Services/OpenRouteServiceProvider.cs`, key trong `appsettings.json` section `OpenRouteService`.

## 55. Build PC controller nào?

**Trả lời:** `Controllers/BuildPcController.cs`.

## 56. Route Build PC là gì?

**Trả lời:** `/buildpc`.

## 57. View Build PC là gì?

**Trả lời:** `Views/BuildPc/Index.cshtml`.

## 58. ViewModel Build PC là gì?

**Trả lời:** `ViewModels/BuildPcVm.cs`.

## 59. Tương thích linh kiện kiểm tra ở đâu?

**Trả lời:** `Services/BuildCompatibilityService.cs`.

## 60. Build PC chọn linh kiện lưu ở đâu?

**Trả lời:** Luồng hiện tại lưu selected components trong Session; entity `BuildPcConfigs`, `BuildPcItems` có trong DbContext.

## 61. Dữ liệu linh kiện lấy từ bảng nào?

**Trả lời:** `Products`, với `ProductType=Component` và `ComponentType`.

## 62. Thêm cấu hình build vào giỏ gọi gì?

**Trả lời:** `BuildPcController.AddBuildToCart` gọi `CartService`.

## 63. Chat AI API ở đâu?

**Trả lời:** `Controllers/AiChatController.cs`, route `api/chat/ai`.

## 64. AI service nào?

**Trả lời:** `GeminiChatService` trong `Services/AiChatService.cs`.

## 65. AI cấu hình ở đâu?

**Trả lời:** `appsettings.json` section `AiChat`.

## 66. AI lấy sản phẩm bằng service nào?

**Trả lời:** `Services/ProductSearchForAiService.cs`.

## 67. Model request AI ở đâu?

**Trả lời:** `ViewModels/Ai/AiChatModels.cs`.

## 68. Chat hỗ trợ khách controller nào?

**Trả lời:** `Controllers/SupportChatController.cs`.

## 69. Chat admin controller nào?

**Trả lời:** `Controllers/AdminChatController.cs`.

## 70. Chat realtime dùng hub nào?

**Trả lời:** `Hubs/ChatHub.cs`, map `/hubs/support-chat`.

## 71. Chat hỗ trợ lưu bảng nào?

**Trả lời:** `ChatConversations` và `ChatMessages`.

## 72. Quick action chat service nào?

**Trả lời:** `Services/SupportChatAutomationService.cs`.

## 73. Partial chat khách ở đâu?

**Trả lời:** `Views/Shared/_SupportChatBox.cshtml`.

## 74. View chat admin ở đâu?

**Trả lời:** `Views/AdminChat/Index.cshtml`.

## 75. Upload Cloudinary controller nào?

**Trả lời:** `Controllers/AdminImageUploadsController.cs`.

## 76. Upload Cloudinary service nào?

**Trả lời:** `Services/CloudinaryImageUploadService.cs`.

## 77. Cloudinary config ở đâu?

**Trả lời:** `appsettings.json` section `Cloudinary`.

## 78. Partial upload admin ở đâu?

**Trả lời:** `Views/Shared/_AdminImageUploader.cshtml`.

## 79. Ảnh bài viết upload ở controller nào?

**Trả lời:** `Controllers/AdminArticlesController.cs` nhận `coverImageFile`.

## 80. ProductImageStorageService dùng để làm gì?

**Trả lời:** Validate/lưu/xóa ảnh sản phẩm nội bộ.

## 81. Review controller nào?

**Trả lời:** `Controllers/ProductReviewsController.cs`.

## 82. Review service nào?

**Trả lời:** `Services/ProductReviewService.cs`.

## 83. Review model ở đâu?

**Trả lời:** `Models/ProductReview.cs`.

## 84. Review lưu bảng nào?

**Trả lời:** `ProductReviews`.

## 85. Partial hiển thị review ở đâu?

**Trả lời:** `Views/Products/_ProductReviews.cshtml`.

## 86. View tạo review ở đâu?

**Trả lời:** `Views/ProductReviews/Create.cshtml`.

## 87. Admin quản lý review ở đâu?

**Trả lời:** `Controllers/AdminReviewsController.cs`, views `Views/AdminReviews`.

## 88. Ai được review?

**Trả lời:** User đăng nhập có order detail hợp lệ, kiểm tra trong `ProductReviewService.FindEligibleOrderDetailAsync`.

## 89. Bảo hành controller nào?

**Trả lời:** `Controllers/WarrantyController.cs`.

## 90. View bảo hành ở đâu?

**Trả lời:** `Views/Warranty/Check.cshtml`, `Create.cshtml`, `MyRequests.cshtml`, `Detail.cshtml`.

## 91. Bảo hành lưu bảng nào?

**Trả lời:** `WarrantyRequests`.

## 92. Admin bảo hành ở đâu?

**Trả lời:** `Controllers/AdminWarrantyController.cs`, views `Views/AdminWarranty`.

## 93. File evidence bảo hành xử lý ở đâu?

**Trả lời:** `WarrantyController.Create` xử lý upload evidence file.

## 94. Banner model và bảng là gì?

**Trả lời:** Model `Banner`, bảng `Banners`.

## 95. Admin banner controller nào?

**Trả lời:** `Controllers/AdminBannersController.cs`.

## 96. Views banner ở đâu?

**Trả lời:** `Views/AdminBanners`.

## 97. Banner trang chủ lấy controller nào?

**Trả lời:** `HomeController.Index` lấy từ `Banners`.

## 98. Site settings lưu bảng nào?

**Trả lời:** `SiteSettings`.

## 99. Admin settings controller nào?

**Trả lời:** `Controllers/AdminSettingsController.cs`.

## 100. Bài viết public controller nào?

**Trả lời:** `Controllers/ArticlesController.cs`.

## 101. Bài viết admin controller nào?

**Trả lời:** `Controllers/AdminArticlesController.cs`.

## 102. Bài viết lưu bảng nào?

**Trả lời:** `Articles`.

## 103. Views bài viết public ở đâu?

**Trả lời:** `Views/Articles`.

## 104. Views bài viết admin ở đâu?

**Trả lời:** `Views/AdminArticles`.

## 105. Liên hệ controller nào?

**Trả lời:** `Controllers/ContactController.cs`.

## 106. Liên hệ lưu bảng nào?

**Trả lời:** `Feedbacks`.

## 107. Admin xem liên hệ ở đâu?

**Trả lời:** `ContactController.Manage`, view `Views/Contact/Manage.cshtml`.

## 108. Dashboard admin ở đâu?

**Trả lời:** `Controllers/AdminDashboardController.cs`, view `Views/AdminDashboard/Index.cshtml`.

## 109. Báo cáo doanh thu ở đâu?

**Trả lời:** `Controllers/AdminReportsController.cs`, view `Views/AdminReports/Revenue.cshtml`.

## 110. Hóa đơn admin ở đâu?

**Trả lời:** `Controllers/AdminInvoicesController.cs`, view `Views/AdminInvoices/Index.cshtml`.

## 111. Quản lý user ở đâu?

**Trả lời:** `Controllers/AdminUsersController.cs`, views `Views/AdminUsers`.

## 112. Quản lý category ở đâu?

**Trả lời:** `Controllers/AdminCategoriesController.cs`, views `Views/AdminCategories`.

## 113. Quản lý linh kiện ở đâu?

**Trả lời:** `Controllers/AdminComponentsController.cs`, views `Views/AdminComponents`.

## 114. API brands nằm đâu?

**Trả lời:** `Controllers/BrandsApiController.cs`, route `api/brands`.

## 115. API accessories nằm đâu?

**Trả lời:** `Controllers/AccessoriesApiController.cs`, route `api/accessories`.

## 116. Compare controller nào?

**Trả lời:** `Controllers/CompareController.cs`.

## 117. Compare view nào?

**Trả lời:** `Views/Compare/Index.cshtml`.

## 118. Compare lưu dữ liệu ở đâu?

**Trả lời:** Session qua `Services/CompareSessionService.cs`.

## 119. SignalR route map ở đâu?

**Trả lời:** `Program.cs` map `ChatHub` tới `/hubs/support-chat`.

## 120. Middleware session đăng ký ở đâu?

**Trả lời:** `Program.cs` gọi `AddSession` và `UseSession`.

## 121. Middleware authentication authorization thứ tự thế nào?

**Trả lời:** Trong `Program.cs`: `UseSession` rồi `UseAuthentication` rồi `UseAuthorization`.

## 122. AdminProducts yêu cầu role gì?

**Trả lời:** `[Authorize(Roles = "Admin,Staff")]`.

## 123. AdminUsers yêu cầu role gì?

**Trả lời:** `[Authorize(Roles = "Admin")]`.

## 124. AdminChat yêu cầu role gì?

**Trả lời:** `Admin,SupportStaff,CustomerSupport`.

## 125. ProductReviews có yêu cầu đăng nhập không?

**Trả lời:** Có, controller có `[Authorize]`.

## 126. DbSet SearchKeywords nằm đâu?

**Trả lời:** `Data/ApplicationDbContext.cs`.

## 127. Audit CreatedAt UpdatedAt xử lý ở đâu?

**Trả lời:** `ApplicationDbContext.ApplyAuditFields` trong `Data/ApplicationDbContext.cs`.

## 128. Route article slug cấu hình ở đâu?

**Trả lời:** `Program.cs` map `Articles/{slug}` và `Articles/Detail/{slug}`.

## 129. Route linh kiện /linh-kien cấu hình ở đâu?

**Trả lời:** `Program.cs` map route `components-root` và các route liên quan.

## 130. Nếu request JSON chưa đăng nhập trả gì?

**Trả lời:** Trong `Program.cs`, cookie events trả JSON 401 thay vì redirect nếu request expects JSON.

## 131. Database script mẫu ở đâu?

**Trả lời:** `Database/DATN_PCStore.sql`.

## 132. Release hướng dẫn chạy ở đâu?

**Trả lời:** `README_CHAY_WEB.txt` và `ReleasePackage/README_CHAY_WEB.txt`.

## 133. Chức năng showroom dùng service nào?

**Trả lời:** `Services/ShowroomService.cs`.

## 134. Shop policy cho AI/chat ở đâu?

**Trả lời:** `Services/ShopPolicyService.cs`.

## 135. Email settings nằm đâu?

**Trả lời:** `appsettings.json` section `EmailSettings` và class `Services/EmailSettings.cs`.

## 136. RolePermissionService có file nào?

**Trả lời:** `Services/RolePermissionService.cs`.

## 137. Constants auth scheme ở đâu?

**Trả lời:** `Constants/AuthSchemes.cs`.

## 138. Support chat defaults ở đâu?

**Trả lời:** `Constants/SupportChatDefaults.cs`.

## 139. View layout chính ở đâu?

**Trả lời:** `Views/Shared/_Layout.cshtml`.

## 140. Cart count view component ở đâu?

**Trả lời:** `ViewComponents/CartCountViewComponent.cs` và view `Views/Shared/Components/CartCount/Default.cshtml`.

## 141. Nếu chưa chắc chức năng nào thì trả lời thế nào?

**Trả lời:** Nói rõ theo source hiện tại em thấy/chưa thấy, ví dụ chưa thấy tích hợp VNPay/Momo/Stripe riêng.

