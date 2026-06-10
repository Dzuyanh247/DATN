# Lời thuyết trình — DATN PC Store

> Nội dung khớp với 11 slide bảo vệ đồ án được sinh từ source code hiện tại.

## Slide 01 — Website bán máy tính & linh kiện

### Lời thuyết trình

Em xin kính chào hội đồng và thầy cô tham dự buổi bảo vệ đồ án tốt nghiệp.
Đề tài của em là xây dựng website bán máy tính và linh kiện PC theo mô hình thương mại điện tử.
Sản phẩm tập trung vào trải nghiệm mua sắm của khách hàng và khả năng vận hành tập trung của quản trị viên.
Trong phần trình bày, em sẽ giới thiệu mục tiêu, công nghệ, chức năng chính, giao diện và định hướng phát triển.

### Ý cần nhấn mạnh
- Bài toán thương mại điện tử thực tế
- Hai nhóm người dùng: khách hàng và quản trị

### Ảnh cần chèn
- Không.

## Slide 02 — Giới thiệu đề tài

### Lời thuyết trình

Đề tài xuất phát từ thực tế sản phẩm công nghệ có nhiều thông số và khách hàng thường mất thời gian đối chiếu.
Nhu cầu không chỉ dừng ở xem giá mà còn gồm tìm kiếm, lọc, đặt hàng và theo dõi sau mua.
Vì vậy hệ thống được định hướng như một quy trình mua sắm hoàn chỉnh thay vì chỉ là trang trưng bày sản phẩm.
Mục tiêu cuối cùng là cân bằng giữa trải nghiệm khách hàng và hiệu quả quản trị của cửa hàng.

### Ý cần nhấn mạnh
- Bài toán có nhu cầu thực tế
- Mục tiêu là một quy trình mua sắm hoàn chỉnh

### Ảnh cần chèn
- Không.

## Slide 03 — Tổng quan hệ thống

### Lời thuyết trình

Hệ thống phục vụ ba nhóm chính gồm khách vãng lai, khách có tài khoản và quản trị viên.
Phần khách hàng bao phủ hành trình từ khám phá sản phẩm, thêm giỏ, đặt hàng đến theo dõi trạng thái.
Phần quản trị tập trung dữ liệu sản phẩm, đơn hàng, người dùng và báo cáo để hỗ trợ vận hành.
Kiến trúc MVC và lớp dịch vụ giúp mã nguồn có tổ chức, dễ bảo trì và mở rộng thêm tích hợp trong tương lai.

### Ý cần nhấn mạnh
- Ba nhóm người dùng
- Một hành trình xuyên suốt từ khám phá đến theo dõi

### Ảnh cần chèn
- Không.

## Slide 04 — Công nghệ sử dụng

### Lời thuyết trình

ASP.NET Core MVC là nền tảng chính, giúp phân tách trách nhiệm giữa dữ liệu, xử lý và giao diện.
Entity Framework Core kết nối ứng dụng với SQL Server, giảm mã truy vấn lặp lại và hỗ trợ migration.
SQL Server lưu trữ dữ liệu nghiệp vụ có quan hệ như khách hàng, sản phẩm, giỏ hàng và đơn hàng.
Ở phía giao diện, Bootstrap, CSS và JavaScript được kết hợp để bảo đảm khả năng hiển thị responsive và tương tác thuận tiện.

### Ý cần nhấn mạnh
- Mỗi công nghệ có một vai trò rõ ràng
- Stack phù hợp ứng dụng thương mại điện tử MVC

### Ảnh cần chèn
- Không.

## Slide 05 — Chức năng dành cho khách hàng

### Lời thuyết trình

Các chức năng khách hàng được thiết kế theo đúng thứ tự của một hành trình mua sắm trực tuyến.
Người dùng có thể tìm và lọc sản phẩm trước khi đăng nhập, sau đó quản lý lựa chọn trong giỏ hàng.
Tại checkout, hệ thống thu thập thông tin giao nhận, kiểm tra dữ liệu và tạo đơn với mã theo dõi.
Sau khi đặt hàng, khách có thể kiểm tra trạng thái và lựa chọn COD hoặc chuyển khoản tùy nhu cầu.

### Ý cần nhấn mạnh
- Sáu chức năng theo một hành trình
- Giảm số bước và giữ thông tin minh bạch

### Ảnh cần chèn
- Không.

## Slide 06 — Chức năng quản trị

### Lời thuyết trình

Khu vực quản trị được tổ chức theo các nhóm nghiệp vụ mà cửa hàng phải thực hiện hằng ngày.
Quản trị viên có thể cập nhật sản phẩm, tồn kho và hình ảnh mà không cần sửa trực tiếp cơ sở dữ liệu.
Đơn hàng được theo dõi theo trạng thái, kết hợp thông tin giao nhận và phương thức thanh toán.
Các nhóm người dùng, khuyến mãi và thống kê giúp hệ thống không chỉ bán hàng mà còn hỗ trợ vận hành và đánh giá.

