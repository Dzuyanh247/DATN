# Lời thuyết trình — DATN PC Store

> Nội dung khớp với 30 slide được sinh từ source code hiện tại.

## Slide 01 — DATN PC Store

### Lời thuyết trình

Em xin kính chào hội đồng và thầy cô tham dự buổi bảo vệ.
Đề tài của em là xây dựng website PC Store phục vụ bán linh kiện máy tính.
Hệ thống tập trung vào hành trình từ tìm sản phẩm đến đặt hàng và hậu mãi.
Sau đây em xin trình bày ngắn gọn bài toán, giải pháp và phần demo chính.

### Ý cần nhấn mạnh
- Website thương mại điện tử cho linh kiện PC
- Trình bày theo luồng nghiệp vụ thực tế

### Ảnh cần chèn
- Không.

## Slide 02 — Đặt vấn đề

### Lời thuyết trình

Ở phần đặt vấn đề, em tập trung vào vấn đề trực tiếp của người dùng.
Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.
Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.
Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo.

### Ý cần nhấn mạnh
- Nhu cầu mua linh kiện ngày càng tăng
- Khách cần quy trình mua hàng liền mạch

### Ảnh cần chèn
- Không.

## Slide 03 — Khó khăn thực tế

### Lời thuyết trình

Ở phần khó khăn thực tế, em tập trung vào vấn đề trực tiếp của người dùng.
Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.
Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.
Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo.

### Ý cần nhấn mạnh
- Thông số kỹ thuật khó đọc
- Theo dõi đơn và hậu mãi chưa thuận tiện

### Ảnh cần chèn
- Không.

## Slide 04 — Lý do chọn đề tài

### Lời thuyết trình

Ở phần lý do chọn đề tài, em tập trung vào vấn đề trực tiếp của người dùng.
Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.
Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.
Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo.

### Ý cần nhấn mạnh
- Bài toán gần với thực tế
- Có nhiều nghiệp vụ để kiểm chứng

### Ảnh cần chèn
- Không.

## Slide 05 — Mục tiêu đề tài

### Lời thuyết trình

Ở phần mục tiêu đề tài, em tập trung vào vấn đề trực tiếp của người dùng.
Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.
Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.
Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo.

### Ý cần nhấn mạnh
- Xây dựng website bán linh kiện
- Theo dõi đơn và hỗ trợ sau bán

### Ảnh cần chèn
- Không.

## Slide 06 — Đối tượng và phạm vi

### Lời thuyết trình

Ở phần đối tượng và phạm vi, em tập trung vào vấn đề trực tiếp của người dùng.
Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.
Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.
Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo.

### Ý cần nhấn mạnh
- Khách vãng lai và khách đăng nhập
- Thanh toán COD và chuyển khoản

### Ảnh cần chèn
- Không.

## Slide 07 — Phân tích & thiết kế

### Lời thuyết trình

Tiếp theo em xin chuyển sang phần phân tích và thiết kế.
Phần này không đi sâu vào sơ đồ phức tạp mà tập trung vào các thành phần chính.
Em sẽ lần lượt trình bày yêu cầu, kiến trúc, công nghệ và dữ liệu.
Mục tiêu là cho thấy giải pháp bám sát bài toán đã nêu.

### Ý cần nhấn mạnh
- Thiết kế đơn giản, bám nghiệp vụ
- Tập trung vào luồng xử lý chính

### Ảnh cần chèn
- Không.

## Slide 08 — Yêu cầu chức năng

### Lời thuyết trình

Yêu cầu được chia thành hai nhóm người dùng chính.
Khách hàng thao tác từ khám phá sản phẩm đến dịch vụ sau bán.
Quản trị viên tập trung vào dữ liệu, đơn hàng và hỗ trợ khách.
Cách chia này giúp thiết kế controller và giao diện rõ trách nhiệm.

### Ý cần nhấn mạnh
- Hai nhóm chức năng rõ ràng
- Không đưa chức năng ngoài source

### Ảnh cần chèn
- Không.

## Slide 09 — Kiến trúc hệ thống

### Lời thuyết trình

Hệ thống sử dụng kiến trúc phân lớp quen thuộc của ASP.NET Core MVC.
Yêu cầu từ trình duyệt đi qua controller rồi đến lớp service.
EF Core đảm nhiệm truy cập SQL Server và ánh xạ dữ liệu.
SignalR, GHN và SMTP hoặc QR là các tích hợp hỗ trợ bên ngoài.

### Ý cần nhấn mạnh
- Năm lớp xử lý chính
- Tích hợp ngoài được tách riêng

### Ảnh cần chèn
- Không.

## Slide 10 — Công nghệ sử dụng

### Lời thuyết trình

Công nghệ chính là ASP.NET Core MVC kết hợp Entity Framework Core.
SQL Server lưu dữ liệu nghiệp vụ và migration quản lý thay đổi cấu trúc.
SignalR hỗ trợ chat thời gian thực giữa khách và quản trị.
Giao vận, email và QR được tích hợp theo từng nghiệp vụ cụ thể.

