# Lời thuyết trình — DATN PC Store

> Nội dung khớp với 30 slide được sinh từ source code hiện tại.

## Slide 01 — DATN PC Store

### Lời thuyết trình

Em xin kính chào hội đồng và thầy cô.
Đề tài xây dựng một website thương mại điện tử chuyên PC, laptop và linh kiện.
Điểm nhấn là hành trình mua hàng khép kín, Build PC, so sánh và hỗ trợ sau bán.
Phần trình bày được sắp theo đúng luồng người dùng và các nghiệp vụ quản trị thực tế.

### Ý cần nhấn mạnh
- Website thương mại điện tử chuyên PC
- Demo theo hành trình thực tế

### Ảnh cần chèn
- Không.

## Slide 02 — Bài toán thực tế & cơ hội số hóa

### Lời thuyết trình

Bài toán của cửa hàng PC phức tạp hơn một website bán hàng thông thường.
Khách cần thông tin kỹ thuật, kiểm tra tương thích và một quy trình giao dịch rõ ràng.
Giải pháp gom bốn giai đoạn khám phá, tư vấn, giao dịch và hậu mãi vào cùng hệ thống.
Đây là giá trị xuyên suốt để đánh giá các chức năng ở phần demo.

### Ý cần nhấn mạnh
- Bốn giai đoạn giá trị
- Giải quyết rủi ro tương thích

### Ảnh cần chèn
- Không.

## Slide 03 — Mục tiêu và tiêu chí thành công

### Lời thuyết trình

Mục tiêu trung tâm là tạo hành trình mua PC khép kín chứ không chỉ hiển thị sản phẩm.
Với khách hàng, hệ thống phải hỗ trợ tìm, chọn, mua và theo dõi.
Với cửa hàng, dữ liệu sản phẩm, đơn hàng, nội dung và hỗ trợ cần được quản trị tập trung.
Tiêu chí thành công là chức năng đúng source, giao diện rõ và quy trình có thể demo được.

### Ý cần nhấn mạnh
- Hành trình mua PC khép kín
- Cân bằng khách hàng và vận hành

### Ảnh cần chèn
- Không.

## Slide 04 — Đối tượng sử dụng & hành trình nghiệp vụ

### Lời thuyết trình

Source thể hiện bốn nhóm sử dụng với quyền và dữ liệu khác nhau.
Khách vãng lai vẫn có thể duyệt, so sánh và dùng giỏ hàng session.
Khách đăng nhập có thêm hồ sơ, đơn cá nhân và bảo hành; Staff và Admin xử lý vận hành.
Các slide tiếp theo bám sáu bước của hành trình chính này.

### Ý cần nhấn mạnh
- Bốn nhóm người dùng
- Sáu bước hành trình

### Ảnh cần chèn
- Không.

## Slide 05 — Bản đồ chức năng đã triển khai

### Lời thuyết trình

Bản đồ chức năng được lập sau khi rà soát controller, view, service và migration.
Năm cụm bao phủ từ khám phá sản phẩm đến quản trị hệ thống.
Các chức năng xuất dữ liệu, chat, feedback, bài viết và cấu hình site cũng được đưa vào thay vì chỉ tập trung giỏ hàng.
Đây là phạm vi thực tế của deck, không bổ sung chức năng chưa có trong code.

### Ý cần nhấn mạnh
- Năm cụm chức năng thực tế
- Không bỏ sót module quản trị

### Ảnh cần chèn
- Không.

## Slide 06 — Kiến trúc xử lý ASP.NET Core MVC

### Lời thuyết trình

Hệ thống sử dụng kiến trúc MVC quen thuộc của ASP.NET Core.
Controller điều phối request; service đóng gói giỏ hàng, xác thực, so sánh, vận chuyển và tương thích.
EF Core làm việc với SQL Server, còn SignalR, GHN, SMTP và QR là các tích hợp theo nghiệp vụ.
Session được dùng cho trải nghiệm chưa đăng nhập như cart, compare và Build PC.

### Ý cần nhấn mạnh
- Phân lớp rõ trách nhiệm
- Tích hợp được service hóa

### Ảnh cần chèn
- Không.

## Slide 07 — Công nghệ, tích hợp & kiểm soát truy cập

