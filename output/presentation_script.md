# Kịch bản thuyết trình rút gọn

## Slide 01 — DATN PC Store
Em xin kính chào hội đồng và thầy cô. Đề tài xây dựng một website thương mại điện tử chuyên PC, laptop và linh kiện. Điểm nhấn là hành trình mua hàng khép kín, Build PC, so sánh và hỗ trợ sau bán. Phần trình bày được sắp theo đúng luồng người dùng và các nghiệp vụ quản trị thực tế.

## Slide 02 — Bài toán thực tế & cơ hội số hóa
Bài toán của cửa hàng PC phức tạp hơn một website bán hàng thông thường. Khách cần thông tin kỹ thuật, kiểm tra tương thích và một quy trình giao dịch rõ ràng. Giải pháp gom bốn giai đoạn khám phá, tư vấn, giao dịch và hậu mãi vào cùng hệ thống. Đây là giá trị xuyên suốt để đánh giá các chức năng ở phần demo.

## Slide 03 — Mục tiêu và tiêu chí thành công
Mục tiêu trung tâm là tạo hành trình mua PC khép kín chứ không chỉ hiển thị sản phẩm. Với khách hàng, hệ thống phải hỗ trợ tìm, chọn, mua và theo dõi. Với cửa hàng, dữ liệu sản phẩm, đơn hàng, nội dung và hỗ trợ cần được quản trị tập trung. Tiêu chí thành công là chức năng đúng source, giao diện rõ và quy trình có thể demo được.

## Slide 04 — Đối tượng sử dụng & hành trình nghiệp vụ
Source thể hiện bốn nhóm sử dụng với quyền và dữ liệu khác nhau. Khách vãng lai vẫn có thể duyệt, so sánh và dùng giỏ hàng session. Khách đăng nhập có thêm hồ sơ, đơn cá nhân và bảo hành; Staff và Admin xử lý vận hành. Các slide tiếp theo bám sáu bước của hành trình chính này.

## Slide 05 — Bản đồ chức năng đã triển khai
Bản đồ chức năng được lập sau khi rà soát controller, view, service và migration. Năm cụm bao phủ từ khám phá sản phẩm đến quản trị hệ thống. Các chức năng xuất dữ liệu, chat, feedback, bài viết và cấu hình site cũng được đưa vào thay vì chỉ tập trung giỏ hàng. Đây là phạm vi thực tế của deck, không bổ sung chức năng chưa có trong code.

## Slide 06 — Kiến trúc xử lý ASP.NET Core MVC
Hệ thống sử dụng kiến trúc MVC quen thuộc của ASP.NET Core. Controller điều phối request; service đóng gói giỏ hàng, xác thực, so sánh, vận chuyển và tương thích. EF Core làm việc với SQL Server, còn SignalR, GHN, SMTP và QR là các tích hợp theo nghiệp vụ. Session được dùng cho trải nghiệm chưa đăng nhập như cart, compare và Build PC.

## Slide 07 — Công nghệ, tích hợp & kiểm soát truy cập
Stack chính là .NET 8, EF Core 8, SQL Server và giao diện Razor kết hợp JavaScript. Kiểm soát truy cập dựa trên cookie, role Admin hoặc Staff và anti-forgery cho thao tác thay đổi dữ liệu. Quên mật khẩu dùng OTP email có thời hạn, chat khách dùng access token riêng. Các tích hợp đều phục vụ chức năng đã có thay vì trình diễn công nghệ đơn lẻ.

## Slide 08 — Mô hình dữ liệu theo miền nghiệp vụ
Thay vì đưa ERD dày đặc, dữ liệu được nhóm thành năm miền dễ theo dõi. Identity quản lý tài khoản và OTP; Catalog quản lý dữ liệu hiển thị. Commerce lưu giỏ và đơn; Support lưu bảo hành, feedback và chat; Config lưu cấu hình site, vận chuyển và Build PC. DbContext hiện có 22 DbSet, phản ánh phạm vi nghiệp vụ tương đối đầy đủ.

## Slide 09 — Hành trình trải nghiệm khách hàng
Phần demo được sắp theo hành trình thực tế thay vì theo tên controller. Khách bắt đầu từ trang chủ, thu hẹp lựa chọn ở catalog và đánh giá ở trang chi tiết. Build PC hoặc so sánh hỗ trợ quyết định trước khi vào giỏ và checkout. Sau giao dịch, hệ thống tiếp tục bằng theo dõi, chat, nội dung và bảo hành.

## Slide 10 — Trang chủ — điểm vào hành trình mua sắm
Trang chủ là điểm tập trung nội dung bán hàng quan trọng nhất. Banner, danh mục và các section sản phẩm dẫn người dùng tới đúng nhóm nhu cầu. Nội dung hiển thị lấy từ dữ liệu banner, category, product và site setting. Quản trị viên có thể thay đổi hình ảnh và background mà không sửa view.

