# Lời thuyết trình DATN PC Store

## Slide 01 | DATN PC Store
Kính chào thầy cô và các bạn. Sau đây em xin trình bày đồ án DATN PC Store, một website thương mại điện tử bán PC, laptop và linh kiện được xây dựng trên ASP.NET Core MVC.

## Slide 02 | Bài toán và mục tiêu
Đề tài giải quyết nhu cầu đưa hoạt động bán thiết bị máy tính lên môi trường trực tuyến. Hệ thống không chỉ hỗ trợ quy trình mua hàng cơ bản mà còn triển khai các nghiệp vụ phù hợp cửa hàng PC như build cấu hình, so sánh linh kiện, bảo hành và hỗ trợ khách hàng.

## Slide 03 | Phạm vi và vai trò sử dụng
Hệ thống có ba nhóm sử dụng. Khách vãng lai vẫn có thể duyệt hàng, dùng giỏ hàng phiên, tra cứu đơn và chat. Khách hàng có thêm hồ sơ, lịch sử mua hàng và bảo hành. Toàn bộ màn hình quản trị được bảo vệ bằng role Admin.

## Slide 04 | Kiến trúc tổng thể
Dự án tổ chức theo mô hình MVC monolith. Controller nhận request và điều phối, Razor View render HTML, service xử lý nghiệp vụ, còn Entity Framework Core làm việc với SQL Server. Các tích hợp bên ngoài được đặt sau interface hoặc service để giảm phụ thuộc trực tiếp từ controller.

## Slide 05 | Công nghệ sử dụng
Nền tảng chính là .NET 8. Giao diện sử dụng Razor, CSS và JavaScript. Dữ liệu được truy cập qua EF Core 8 với SQL Server. Cookie dùng cho đăng nhập, Session dùng cho trạng thái khách, SignalR phục vụ chat. Chức năng xuất đơn hàng tạo bảng HTML và trả về tệp `.xls` tương thích Excel.

## Slide 06 | Mô hình dữ liệu chính
Cơ sở dữ liệu được chia theo các nhóm nghiệp vụ rõ ràng: tài khoản, catalog, bán hàng và hậu mãi. Ngoài các bảng thương mại điện tử quen thuộc, source còn có cấu hình build PC, vận chuyển, OTP đặt lại mật khẩu và các bảng hội thoại hỗ trợ.

## Slide 07 | Trang chủ và điều hướng mua sắm
Trang chủ lấy banner đang hoạt động và các nhóm sản phẩm để tạo điểm bắt đầu mua sắm. Thanh điều hướng kết nối trực tiếp đến tìm kiếm, trang tin, build PC, tài khoản và giỏ hàng. Khung bên phải là vị trí để bổ sung ảnh chụp giao diện thật.

## Slide 08 | Danh sách và chi tiết sản phẩm
Trang danh sách hỗ trợ nhiều tiêu chí lọc đã được triển khai trong controller, gồm danh mục, hãng, giá, CPU, RAM và GPU. Trang chi tiết trình bày thông tin sản phẩm, ảnh, tồn kho, thông số, sản phẩm mua kèm và các hành động mua hàng hoặc so sánh.

## Slide 09 | Tài khoản và phân quyền
Luồng tài khoản gồm đăng ký, đăng nhập, cập nhật hồ sơ, đổi mật khẩu và quên mật khẩu. Mã OTP đặt lại mật khẩu được gửi qua email, có thời hạn và số lần thử. Khi đăng nhập thành công, giỏ hàng của khách được hợp nhất vào giỏ của tài khoản.

## Slide 10 | Giỏ hàng đa trạng thái
CartService xử lý hai hình thức lưu trữ. Khách chưa đăng nhập dùng Session, còn người dùng đăng nhập dùng cơ sở dữ liệu. Các thao tác thay đổi số lượng đều kiểm tra tính hợp lệ và tồn kho trước khi cập nhật.

## Slide 11 | Checkout và tính phí vận chuyển
Checkout thu thập địa chỉ và gọi các service vận chuyển. Source có tích hợp danh mục địa chỉ và tính phí GHN, đồng thời có luồng bản đồ, định tuyến và bộ tính phí theo chính sách. Trước khi đặt hàng, hệ thống kiểm tra lại dữ liệu giỏ và tồn kho.

## Slide 12 | Quy trình tạo đơn an toàn
Điểm quan trọng của quy trình đặt hàng là transaction. Đơn hàng, chi tiết đơn, cập nhật tồn kho và xóa giỏ được xử lý như một đơn vị công việc. Nếu có lỗi, transaction rollback để tránh trạng thái lưu một phần.

## Slide 13 | Thanh toán và chứng từ đơn hàng
Source triển khai COD và chuyển khoản ngân hàng. Với chuyển khoản, người mua xem thông tin thanh toán, xác nhận đã chuyển; Admin có thao tác xác nhận riêng. Đơn chờ thanh toán có thời hạn. Hệ thống cũng tạo trang báo giá để in và file Excel cho đơn hàng.

## Slide 14 | Build PC theo linh kiện
Màn hình Build PC cho phép chọn từng nhóm linh kiện, tìm và sắp xếp sản phẩm. BuildCompatibilityService đưa ra các cảnh báo cần kiểm tra về socket CPU/Mainboard, chuẩn DDR và công suất nguồn; service hiện vẫn cho phép tiếp tục chọn linh kiện. Cấu hình có thể thêm vào giỏ, in, tải ảnh từ giao diện phía trình duyệt hoặc xuất CSV.

## Slide 15 | So sánh sản phẩm
Danh sách so sánh được lưu trong Session và giới hạn tối đa hai sản phẩm. Người dùng có thể thêm từ thẻ hoặc trang chi tiết, sau đó xem bảng thuộc tính, xóa từng sản phẩm hoặc xóa toàn bộ.

## Slide 16 | Theo dõi đơn và bảo hành
Hệ thống hỗ trợ cả khách và người dùng đăng nhập theo dõi đơn. Khách cần mã đơn và số điện thoại, còn tài khoản xem các đơn thuộc chính mình. Sau mua hàng, người dùng đăng nhập có thể gửi yêu cầu bảo hành và Admin cập nhật trạng thái xử lý.

## Slide 17 | Nội dung và phản hồi khách hàng
Ngoài bán hàng, website có module bài viết và liên hệ. Bài viết có danh sách, chi tiết theo slug và các thao tác quản trị. Feedback được lưu lại và có màn hình quản lý. Banner cùng một số thiết lập giao diện cũng được cấu hình trong Admin.

## Slide 18 | Hỗ trợ trực tuyến với SignalR
Module chat sử dụng API controller kết hợp SignalR. Khách giữ access token để truy cập đúng hội thoại. Admin có màn hình xem hội thoại, đọc và gửi tin nhắn, sau đó đóng cuộc trò chuyện khi hoàn tất.

## Slide 19 | Hệ thống quản trị
Khu vực Admin tập trung các nghiệp vụ vận hành: dashboard, sản phẩm, danh mục, đơn hàng, người dùng, banner, cài đặt, chat và bảo hành. Các controller quản trị đều được gắn Authorize với role Admin.

## Slide 20 | Đánh giá và kết luận
Tóm lại, source hiện tại đã bao phủ một chu trình thương mại điện tử tương đối đầy đủ và có các điểm nhấn phù hợp đề tài cửa hàng PC. Những giới hạn được nêu chỉ dựa trên repository: chưa thấy test tự động và CI/CD. Hướng phát triển là nâng chuẩn xác thực, kiểm thử và vận hành production.