### Lời thuyết trình

Stack chính là .NET 8, EF Core 8, SQL Server và giao diện Razor kết hợp JavaScript.
Kiểm soát truy cập dựa trên cookie, role Admin hoặc Staff và anti-forgery cho thao tác thay đổi dữ liệu.
Quên mật khẩu dùng OTP email có thời hạn, chat khách dùng access token riêng.
Các tích hợp đều phục vụ chức năng đã có thay vì trình diễn công nghệ đơn lẻ.

### Ý cần nhấn mạnh
- Role và anti-forgery
- GHN, SMTP, QR, SignalR

### Ảnh cần chèn
- Không.

## Slide 08 — Mô hình dữ liệu theo miền nghiệp vụ

### Lời thuyết trình

Thay vì đưa ERD dày đặc, dữ liệu được nhóm thành năm miền dễ theo dõi.
Identity quản lý tài khoản và OTP; Catalog quản lý dữ liệu hiển thị.
Commerce lưu giỏ và đơn; Support lưu bảo hành, feedback và chat; Config lưu cấu hình site, vận chuyển và Build PC.
DbContext hiện có 22 DbSet, phản ánh phạm vi nghiệp vụ tương đối đầy đủ.

### Ý cần nhấn mạnh
- Năm miền dữ liệu
- 22 DbSet thực tế

### Ảnh cần chèn
- Không.

## Slide 09 — Hành trình trải nghiệm khách hàng

### Lời thuyết trình

Phần demo được sắp theo hành trình thực tế thay vì theo tên controller.
Khách bắt đầu từ trang chủ, thu hẹp lựa chọn ở catalog và đánh giá ở trang chi tiết.
Build PC hoặc so sánh hỗ trợ quyết định trước khi vào giỏ và checkout.
Sau giao dịch, hệ thống tiếp tục bằng theo dõi, chat, nội dung và bảo hành.

### Ý cần nhấn mạnh
- Demo theo thứ tự sử dụng
- Không tách rời hậu mãi

### Ảnh cần chèn
- Không.

## Slide 10 — Trang chủ — điểm vào hành trình mua sắm

### Lời thuyết trình

Trang chủ là điểm tập trung nội dung bán hàng quan trọng nhất.
Banner, danh mục và các section sản phẩm dẫn người dùng tới đúng nhóm nhu cầu.
Nội dung hiển thị lấy từ dữ liệu banner, category, product và site setting.
Quản trị viên có thể thay đổi hình ảnh và background mà không sửa view.

### Ý cần nhấn mạnh
- Banner và danh mục động
- Điểm vào các nhóm sản phẩm

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/Home/Index.cshtml.

## Slide 11 — Danh sách sản phẩm — tìm kiếm và thu hẹp lựa chọn

### Lời thuyết trình

Trang danh sách hỗ trợ tìm kiếm, lọc và sắp xếp để giảm số lựa chọn.
Bộ lọc dùng category, khoảng giá và trạng thái liên quan đến tồn kho hoặc khuyến mãi.
Mỗi card thể hiện giá, giá giảm và điểm vào chi tiết hoặc so sánh.
Đây là màn hình nối giữa nhu cầu chung và quyết định ở từng sản phẩm.

### Ý cần nhấn mạnh
- Tìm · lọc · sắp xếp
- Hiển thị tồn kho và khuyến mãi

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/Products/Index.cshtml.

## Slide 12 — Chi tiết sản phẩm — đủ dữ liệu để ra quyết định

### Lời thuyết trình

Trang chi tiết tập trung toàn bộ dữ liệu cần cho quyết định mua.
Ngoài hình ảnh, giá và tồn kho, view còn hiển thị thông số kỹ thuật và bảo hành.
Người dùng có thể thêm giỏ, mua ngay, xem sản phẩm mua kèm và sản phẩm liên quan.
Nút so sánh tiếp tục đưa sản phẩm vào quy trình đánh giá cạnh nhau.

### Ý cần nhấn mạnh
- Thông số kỹ thuật rõ ràng
- Mua kèm và sản phẩm liên quan

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/Products/Detail.cshtml.

## Slide 13 — So sánh sản phẩm — đối chiếu tối đa 2 lựa chọn