### Ý cần nhấn mạnh
- Stack đồng nhất với source
- Tích hợp phục vụ nghiệp vụ thật

### Ảnh cần chèn
- Không.

## Slide 11 — Cơ sở dữ liệu

### Lời thuyết trình

Thay vì trình bày ERD chi tiết, em nhóm dữ liệu theo năm miền nghiệp vụ.
Tài khoản và sản phẩm là dữ liệu nền của hệ thống.
Giỏ hàng, đơn hàng và bảo hành thể hiện luồng mua bán.
Nhóm chat lưu cả hội thoại và tin nhắn để hỗ trợ khách lâu dài.

### Ý cần nhấn mạnh
- 22 DbSet trong ApplicationDbContext
- Năm nhóm dữ liệu dễ theo dõi

### Ảnh cần chèn
- Không.

## Slide 12 — Các module chính

### Lời thuyết trình

Source code được tổ chức thành các module nghiệp vụ tương đối rõ.
Nhóm sản phẩm, tài khoản và giỏ hàng phục vụ đầu hành trình mua sắm.
Đơn hàng, Build PC và so sánh hỗ trợ quyết định và giao dịch.
Khối quản trị cùng hỗ trợ giúp cửa hàng vận hành sau khi khách đặt hàng.

### Ý cần nhấn mạnh
- Sáu module nghiệp vụ
- Module khách hàng và quản trị liên kết

### Ảnh cần chèn
- Không.

## Slide 13 — Demo website khách hàng

### Lời thuyết trình

Sau phần thiết kế, em xin chuyển sang demo website khách hàng.
Các màn hình được sắp theo đúng hành trình sử dụng phổ biến.
Mỗi slide dành phần lớn diện tích cho ảnh chụp giao diện thật.
Khi bảo vệ, em sẽ thao tác trực tiếp và dùng slide làm phương án dự phòng.

### Ý cần nhấn mạnh
- Demo theo hành trình người dùng
- Ưu tiên ảnh thật, ít chữ

### Ảnh cần chèn
- Không.

## Slide 14 — Trang chủ

### Lời thuyết trình

Màn hình trang chủ là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm banner và danh mục nổi bật và sản phẩm khuyến mãi.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Banner và danh mục nổi bật
- Điểm vào hành trình mua sắm

### Ảnh cần chèn
- Có.
- URL: /

## Slide 15 — Danh sách sản phẩm

### Lời thuyết trình

Màn hình danh sách sản phẩm là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm lọc theo danh mục và giá và tìm kiếm, sắp xếp.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Lọc theo danh mục và giá
- Hiển thị tồn kho và khuyến mãi

### Ảnh cần chèn
- Có.
- URL: /Products

## Slide 16 — Chi tiết sản phẩm

### Lời thuyết trình

Màn hình chi tiết sản phẩm là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm thông tin và hình ảnh sản phẩm và thông số kỹ thuật rõ ràng.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Thông tin và hình ảnh sản phẩm
- Thêm giỏ hoặc mua ngay

### Ảnh cần chèn
- Có.
- URL: /Products/Detail/{id}

## Slide 17 — Giỏ hàng

### Lời thuyết trình

Màn hình giỏ hàng là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm cập nhật số lượng và xóa hoặc làm trống giỏ.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Cập nhật số lượng
- Tính tổng trước checkout

### Ảnh cần chèn
- Có.
- URL: /Cart

## Slide 18 — Checkout & vận chuyển

### Lời thuyết trình

Màn hình checkout & vận chuyển là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm nhập thông tin nhận hàng và tính phí giao hàng.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Nhập thông tin nhận hàng
- Chọn phương thức thanh toán

### Ảnh cần chèn
- Có.
- URL: /Checkout

## Slide 19 — Thanh toán QR / chuyển khoản

### Lời thuyết trình

Màn hình thanh toán qr / chuyển khoản là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm hiển thị qr và nội dung chuyển và có thời hạn thanh toán.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Hiển thị QR và nội dung chuyển
- Khách xác nhận đã chuyển tiền

### Ảnh cần chèn
- Có.
- URL: /Orders/BankTransfer/{id}

## Slide 20 — Theo dõi đơn hàng

### Lời thuyết trình

Màn hình theo dõi đơn hàng là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm tra cứu bằng mã đơn và theo dõi trạng thái xử lý.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Tra cứu bằng mã đơn
- Xem thông tin vận chuyển

### Ảnh cần chèn
- Có.
- URL: /Order/Tracking/{id}

## Slide 21 — Build PC

### Lời thuyết trình

Màn hình build pc là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm chọn linh kiện theo nhóm và kiểm tra tương thích cơ bản.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Chọn linh kiện theo nhóm
- Thêm cấu hình vào giỏ

### Ảnh cần chèn
- Có.
- URL: /BuildPc

## Slide 22 — So sánh sản phẩm

### Lời thuyết trình

