# DATN PC Store — Dàn ý PowerPoint 30 slide

> Nội dung sinh cùng file PowerPoint và đã đối chiếu source code.

## Slide 01 — DATN PC Store
- Mục đích: Em xin kính chào hội đồng và thầy cô.
- Điểm nhấn: Website thương mại điện tử chuyên PC; Demo theo hành trình thực tế

## Slide 02 — Bài toán thực tế & cơ hội số hóa
- Mục đích: Bài toán của cửa hàng PC phức tạp hơn một website bán hàng thông thường.
- Điểm nhấn: Bốn giai đoạn giá trị; Giải quyết rủi ro tương thích

## Slide 03 — Mục tiêu và tiêu chí thành công
- Mục đích: Mục tiêu trung tâm là tạo hành trình mua PC khép kín chứ không chỉ hiển thị sản phẩm.
- Điểm nhấn: Hành trình mua PC khép kín; Cân bằng khách hàng và vận hành

## Slide 04 — Đối tượng sử dụng & hành trình nghiệp vụ
- Mục đích: Source thể hiện bốn nhóm sử dụng với quyền và dữ liệu khác nhau.
- Điểm nhấn: Bốn nhóm người dùng; Sáu bước hành trình

## Slide 05 — Bản đồ chức năng đã triển khai
- Mục đích: Bản đồ chức năng được lập sau khi rà soát controller, view, service và migration.
- Điểm nhấn: Năm cụm chức năng thực tế; Không bỏ sót module quản trị

## Slide 06 — Kiến trúc xử lý ASP.NET Core MVC
- Mục đích: Hệ thống sử dụng kiến trúc MVC quen thuộc của ASP.NET Core.
- Điểm nhấn: Phân lớp rõ trách nhiệm; Tích hợp được service hóa

## Slide 07 — Công nghệ, tích hợp & kiểm soát truy cập
- Mục đích: Stack chính là .NET 8, EF Core 8, SQL Server và giao diện Razor kết hợp JavaScript.
- Điểm nhấn: Role và anti-forgery; GHN, SMTP, QR, SignalR

## Slide 08 — Mô hình dữ liệu theo miền nghiệp vụ
- Mục đích: Thay vì đưa ERD dày đặc, dữ liệu được nhóm thành năm miền dễ theo dõi.
- Điểm nhấn: Năm miền dữ liệu; 22 DbSet thực tế

## Slide 09 — Hành trình trải nghiệm khách hàng
- Mục đích: Phần demo được sắp theo hành trình thực tế thay vì theo tên controller.
- Điểm nhấn: Demo theo thứ tự sử dụng; Không tách rời hậu mãi

## Slide 10 — Trang chủ — điểm vào hành trình mua sắm
- Mục đích: Trang chủ là điểm tập trung nội dung bán hàng quan trọng nhất.
- Điểm nhấn: Banner và danh mục động; Điểm vào các nhóm sản phẩm

## Slide 11 — Danh sách sản phẩm — tìm kiếm và thu hẹp lựa chọn
- Mục đích: Trang danh sách hỗ trợ tìm kiếm, lọc và sắp xếp để giảm số lựa chọn.
- Điểm nhấn: Tìm · lọc · sắp xếp; Hiển thị tồn kho và khuyến mãi

## Slide 12 — Chi tiết sản phẩm — đủ dữ liệu để ra quyết định
- Mục đích: Trang chi tiết tập trung toàn bộ dữ liệu cần cho quyết định mua.
- Điểm nhấn: Thông số kỹ thuật rõ ràng; Mua kèm và sản phẩm liên quan

## Slide 13 — So sánh sản phẩm — đối chiếu tối đa 2 lựa chọn
- Mục đích: So sánh giúp khách chuyển từ cảm nhận sang đối chiếu dữ liệu.
- Điểm nhấn: Tối đa hai sản phẩm; Lưu lựa chọn trong session

## Slide 14 — Build PC — chọn linh kiện và kiểm tra tương thích
- Mục đích: Build PC là chức năng nổi bật nhất về tư vấn kỹ thuật.
- Điểm nhấn: Chín nhóm linh kiện; Kiểm tra tương thích và xuất CSV

## Slide 15 — Tài khoản — định danh, hồ sơ và khôi phục mật khẩu
- Mục đích: Module tài khoản không chỉ có đăng nhập và đăng ký.
- Điểm nhấn: OTP email có thời hạn; Hồ sơ và đổi mật khẩu