### Lời thuyết trình

So sánh giúp khách chuyển từ cảm nhận sang đối chiếu dữ liệu.
Hệ thống giới hạn hai sản phẩm để bảng không quá rộng và dễ đọc trên nhiều màn hình.
Lựa chọn được lưu trong session nên khách vãng lai vẫn sử dụng được.
Các dòng so sánh bao phủ giá và các thông số PC quan trọng.

### Ý cần nhấn mạnh
- Tối đa hai sản phẩm
- Lưu lựa chọn trong session

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/Compare/Index.cshtml.

## Slide 14 — Build PC — chọn linh kiện và kiểm tra tương thích

### Lời thuyết trình

Build PC là chức năng nổi bật nhất về tư vấn kỹ thuật.
Người dùng chọn linh kiện theo chín nhóm; trạng thái cấu hình được giữ trong session.
BuildCompatibilityService kiểm tra các quy tắc cơ bản như socket, RAM và công suất.
Khi hoàn tất, khách có thể thêm toàn bộ linh kiện vào giỏ hoặc xuất cấu hình CSV.

### Ý cần nhấn mạnh
- Chín nhóm linh kiện
- Kiểm tra tương thích và xuất CSV

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/BuildPc/Index.cshtml.

## Slide 15 — Tài khoản — định danh, hồ sơ và khôi phục mật khẩu

### Lời thuyết trình

Module tài khoản không chỉ có đăng nhập và đăng ký.
Người dùng có thể cập nhật hồ sơ, đổi mật khẩu và truy cập đơn hàng cá nhân.
Luồng quên mật khẩu tạo OTP, gửi qua email, xác minh thời hạn rồi mới cho đặt mật khẩu mới.
Cookie authentication và role được dùng để phân tách customer, staff và admin.

### Ý cần nhấn mạnh
- OTP email có thời hạn
- Hồ sơ và đổi mật khẩu

### Ảnh cần chèn
- Không.

## Slide 16 — Giỏ hàng — hợp nhất khách vãng lai và người đăng nhập

### Lời thuyết trình

Giỏ hàng hỗ trợ cả khách vãng lai và người dùng đã đăng nhập.
Khách dùng session; tài khoản dùng Cart và CartItem trong database.
Các thao tác gồm thêm, mua ngay, cập nhật số lượng, xóa từng dòng hoặc làm trống.
Tổng tiền được tính lại trước khi chuyển sang checkout.

### Ý cần nhấn mạnh
- Session và database
- Đầy đủ thao tác giỏ hàng

### Ảnh cần chèn
- Có. Khung giao diện vector dựa trên Views/Cart/Index.cshtml.

## Slide 17 — Checkout — địa chỉ GHN và phí vận chuyển

### Lời thuyết trình

Checkout thu thập đầy đủ người nhận, liên hệ và địa chỉ giao hàng.
Các danh sách tỉnh, quận và phường được tải qua dịch vụ địa chỉ GHN.
API shipping nhận địa chỉ cùng giỏ hàng để tính phí và thời gian dự kiến theo chính sách cấu hình.
Cuối luồng, khách chọn COD hoặc chuyển khoản.

### Ý cần nhấn mạnh
- GHN tỉnh–quận–phường
- Tính phí từ địa chỉ và giỏ hàng

### Ảnh cần chèn
- Có. Nội dung dựa trên Views/Orders/Checkout.cshtml và ShippingController.

## Slide 18 — Thanh toán — COD và chuyển khoản QR có thời hạn

### Lời thuyết trình

Hệ thống triển khai hai phương thức thanh toán với trạng thái đơn khác nhau.
COD tạo đơn chờ xác nhận và không yêu cầu thanh toán trước.
Chuyển khoản hiển thị QR, nội dung theo mã đơn và thời hạn thanh toán hai giờ.
Khách xác nhận đã chuyển; quản trị viên kiểm tra và xác nhận giao dịch trong màn hình đơn.

### Ý cần nhấn mạnh
- Hai phương thức thanh toán
- QR và thời hạn hai giờ

### Ảnh cần chèn
- Có. Nội dung dựa trên Views/Orders/BankTransfer.cshtml.

