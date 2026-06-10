# DATN PC Store - Giới thiệu hệ thống

## Slide 01 | DATN PC Store
- Website thương mại điện tử bán PC, laptop và linh kiện
- Nền tảng ASP.NET Core MVC
- Đồ án tốt nghiệp

## Slide 02 | Bài toán và mục tiêu
- Xây dựng kênh bán hàng trực tuyến cho cửa hàng thiết bị máy tính
- Hỗ trợ khách xem, tìm kiếm, lựa chọn và đặt mua sản phẩm
- Quản lý tập trung danh mục, sản phẩm, đơn hàng, người dùng và nội dung
- Bổ sung nghiệp vụ đặc thù: build PC, so sánh, bảo hành và hỗ trợ trực tuyến

## Slide 03 | Phạm vi và vai trò sử dụng
- Khách vãng lai: duyệt sản phẩm, giỏ hàng phiên, tra cứu đơn và chat hỗ trợ
- Khách hàng: hồ sơ, giỏ hàng lưu trong cơ sở dữ liệu, lịch sử đơn và bảo hành
- Quản trị viên: dashboard và các màn hình quản trị có phân quyền Admin
- Hệ thống tập trung vào mô hình thương mại điện tử server-rendered

## Slide 04 | Kiến trúc tổng thể
- Presentation: Controllers, Razor Views, CSS và JavaScript
- Business: service xác thực, giỏ hàng, vận chuyển, build PC, ảnh và đơn hàng
- Data: Entity Framework Core và SQL Server
- Tích hợp: GHN, bản đồ/định tuyến, SMTP và SignalR
- Dependency Injection được cấu hình tại Program.cs

## Slide 05 | Công nghệ sử dụng
- .NET 8 và ASP.NET Core MVC
- Razor Views cho giao diện render phía máy chủ
- Entity Framework Core 8 với SQL Server
- Cookie Authentication, Session và Memory Cache
- SignalR cho chat hỗ trợ thời gian thực
- Xuất dữ liệu đơn hàng dưới dạng bảng HTML tương thích tệp Excel `.xls`

## Slide 06 | Mô hình dữ liệu chính
- Tài khoản: User, Role và PasswordResetOtp
- Catalog: Category, Product, ProductImage và Banner
- Bán hàng: Cart, CartItem, Order và OrderDetail
- Nghiệp vụ mở rộng: BuildPcConfig, WarrantyRequest, Article và Feedback
- Vận hành: SiteSetting, ShippingConfig, ChatConversation và ChatMessage

## Slide 07 | Trang chủ và điều hướng mua sắm
- Hiển thị banner đang hoạt động theo vị trí và thứ tự
- Tổ chức sản phẩm theo các khu vực nổi bật trên trang chủ
- Thanh tìm kiếm dẫn đến danh sách sản phẩm
- Điều hướng nhanh đến tin công nghệ, build PC, tài khoản và giỏ hàng

## Slide 08 | Danh sách và chi tiết sản phẩm
- Tìm theo từ khóa và danh mục
- Lọc theo thương hiệu, khoảng giá, CPU, RAM và GPU
- Kết quả được sắp xếp theo thời điểm tạo mới nhất
- Trang chi tiết hiển thị ảnh, giá, tồn kho, thông số và sản phẩm mua kèm
- Có thao tác thêm giỏ hàng, mua ngay và thêm vào danh sách so sánh

## Slide 09 | Tài khoản và phân quyền
- Đăng ký, đăng nhập và đăng xuất bằng cookie authentication
- Cập nhật hồ sơ và đổi mật khẩu
- Quên mật khẩu qua mã OTP gửi email, có thời hạn và giới hạn số lần thử
- Phân quyền theo role; khu vực quản trị yêu cầu vai trò Admin
- Chuyển giỏ hàng phiên sang tài khoản sau khi đăng nhập

## Slide 10 | Giỏ hàng đa trạng thái
- Khách vãng lai lưu giỏ hàng trong Session
- Người dùng đăng nhập lưu Cart và CartItem trong SQL Server
- Thêm, mua ngay, cập nhật số lượng, xóa từng dòng hoặc xóa toàn bộ
- Kiểm tra sản phẩm hoạt động, tồn kho và số lượng hợp lệ
- Tính tạm tính, giảm giá và tổng tiền trên ViewModel