### Ý cần nhấn mạnh
- Quản trị theo nghiệp vụ thực tế
- Dữ liệu tập trung, dễ theo dõi và cập nhật

### Ảnh cần chèn
- Không.

## Slide 07 — Giao diện trang chủ

### Lời thuyết trình

Trang chủ là điểm bắt đầu của hành trình nên phần ảnh chụp được đặt lớn để hội đồng quan sát tổng thể.
Header tập trung tìm kiếm, danh mục, tài khoản và giỏ hàng để giảm thời gian điều hướng.
Banner và các khu vực khuyến mãi tạo điểm nhấn nhưng vẫn giữ cấu trúc rõ ràng, không che nội dung sản phẩm.
Các nhóm sản phẩm được trình bày theo card đồng nhất, giúp người dùng quét nhanh tên, giá và ưu đãi.

### Ý cần nhấn mạnh
- Ảnh demo chiếm khoảng 60%
- Bốn khu vực chính có vai trò khác nhau

### Ảnh cần chèn
- Có.
- URL: /

## Slide 08 — Giao diện chi tiết sản phẩm

### Lời thuyết trình

Trang chi tiết sản phẩm cần trả lời ba câu hỏi: đây là sản phẩm gì, có phù hợp không và mua bằng cách nào.
Hình ảnh, giá, tồn kho và khuyến mãi được ưu tiên ở vùng nhìn đầu tiên để hỗ trợ quyết định nhanh.
Thông số được nhóm theo cấu trúc dễ đọc thay vì đưa thành một đoạn văn dài.
Các thao tác thêm giỏ, mua ngay hoặc so sánh được đặt gần thông tin chính để giảm số lần chuyển trang.

### Ý cần nhấn mạnh
- Một màn hình hỗ trợ quyết định mua
- Thông tin và hành động được đặt gần nhau

### Ảnh cần chèn
- Có.
- URL: /Products/Details/{id}

## Slide 09 — Giỏ hàng & thanh toán

### Lời thuyết trình

Quy trình checkout được chia thành bốn bước ngắn để người dùng luôn biết mình đang ở giai đoạn nào.
Trước khi tạo đơn, khách kiểm tra lại sản phẩm, số lượng, giá và nhập thông tin giao nhận.
Với chuyển khoản, hệ thống hiển thị số tiền, nội dung và mã QR để hạn chế sai sót khi thanh toán.
Sau khi hoàn tất, mã đơn và trạng thái giúp khách chủ động theo dõi thay vì phải liên hệ cửa hàng nhiều lần.

### Ý cần nhấn mạnh
- Quy trình bốn bước
- Thông tin chuyển khoản rõ ràng và có thể đối chiếu

### Ảnh cần chèn
- Có.
- URL: /Cart/Checkout

## Slide 10 — Ưu điểm của hệ thống

### Lời thuyết trình

Ưu điểm đầu tiên là giao diện đồng bộ với website, dễ đọc và làm nổi bật thông tin mua hàng quan trọng.
Luồng sử dụng quen thuộc giúp khách mới có thể tìm sản phẩm và đặt hàng mà không cần hướng dẫn dài.
Đối với cửa hàng, dữ liệu quản trị được tập trung nên việc cập nhật và theo dõi thuận tiện hơn.
Kiến trúc hiện tại cũng tạo nền tảng để tối ưu hiệu năng và tích hợp thêm dịch vụ khi quy mô tăng.

### Ý cần nhấn mạnh
- Ưu điểm trải đều ở UX, quản trị và kỹ thuật
- Kiến trúc tạo nền tảng mở rộng

### Ảnh cần chèn
- Không.

## Slide 11 — Kết luận & hướng phát triển

### Lời thuyết trình

Đồ án đã hoàn thiện các chức năng cốt lõi của một website bán máy tính, từ giao diện khách hàng đến quản trị.
Quá trình thực hiện giúp em vận dụng kiến thức MVC, cơ sở dữ liệu, thiết kế giao diện và phân tích nghiệp vụ.
Tuy nhiên hệ thống vẫn cần tiếp tục nâng cấp bảo mật, kiểm thử và tự động hóa thanh toán để phù hợp vận hành thực tế.
Trong tương lai, em định hướng bổ sung gợi ý sản phẩm, báo cáo chuyên sâu, tối ưu mobile và triển khai trên hạ tầng cloud.
Em xin chân thành cảm ơn hội đồng và sẵn sàng tiếp nhận câu hỏi, góp ý.

### Ý cần nhấn mạnh
- Đã đạt mục tiêu cốt lõi
- Hướng phát triển ưu tiên tính thực tế và khả năng vận hành

### Ảnh cần chèn
- Không.