## Slide 19 — Đơn hàng — theo dõi, lịch sử và xuất chứng từ

### Lời thuyết trình

Sau checkout, khách có thể theo dõi tiến trình đơn bằng mã đơn và số điện thoại.
Người đăng nhập có danh sách My Orders và trang chi tiết riêng.
Đơn chờ thanh toán có thể mở lại luồng thanh toán QR.
Hệ thống còn tạo trang báo giá và xuất Excel từ dữ liệu đơn, phục vụ trao đổi hoặc lưu trữ.

### Ý cần nhấn mạnh
- Tra cứu không cần đăng nhập
- Báo giá và xuất Excel

### Ảnh cần chèn
- Có. Nội dung dựa trên nhóm Views/Orders.

## Slide 20 — Hỗ trợ realtime — chat khách hàng và feedback

### Lời thuyết trình

Hệ thống có hai kênh tiếp nhận hỗ trợ bổ sung cho nhau.
Chat dùng SignalR để cập nhật realtime, lưu conversation và message trong database.
Khách vãng lai được cấp token truy cập hội thoại; tài khoản có thể gắn với user.
Feedback là kênh không đồng bộ và được Staff hoặc Admin xem trong màn hình quản lý.

### Ý cần nhấn mạnh
- SignalR realtime
- Chat và feedback tách mục đích

### Ảnh cần chèn
- Có. Nội dung dựa trên _SupportChatBox, SupportChatController và ContactController.

## Slide 21 — Hậu mãi & nội dung — bảo hành, tin tức, khuyến mãi

### Lời thuyết trình

Hậu mãi gồm cả xử lý sự cố sản phẩm và nội dung duy trì quan hệ khách hàng.
Khách đăng nhập có thể tạo yêu cầu bảo hành cho sản phẩm và mô tả vấn đề.
AdminWarranty theo dõi và cập nhật trạng thái xử lý.
Module Article cung cấp tin công nghệ, hướng dẫn hoặc khuyến mãi với trang danh sách và chi tiết theo slug.

### Ý cần nhấn mạnh
- Bảo hành có trạng thái
- Article theo slug

### Ảnh cần chèn
- Không.

## Slide 22 — Quản trị hệ thống — trung tâm vận hành cửa hàng

### Lời thuyết trình

Phần quản trị được xem như trung tâm vận hành, không chỉ là vài bảng CRUD.
Dashboard tổng hợp bốn KPI thật từ AdminDashboardVm: sản phẩm, đơn, người dùng và bảo hành.
Menu quản trị chia thành catalog, commerce, identity, content và support.
Các slide tiếp theo trình bày từng cụm theo mức độ ảnh hưởng đến vận hành.

### Ý cần nhấn mạnh
- Bốn KPI từ ViewModel
- Năm cụm quản trị

### Ảnh cần chèn
- Có. Nội dung dựa trên Views/AdminDashboard/Index.cshtml.

## Slide 23 — Quản trị catalog — sản phẩm, ảnh và danh mục

### Lời thuyết trình

Quản trị catalog bao phủ vòng đời sản phẩm và cấu trúc danh mục.
Form sản phẩm hỗ trợ thông tin bán hàng, tồn kho, trạng thái, mô tả, thông số và bảo hành.
ProductImageStorageService xử lý thumbnail và nhiều ảnh sản phẩm.
Danh mục có tên và icon, đồng thời là cơ sở cho điều hướng và bộ lọc phía khách hàng.

### Ý cần nhấn mạnh
- Đầy đủ dữ liệu sản phẩm
- Thumbnail và gallery

### Ảnh cần chèn
- Có. Nội dung dựa trên AdminProducts và AdminCategories.

## Slide 24 — Quản trị đơn hàng — xử lý trạng thái và thanh toán

### Lời thuyết trình

Quản trị đơn hàng là nghiệp vụ quan trọng nhất sau catalog.
Nhân viên xem danh sách, mở chi tiết, kiểm tra địa chỉ, sản phẩm, phí và cập nhật trạng thái.
Với chuyển khoản, trạng thái tách rõ chờ thanh toán, chờ xác nhận, đã thanh toán và hết hạn.
OrderExpirationService hỗ trợ tự động xử lý đơn quá thời hạn.