## Slide 11 | Checkout và tính phí vận chuyển
- Nhập thông tin nhận hàng, địa chỉ và phương thức thanh toán
- Lấy tỉnh, quận/huyện, phường/xã qua dịch vụ địa chỉ GHN
- Tính phí qua GHN khi cấu hình hợp lệ
- Có cơ chế tính theo khoảng cách và chính sách vận chuyển dự phòng
- Kiểm tra lại giỏ hàng và tồn kho trước khi tạo đơn

## Slide 12 | Quy trình tạo đơn an toàn
- Checkout sử dụng transaction của Entity Framework Core
- Tạo Order và các OrderDetail từ dữ liệu giỏ hàng
- Trừ tồn kho từng sản phẩm trong cùng transaction
- Xóa giỏ hàng sau khi tạo đơn thành công
- Rollback khi phát sinh lỗi để tránh dữ liệu đơn hàng dở dang

## Slide 13 | Thanh toán và chứng từ đơn hàng
- Hỗ trợ thanh toán khi nhận hàng và chuyển khoản ngân hàng
- Trang chuyển khoản hiển thị thông tin tài khoản, nội dung và mã QR cấu hình
- Đơn chờ thanh toán có thời hạn và có thể chuyển sang hết hạn/hủy
- Người dùng xác nhận đã chuyển khoản; Admin xác nhận giao dịch
- Có báo giá để in và xuất thông tin đơn hàng ra Excel

## Slide 14 | Build PC theo linh kiện
- Chọn linh kiện theo từng nhóm thành phần
- Tìm kiếm và sắp xếp sản phẩm trong cửa sổ chọn
- BuildCompatibilityService đưa cảnh báo kiểm tra socket, chuẩn RAM và công suất nguồn
- Tính tổng giá cấu hình và cho phép thay đổi hoặc xóa lựa chọn
- Thêm toàn bộ cấu hình vào giỏ và xuất cấu hình dạng CSV

## Slide 15 | So sánh sản phẩm
- Lưu danh sách mã sản phẩm so sánh trong Session
- Thêm, xóa hoặc xóa toàn bộ danh sách
- Giới hạn tối đa hai sản phẩm
- Hiển thị các thuộc tính sản phẩm theo bảng so sánh
- Cho phép quay lại chi tiết hoặc tiếp tục mua sắm

## Slide 16 | Theo dõi đơn và bảo hành
- Khách tra cứu đơn bằng mã đơn hàng và số điện thoại
- Người dùng đăng nhập xem danh sách và chi tiết đơn của mình
- Trang theo dõi cung cấp trạng thái đơn và trạng thái thanh toán
- Khách hàng đã đăng nhập gửi yêu cầu bảo hành theo sản phẩm đã mua
- Admin xem và cập nhật trạng thái yêu cầu bảo hành

## Slide 17 | Nội dung và phản hồi khách hàng
- Danh sách và trang chi tiết bài viết theo slug
- Admin có thể tạo, sửa và xóa bài viết
- Biểu mẫu liên hệ lưu Feedback của khách hàng
- Có màn hình quản lý phản hồi dành cho Admin
- Banner và cấu hình giao diện được quản lý từ khu vực quản trị

## Slide 18 | Hỗ trợ trực tuyến với SignalR
- Khách tạo cuộc hội thoại bằng thông tin liên hệ
- Access token bảo vệ quyền truy cập hội thoại của khách
- Gửi, nhận và tải lại lịch sử tin nhắn
- Admin xem danh sách hội thoại, phản hồi và đóng cuộc trò chuyện
- ChatHub phát sự kiện cập nhật theo nhóm hội thoại và nhóm Admin

## Slide 19 | Hệ thống quản trị
- Dashboard tổng hợp số liệu người dùng, sản phẩm, đơn hàng và doanh thu
- CRUD sản phẩm, danh mục, banner và người dùng
- Lọc sản phẩm; quản lý ảnh tải lên và ảnh theo URL
- Theo dõi chi tiết đơn, cập nhật trạng thái và xác nhận chuyển khoản
- Quản lý cài đặt trang, chat hỗ trợ và bảo hành

## Slide 20 | Đánh giá và kết luận
- Hệ thống bao phủ luồng bán hàng từ khám phá sản phẩm đến hậu mãi
- Kiến trúc MVC phân tách giao diện, điều phối, nghiệp vụ và dữ liệu
- Điểm mạnh: transaction checkout, giỏ hàng guest/user và tích hợp vận chuyển
- Giới hạn hiện tại: chưa có bộ kiểm thử tự động và pipeline CI/CD trong repository
- Hướng nâng cấp: chuẩn hóa Identity, tăng kiểm thử, logging và khả năng mở rộng