## Slide 11 — Danh sách sản phẩm — tìm kiếm và thu hẹp lựa chọn
Trang danh sách hỗ trợ tìm kiếm, lọc và sắp xếp để giảm số lựa chọn. Bộ lọc dùng category, khoảng giá và trạng thái liên quan đến tồn kho hoặc khuyến mãi. Mỗi card thể hiện giá, giá giảm và điểm vào chi tiết hoặc so sánh. Đây là màn hình nối giữa nhu cầu chung và quyết định ở từng sản phẩm.

## Slide 12 — Chi tiết sản phẩm — đủ dữ liệu để ra quyết định
Trang chi tiết tập trung toàn bộ dữ liệu cần cho quyết định mua. Ngoài hình ảnh, giá và tồn kho, view còn hiển thị thông số kỹ thuật và bảo hành. Người dùng có thể thêm giỏ, mua ngay, xem sản phẩm mua kèm và sản phẩm liên quan. Nút so sánh tiếp tục đưa sản phẩm vào quy trình đánh giá cạnh nhau.

## Slide 13 — So sánh sản phẩm — đối chiếu tối đa 2 lựa chọn
So sánh giúp khách chuyển từ cảm nhận sang đối chiếu dữ liệu. Hệ thống giới hạn hai sản phẩm để bảng không quá rộng và dễ đọc trên nhiều màn hình. Lựa chọn được lưu trong session nên khách vãng lai vẫn sử dụng được. Các dòng so sánh bao phủ giá và các thông số PC quan trọng.

## Slide 14 — Build PC — chọn linh kiện và kiểm tra tương thích
Build PC là chức năng nổi bật nhất về tư vấn kỹ thuật. Người dùng chọn linh kiện theo chín nhóm; trạng thái cấu hình được giữ trong session. BuildCompatibilityService kiểm tra các quy tắc cơ bản như socket, RAM và công suất. Khi hoàn tất, khách có thể thêm toàn bộ linh kiện vào giỏ hoặc xuất cấu hình CSV.

## Slide 15 — Tài khoản — định danh, hồ sơ và khôi phục mật khẩu
Module tài khoản không chỉ có đăng nhập và đăng ký. Người dùng có thể cập nhật hồ sơ, đổi mật khẩu và truy cập đơn hàng cá nhân. Luồng quên mật khẩu tạo OTP, gửi qua email, xác minh thời hạn rồi mới cho đặt mật khẩu mới. Cookie authentication và role được dùng để phân tách customer, staff và admin.

## Slide 16 — Giỏ hàng — hợp nhất khách vãng lai và người đăng nhập
Giỏ hàng hỗ trợ cả khách vãng lai và người dùng đã đăng nhập. Khách dùng session; tài khoản dùng Cart và CartItem trong database. Các thao tác gồm thêm, mua ngay, cập nhật số lượng, xóa từng dòng hoặc làm trống. Tổng tiền được tính lại trước khi chuyển sang checkout.

## Slide 17 — Checkout — địa chỉ GHN và phí vận chuyển
Checkout thu thập đầy đủ người nhận, liên hệ và địa chỉ giao hàng. Các danh sách tỉnh, quận và phường được tải qua dịch vụ địa chỉ GHN. API shipping nhận địa chỉ cùng giỏ hàng để tính phí và thời gian dự kiến theo chính sách cấu hình. Cuối luồng, khách chọn COD hoặc chuyển khoản.

## Slide 18 — Thanh toán — COD và chuyển khoản QR có thời hạn
Hệ thống triển khai hai phương thức thanh toán với trạng thái đơn khác nhau. COD tạo đơn chờ xác nhận và không yêu cầu thanh toán trước. Chuyển khoản hiển thị QR, nội dung theo mã đơn và thời hạn thanh toán hai giờ. Khách xác nhận đã chuyển; quản trị viên kiểm tra và xác nhận giao dịch trong màn hình đơn.

## Slide 19 — Đơn hàng — theo dõi, lịch sử và xuất chứng từ
Sau checkout, khách có thể theo dõi tiến trình đơn bằng mã đơn và số điện thoại. Người đăng nhập có danh sách My Orders và trang chi tiết riêng. Đơn chờ thanh toán có thể mở lại luồng thanh toán QR. Hệ thống còn tạo trang báo giá và xuất Excel từ dữ liệu đơn, phục vụ trao đổi hoặc lưu trữ.

## Slide 20 — Hỗ trợ realtime — chat khách hàng và feedback
Hệ thống có hai kênh tiếp nhận hỗ trợ bổ sung cho nhau. Chat dùng SignalR để cập nhật realtime, lưu conversation và message trong database. Khách vãng lai được cấp token truy cập hội thoại; tài khoản có thể gắn với user. Feedback là kênh không đồng bộ và được Staff hoặc Admin xem trong màn hình quản lý.