### Ý cần nhấn mạnh
- Quy trình trạng thái rõ ràng
- Xác nhận chuyển khoản và hết hạn

### Ảnh cần chèn
- Không.

## Slide 25 — Quản trị tài khoản — vai trò và trạng thái hoạt động

### Lời thuyết trình

Admin có màn hình riêng để quản lý tài khoản và vai trò.
Ba role chính là Admin, Staff và Customer; quyền controller sử dụng Authorize theo role.
Form cho phép tạo hoặc cập nhật thông tin, chọn role và trạng thái hoạt động.
Khả năng khóa tài khoản giúp kiểm soát truy cập mà không phải xóa lịch sử liên quan.

### Ý cần nhấn mạnh
- Ba role thực tế
- Khóa tài khoản không mất lịch sử

### Ảnh cần chèn
- Có. Nội dung dựa trên Views/AdminUsers.

## Slide 26 — Quản trị nội dung — banner, bài viết và giao diện website

### Lời thuyết trình

Nhóm quản trị nội dung giúp website thay đổi hình ảnh và thông tin mà không sửa code.
Banner có vị trí, thứ tự, link và trạng thái hiển thị.
Site Settings quản lý logo, tên site và background cho các section khuyến mãi.
Articles hỗ trợ tạo, sửa, xóa và hiển thị nội dung theo slug.

### Ý cần nhấn mạnh
- Banner có vị trí và thứ tự
- Logo và background thay đổi động

### Ảnh cần chèn
- Không.

## Slide 27 — Trung tâm hỗ trợ — chat, feedback và bảo hành

### Lời thuyết trình

Admin Chat hiển thị danh sách hội thoại và nội dung trao đổi theo thời gian thực.
Staff hoặc Admin có thể chọn conversation, gửi phản hồi và theo dõi lịch sử.
Feedback và bảo hành được quản lý ở các màn hình riêng vì quy trình xử lý khác chat.
Ba kênh này tạo trung tâm hỗ trợ sau bán đầy đủ hơn cho cửa hàng.

### Ý cần nhấn mạnh
- Admin chat realtime
- Ba kênh hỗ trợ bổ sung nhau

### Ảnh cần chèn
- Có. Nội dung dựa trên AdminChat, Contact/Manage và AdminWarranty.

## Slide 28 — Kết quả đạt được & chất lượng triển khai

### Lời thuyết trình

Kết quả được đo từ chính cấu trúc source hiện tại.
Dự án có 21 controller, 22 DbSet và 28 file service.
Luồng bán hàng, tư vấn, quản trị và hậu mãi đã được kết nối thành một hệ thống.
Tuy nhiên thanh toán, Build PC, báo cáo và kiểm thử vẫn còn khoảng trống cần phát triển.

### Ý cần nhấn mạnh
- Kết quả định lượng từ source
- Nêu rõ giới hạn hiện tại

### Ảnh cần chèn
- Không.

## Slide 29 — Hướng phát triển — từ đồ án đến sản phẩm vận hành

### Lời thuyết trình

Lộ trình phát triển bắt đầu từ điểm ảnh hưởng trực tiếp đến vận hành.
Ưu tiên đầu tiên là cổng thanh toán và webhook để tự động đối soát.
Sau đó nâng Build PC bằng benchmark, dữ liệu tương thích và gợi ý tốt hơn.
Analytics, test, monitoring, mobile và SEO giúp hệ thống tiến gần một sản phẩm thương mại thực tế.

### Ý cần nhấn mạnh
- Ưu tiên tự động hóa giao dịch
- Nâng tư vấn và đo lường

### Ảnh cần chèn
- Không.

## Slide 30 — Xin chân thành cảm ơn

### Lời thuyết trình

Phần trình bày của em xin kết thúc tại đây.
Em xin cảm ơn hội đồng và thầy cô đã lắng nghe.
Em sẵn sàng demo lại bất kỳ luồng chức năng nào trong hệ thống.
Em mong nhận được câu hỏi và góp ý để tiếp tục hoàn thiện sản phẩm.

### Ý cần nhấn mạnh
- Cảm ơn hội đồng
- Sẵn sàng demo và trao đổi

### Ảnh cần chèn
- Không.