Màn hình so sánh sản phẩm là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm so sánh tối đa hai sản phẩm và đối chiếu giá và thông số.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- So sánh tối đa hai sản phẩm
- Lưu lựa chọn trong session

### Ảnh cần chèn
- Có.
- URL: /Compare

## Slide 23 — Hỗ trợ và hậu mãi

### Lời thuyết trình

Màn hình hỗ trợ và hậu mãi là một bước trong hành trình của khách hàng.
Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.
Các thao tác chính gồm chat realtime với hỗ trợ và gửi yêu cầu bảo hành.
Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan.

### Ý cần nhấn mạnh
- Chat realtime với hỗ trợ
- Xem báo giá từ đơn hàng

### Ảnh cần chèn
- Có.
- URL: /Warranty

## Slide 24 — Quản trị hệ thống

### Lời thuyết trình

Tiếp theo là phần quản trị hệ thống.
Em tập trung vào dashboard và hai nhóm nghiệp vụ vận hành quan trọng nhất.
Giao diện quản trị sử dụng dữ liệu thật từ database.
Các thao tác được giới hạn theo quyền của người quản trị.

### Ý cần nhấn mạnh
- Dashboard tổng quan
- Quản lý sản phẩm và đơn hàng

### Ảnh cần chèn
- Không.

## Slide 25 — Dashboard quản trị

### Lời thuyết trình

Dashboard cung cấp cái nhìn nhanh về tình trạng vận hành.
Bốn KPI trên slide tương ứng với dữ liệu có trong AdminDashboardVm.
Quản trị viên có thể từ đây chuyển sang các màn hình nghiệp vụ.
Khi demo, em sẽ dùng ảnh thật để tránh tạo dashboard giả trên PowerPoint.

### Ý cần nhấn mạnh
- KPI lấy từ ViewModel thật
- Ảnh thật chiếm phần lớn slide

### Ảnh cần chèn
- Có.
- URL: /AdminDashboard

## Slide 26 — Quản lý sản phẩm & đơn hàng

### Lời thuyết trình

Hai màn hình quản trị chính là sản phẩm và đơn hàng.
Quản lý sản phẩm hỗ trợ tạo, sửa, xóa, ảnh và thông tin tồn kho.
Quản lý đơn cho phép xem chi tiết, cập nhật trạng thái và xác nhận chuyển khoản.
Bố cục hai ảnh giúp so sánh nhanh mà không cần dựng dashboard phức tạp.

### Ý cần nhấn mạnh
- Hai nghiệp vụ vận hành chính
- Không mô phỏng giao diện bằng nhiều shape

### Ảnh cần chèn
- Có.
- URL: /AdminProducts và /AdminOrders

## Slide 27 — Kết quả đạt được

### Lời thuyết trình

Kết quả đạt được được tổng hợp trực tiếp từ cấu trúc source hiện tại.
Hệ thống có 21 controller và 22 DbSet trong DbContext.
Sáu nhóm module bao phủ luồng khách hàng, quản trị và hỗ trợ.
Quan trọng nhất là các chức năng có thể liên kết thành một quy trình mua hàng hoàn chỉnh.

### Ý cần nhấn mạnh
- 21 controller, 22 DbSet
- Luồng nghiệp vụ đã kết nối

### Ảnh cần chèn
- Không.

## Slide 28 — Hạn chế

### Lời thuyết trình

Bên cạnh kết quả đạt được, hệ thống vẫn còn một số giới hạn.
Thanh toán chuyển khoản hiện cần bước xác nhận của khách và quản trị.
Build PC mới dừng ở kiểm tra tương thích theo quy tắc đã cài đặt.
Đây là các điểm thực tế để tiếp tục cải thiện sau đồ án.

### Ý cần nhấn mạnh
- Nhìn nhận đúng giới hạn hiện tại
- Hạn chế gắn với hướng phát triển

### Ảnh cần chèn
- Không.

## Slide 29 — Hướng phát triển

### Lời thuyết trình

Từ các hạn chế vừa nêu, em đề xuất bốn hướng phát triển.
Ưu tiên đầu tiên là tích hợp cổng thanh toán để tự động đối soát.
Sau đó có thể nâng Build PC bằng dữ liệu và mô hình gợi ý phù hợp hơn.
Báo cáo doanh thu, mobile và SEO sẽ giúp hệ thống sẵn sàng vận hành thực tế.

### Ý cần nhấn mạnh
- Bốn bước rõ ràng
- Ưu tiên thanh toán và trải nghiệm

### Ảnh cần chèn
- Không.

## Slide 30 — Xin chân thành cảm ơn

### Lời thuyết trình

Phần trình bày của em xin được kết thúc tại đây.
Em xin cảm ơn thầy cô và hội đồng đã lắng nghe.
Em rất mong nhận được nhận xét để tiếp tục hoàn thiện sản phẩm.
Em xin sẵn sàng trả lời các câu hỏi của hội đồng.

### Ý cần nhấn mạnh
- Cảm ơn hội đồng
- Sẵn sàng trao đổi

### Ảnh cần chèn
- Không.