## Slide 16 — Giỏ hàng — hợp nhất khách vãng lai và người đăng nhập
- Mục đích: Giỏ hàng hỗ trợ cả khách vãng lai và người dùng đã đăng nhập.
- Điểm nhấn: Session và database; Đầy đủ thao tác giỏ hàng

## Slide 17 — Checkout — địa chỉ GHN và phí vận chuyển
- Mục đích: Checkout thu thập đầy đủ người nhận, liên hệ và địa chỉ giao hàng.
- Điểm nhấn: GHN tỉnh–quận–phường; Tính phí từ địa chỉ và giỏ hàng

## Slide 18 — Thanh toán — COD và chuyển khoản QR có thời hạn
- Mục đích: Hệ thống triển khai hai phương thức thanh toán với trạng thái đơn khác nhau.
- Điểm nhấn: Hai phương thức thanh toán; QR và thời hạn hai giờ

## Slide 19 — Đơn hàng — theo dõi, lịch sử và xuất chứng từ
- Mục đích: Sau checkout, khách có thể theo dõi tiến trình đơn bằng mã đơn và số điện thoại.
- Điểm nhấn: Tra cứu không cần đăng nhập; Báo giá và xuất Excel

## Slide 20 — Hỗ trợ realtime — chat khách hàng và feedback
- Mục đích: Hệ thống có hai kênh tiếp nhận hỗ trợ bổ sung cho nhau.
- Điểm nhấn: SignalR realtime; Chat và feedback tách mục đích

## Slide 21 — Hậu mãi & nội dung — bảo hành, tin tức, khuyến mãi
- Mục đích: Hậu mãi gồm cả xử lý sự cố sản phẩm và nội dung duy trì quan hệ khách hàng.
- Điểm nhấn: Bảo hành có trạng thái; Article theo slug

## Slide 22 — Quản trị hệ thống — trung tâm vận hành cửa hàng
- Mục đích: Phần quản trị được xem như trung tâm vận hành, không chỉ là vài bảng CRUD.
- Điểm nhấn: Bốn KPI từ ViewModel; Năm cụm quản trị

## Slide 23 — Quản trị catalog — sản phẩm, ảnh và danh mục
- Mục đích: Quản trị catalog bao phủ vòng đời sản phẩm và cấu trúc danh mục.
- Điểm nhấn: Đầy đủ dữ liệu sản phẩm; Thumbnail và gallery

## Slide 24 — Quản trị đơn hàng — xử lý trạng thái và thanh toán
- Mục đích: Quản trị đơn hàng là nghiệp vụ quan trọng nhất sau catalog.
- Điểm nhấn: Quy trình trạng thái rõ ràng; Xác nhận chuyển khoản và hết hạn

## Slide 25 — Quản trị tài khoản — vai trò và trạng thái hoạt động
- Mục đích: Admin có màn hình riêng để quản lý tài khoản và vai trò.
- Điểm nhấn: Ba role thực tế; Khóa tài khoản không mất lịch sử

## Slide 26 — Quản trị nội dung — banner, bài viết và giao diện website
- Mục đích: Nhóm quản trị nội dung giúp website thay đổi hình ảnh và thông tin mà không sửa code.
- Điểm nhấn: Banner có vị trí và thứ tự; Logo và background thay đổi động

## Slide 27 — Trung tâm hỗ trợ — chat, feedback và bảo hành
- Mục đích: Admin Chat hiển thị danh sách hội thoại và nội dung trao đổi theo thời gian thực.
- Điểm nhấn: Admin chat realtime; Ba kênh hỗ trợ bổ sung nhau

## Slide 28 — Kết quả đạt được & chất lượng triển khai
- Mục đích: Kết quả được đo từ chính cấu trúc source hiện tại.
- Điểm nhấn: Kết quả định lượng từ source; Nêu rõ giới hạn hiện tại

## Slide 29 — Hướng phát triển — từ đồ án đến sản phẩm vận hành
- Mục đích: Lộ trình phát triển bắt đầu từ điểm ảnh hưởng trực tiếp đến vận hành.
- Điểm nhấn: Ưu tiên tự động hóa giao dịch; Nâng tư vấn và đo lường

## Slide 30 — Xin chân thành cảm ơn
- Mục đích: Phần trình bày của em xin kết thúc tại đây.
- Điểm nhấn: Cảm ơn hội đồng; Sẵn sàng demo và trao đổi