## Slide 21 — Hậu mãi & nội dung — bảo hành, tin tức, khuyến mãi
Hậu mãi gồm cả xử lý sự cố sản phẩm và nội dung duy trì quan hệ khách hàng. Khách đăng nhập có thể tạo yêu cầu bảo hành cho sản phẩm và mô tả vấn đề. AdminWarranty theo dõi và cập nhật trạng thái xử lý. Module Article cung cấp tin công nghệ, hướng dẫn hoặc khuyến mãi với trang danh sách và chi tiết theo slug.

## Slide 22 — Quản trị hệ thống — trung tâm vận hành cửa hàng
Phần quản trị được xem như trung tâm vận hành, không chỉ là vài bảng CRUD. Dashboard tổng hợp bốn KPI thật từ AdminDashboardVm: sản phẩm, đơn, người dùng và bảo hành. Menu quản trị chia thành catalog, commerce, identity, content và support. Các slide tiếp theo trình bày từng cụm theo mức độ ảnh hưởng đến vận hành.

## Slide 23 — Quản trị catalog — sản phẩm, ảnh và danh mục
Quản trị catalog bao phủ vòng đời sản phẩm và cấu trúc danh mục. Form sản phẩm hỗ trợ thông tin bán hàng, tồn kho, trạng thái, mô tả, thông số và bảo hành. ProductImageStorageService xử lý thumbnail và nhiều ảnh sản phẩm. Danh mục có tên và icon, đồng thời là cơ sở cho điều hướng và bộ lọc phía khách hàng.

## Slide 24 — Quản trị đơn hàng — xử lý trạng thái và thanh toán
Quản trị đơn hàng là nghiệp vụ quan trọng nhất sau catalog. Nhân viên xem danh sách, mở chi tiết, kiểm tra địa chỉ, sản phẩm, phí và cập nhật trạng thái. Với chuyển khoản, trạng thái tách rõ chờ thanh toán, chờ xác nhận, đã thanh toán và hết hạn. OrderExpirationService hỗ trợ tự động xử lý đơn quá thời hạn.

## Slide 25 — Quản trị tài khoản — vai trò và trạng thái hoạt động
Admin có màn hình riêng để quản lý tài khoản và vai trò. Ba role chính là Admin, Staff và Customer; quyền controller sử dụng Authorize theo role. Form cho phép tạo hoặc cập nhật thông tin, chọn role và trạng thái hoạt động. Khả năng khóa tài khoản giúp kiểm soát truy cập mà không phải xóa lịch sử liên quan.

## Slide 26 — Quản trị nội dung — banner, bài viết và giao diện website
Nhóm quản trị nội dung giúp website thay đổi hình ảnh và thông tin mà không sửa code. Banner có vị trí, thứ tự, link và trạng thái hiển thị. Site Settings quản lý logo, tên site và background cho các section khuyến mãi. Articles hỗ trợ tạo, sửa, xóa và hiển thị nội dung theo slug.

## Slide 27 — Trung tâm hỗ trợ — chat, feedback và bảo hành
Admin Chat hiển thị danh sách hội thoại và nội dung trao đổi theo thời gian thực. Staff hoặc Admin có thể chọn conversation, gửi phản hồi và theo dõi lịch sử. Feedback và bảo hành được quản lý ở các màn hình riêng vì quy trình xử lý khác chat. Ba kênh này tạo trung tâm hỗ trợ sau bán đầy đủ hơn cho cửa hàng.

## Slide 28 — Kết quả đạt được & chất lượng triển khai
Kết quả được đo từ chính cấu trúc source hiện tại. Dự án có 21 controller, 22 DbSet và 28 file service. Luồng bán hàng, tư vấn, quản trị và hậu mãi đã được kết nối thành một hệ thống. Tuy nhiên thanh toán, Build PC, báo cáo và kiểm thử vẫn còn khoảng trống cần phát triển.

## Slide 29 — Hướng phát triển — từ đồ án đến sản phẩm vận hành
Lộ trình phát triển bắt đầu từ điểm ảnh hưởng trực tiếp đến vận hành. Ưu tiên đầu tiên là cổng thanh toán và webhook để tự động đối soát. Sau đó nâng Build PC bằng benchmark, dữ liệu tương thích và gợi ý tốt hơn. Analytics, test, monitoring, mobile và SEO giúp hệ thống tiến gần một sản phẩm thương mại thực tế.

## Slide 30 — Xin chân thành cảm ơn
Phần trình bày của em xin kết thúc tại đây. Em xin cảm ơn hội đồng và thầy cô đã lắng nghe. Em sẵn sàng demo lại bất kỳ luồng chức năng nào trong hệ thống. Em mong nhận được câu hỏi và góp ý để tiếp tục hoàn thiện sản phẩm.
