# Ghi chú thuyết trình — DATN PC Store

> Nội dung được đối chiếu với source code ngày 10/06/2026. Mỗi slide có lời thuyết trình ngắn và ghi chú ảnh thực tế cần bổ sung.

## Slide 01 — DATN PC STORE

1. Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.
2. Đề tài xây dựng website bán linh kiện máy tính trên nền ASP.NET Core MVC.
3. Hệ thống bao quát quy trình từ tra cứu sản phẩm đến đặt hàng và quản trị.
4. Phần trình bày tập trung vào vấn đề, giải pháp, triển khai và kết quả đạt được.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 02 — Đặt vấn đề

1. Thị trường linh kiện máy tính có nhiều chủng loại và thông số kỹ thuật.
2. Người mua cần một kênh giúp tìm, lọc và đối chiếu sản phẩm nhanh hơn.
3. Quy trình mua hàng cũng cần liên kết giỏ hàng, thanh toán và theo dõi đơn.
4. Về phía cửa hàng, dữ liệu sản phẩm và đơn hàng cần được quản lý tập trung.
5. Đó là bài toán thực tế mà đề tài hướng đến.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 03 — Lý do chọn đề tài

1. Slide này trình bày lý do chọn đề tài theo các nhóm nội dung chính.
2. Các nhóm được rút gọn để tránh danh sách bullet dài.
3. Mỗi nội dung đều đã được đối chiếu với controller, model, service hoặc view tương ứng.
4. Cách trình bày dạng thẻ giúp phân biệt rõ vai trò của từng nhóm.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 04 — Mục tiêu đề tài

1. Mục tiêu trung tâm là xây dựng website thương mại điện tử cho linh kiện PC.
2. Khách hàng có thể tìm kiếm, lọc và so sánh sản phẩm.
3. Module Build PC hỗ trợ chọn linh kiện, tính tổng và đưa cảnh báo tương thích.
4. Quy trình mua hàng bao gồm giỏ hàng, vận chuyển, thanh toán và theo dõi đơn.
5. Admin có các màn hình quản lý dữ liệu vận hành chính.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 05 — Đối tượng sử dụng

1. Slide này trình bày đối tượng sử dụng theo các nhóm nội dung chính.
2. Các nhóm được rút gọn để tránh danh sách bullet dài.
3. Mỗi nội dung đều đã được đối chiếu với controller, model, service hoặc view tương ứng.
4. Cách trình bày dạng thẻ giúp phân biệt rõ vai trò của từng nhóm.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 06 — Phạm vi hệ thống

1. Phạm vi đề tài là website thương mại điện tử cho PC và linh kiện.
2. Nội dung bao gồm luồng khách hàng, quản trị và hỗ trợ sau bán hàng.
3. Các tích hợp được trình bày chỉ khi có đăng ký service hoặc luồng xử lý trong source.
4. Đề tài không tuyên bố các chức năng chưa được triển khai.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 07 — Yêu cầu chức năng

1. Nội dung yêu cầu chức năng được chia thành hai nhóm để dễ đối chiếu.
2. Cột bên trái tập trung vào khách hàng.
3. Cột bên phải tập trung vào quản trị viên.
4. Các nhận định được giữ ở mức thực tế và phù hợp với phạm vi đồ án.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 08 — Sơ đồ use case tổng quan

1. Sơ đồ tổng quan có hai tác nhân chính là khách hàng và quản trị viên.
2. Khách hàng sử dụng các chức năng tra cứu, so sánh, Build PC và mua hàng.
3. Khách đã đăng nhập có thể theo dõi đơn và gửi yêu cầu bảo hành.
4. Chat hỗ trợ kết nối khách hàng với phía quản trị qua SignalR.
5. Admin tập trung vào quản lý sản phẩm, đơn hàng và dữ liệu vận hành.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 09 — Kiến trúc hệ thống

1. Hệ thống sử dụng kiến trúc MVC truyền thống của ASP.NET Core.
2. Request từ trình duyệt đi qua Controller rồi đến các service nghiệp vụ.
3. Entity Framework Core đảm nhiệm truy cập cơ sở dữ liệu SQL Server.
4. SignalR phục vụ chat hỗ trợ theo thời gian thực.
5. Các nhánh tích hợp thực tế gồm GHN, dịch vụ tuyến đường, SMTP và QR chuyển khoản.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 10 — Công nghệ sử dụng

1. Slide này trình bày công nghệ sử dụng theo các nhóm nội dung chính.
2. Các nhóm được rút gọn để tránh danh sách bullet dài.
3. Mỗi nội dung đều đã được đối chiếu với controller, model, service hoặc view tương ứng.
4. Cách trình bày dạng thẻ giúp phân biệt rõ vai trò của từng nhóm.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 11 — Cơ sở dữ liệu

1. ApplicationDbContext khai báo 22 DbSet tương ứng 22 bảng dữ liệu chính.
2. Nhóm sản phẩm quản lý danh mục, sản phẩm, ảnh và banner.
3. Nhóm mua hàng gồm giỏ hàng, đơn hàng và chi tiết đơn.
4. Build PC, bảo hành, vận chuyển và cấu hình website được lưu thành các thực thể riêng.
5. Chat hỗ trợ sử dụng hai bảng Conversation và Message.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 12 — Quan hệ dữ liệu chính

1. Quan hệ cốt lõi bắt đầu từ User tạo Order và Order chứa nhiều OrderDetail.
2. Mỗi chi tiết đơn liên kết đến một Product.
3. Product thuộc Category và có thể xuất hiện trong CartItem, Warranty hoặc BuildPcItem.
4. Giỏ hàng được lưu cho người dùng đăng nhập; khách vãng lai dùng session.
5. ChatConversation liên kết nhiều ChatMessage để giữ lịch sử hỗ trợ.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 13 — Các module chính

1. Slide này trình bày các module chính theo các nhóm nội dung chính.
2. Các nhóm được rút gọn để tránh danh sách bullet dài.
3. Mỗi nội dung đều đã được đối chiếu với controller, model, service hoặc view tương ứng.
4. Cách trình bày dạng thẻ giúp phân biệt rõ vai trò của từng nhóm.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 14 — Trang chủ

1. Slide này minh họa màn hình trang chủ của hệ thống.
2. Source code xác nhận các nội dung chính gồm: banner và nhóm sản phẩm, điều hướng danh mục nhanh, tìm kiếm toàn site.
3. Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.
4. Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ.

**Ghi chú ảnh:** TRANG CHỦ — chụp tại /Home/Index

## Slide 15 — Danh sách sản phẩm

1. Slide này minh họa màn hình danh sách sản phẩm của hệ thống.
2. Source code xác nhận các nội dung chính gồm: tìm theo từ khóa, lọc danh mục và khoảng giá, sắp xếp kết quả.
3. Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.
4. Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ.

**Ghi chú ảnh:** DANH SÁCH SẢN PHẨM — chụp tại /Products

## Slide 16 — Chi tiết sản phẩm

1. Slide này minh họa màn hình chi tiết sản phẩm của hệ thống.
2. Source code xác nhận các nội dung chính gồm: giá và khuyến mãi, ảnh, mô tả, thông số, thêm giỏ và so sánh.
3. Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.
4. Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ.

**Ghi chú ảnh:** CHI TIẾT SẢN PHẨM — chụp tại /Products/Detail/{id}

## Slide 17 — Giỏ hàng

1. Quy trình giỏ hàng được mô tả theo thứ tự từ trái sang phải.
2. Mỗi bước tương ứng với một trạng thái hoặc thao tác có trong luồng xử lý thực tế.
3. Giỏ session cho khách; giỏ database cho tài khoản.
4. Timeline giúp hội đồng theo dõi luồng nghiệp vụ mà không cần đọc nhiều bullet.

**Ghi chú ảnh:** ẢNH GIỎ HÀNG — chụp tại /Cart

## Slide 18 — Quy trình đặt hàng

1. Quy trình quy trình đặt hàng được mô tả theo thứ tự từ trái sang phải.
2. Mỗi bước tương ứng với một trạng thái hoặc thao tác có trong luồng xử lý thực tế.
3. GHN cung cấp địa chỉ và phí giao hàng khi cấu hình hợp lệ.
4. Timeline giúp hội đồng theo dõi luồng nghiệp vụ mà không cần đọc nhiều bullet.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 19 — Thanh toán QR / chuyển khoản

1. Nội dung thanh toán qr / chuyển khoản được chia thành hai nhóm để dễ đối chiếu.
2. Cột bên trái tập trung vào thông tin đơn hàng.
3. Cột bên phải tập trung vào qr & trạng thái.
4. Các nhận định được giữ ở mức thực tế và phù hợp với phạm vi đồ án.

**Ghi chú ảnh:** Thanh toán chuyển khoản — /Orders/BankTransfer/{id}

## Slide 20 — Theo dõi đơn hàng

1. Quy trình theo dõi đơn hàng được mô tả theo thứ tự từ trái sang phải.
2. Mỗi bước tương ứng với một trạng thái hoặc thao tác có trong luồng xử lý thực tế.
3. Trạng thái thật: PendingPayment → PendingConfirmation → Processing → Delivering → Completed.
4. Timeline giúp hội đồng theo dõi luồng nghiệp vụ mà không cần đọc nhiều bullet.

**Ghi chú ảnh:** ẢNH THEO DÕI ĐƠN — chụp tại /Orders/Tracking/{id}

## Slide 21 — Build PC

1. Slide này minh họa màn hình build pc của hệ thống.
2. Source code xác nhận các nội dung chính gồm: chọn linh kiện theo nhóm, cảnh báo socket, ddr, psu, tính tổng và thêm cả bộ vào giỏ.
3. Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.
4. Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ.

**Ghi chú ảnh:** BUILD PC — chụp tại /BuildPc

## Slide 22 — So sánh sản phẩm

1. Hệ thống cho phép lưu tối đa hai sản phẩm trong session để so sánh.
2. Màn hình so sánh tổng hợp giá và các hàng thông số kỹ thuật.
3. ViewModel hỗ trợ các dòng CPU, RAM, GPU, SSD, mainboard, nguồn, case và tản nhiệt.
4. Bảng trên minh họa cách đối chiếu hai sản phẩm theo cùng tiêu chí.

**Ghi chú ảnh:** Màn hình so sánh — chụp tại /Compare

## Slide 23 — Dịch vụ sau bán hàng

1. Slide này minh họa màn hình dịch vụ sau bán hàng của hệ thống.
2. Source code xác nhận các nội dung chính gồm: gửi yêu cầu bảo hành, theo dõi trạng thái xử lý, xuất báo giá đơn hàng.
3. Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.
4. Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ.

**Ghi chú ảnh:** BẢO HÀNH / BÁO GIÁ — chụp tại /Warranty • /Orders/Quotation

## Slide 24 — Chat hỗ trợ SignalR

1. Chat hỗ trợ được triển khai bằng SignalR và ChatHub.
2. Khách vãng lai hoặc người dùng đăng nhập đều có thể mở hội thoại.
3. Tin nhắn được lưu vào ChatConversation và ChatMessage.
4. Admin xem danh sách hội thoại, phản hồi, đánh dấu đã đọc và đóng phiên chat.

**Ghi chú ảnh:** Chat widget và Admin Chat — widget toàn site, /AdminChat

## Slide 25 — Trang quản trị

1. Khu vực quản trị được bảo vệ bằng quyền Admin.
2. Dashboard hiển thị số sản phẩm, đơn hàng, người dùng và yêu cầu bảo hành.
3. Menu quản trị còn có danh mục, banner, cài đặt website và chat hỗ trợ.
4. Các module phản ánh trực tiếp các controller và Razor View trong source.

**Ghi chú ảnh:** Dashboard quản trị — chụp tại /AdminDashboard

## Slide 26 — Quản lý sản phẩm và đơn hàng

1. Nội dung quản lý sản phẩm và đơn hàng được chia thành hai nhóm để dễ đối chiếu.
2. Cột bên trái tập trung vào quản lý sản phẩm.
3. Cột bên phải tập trung vào quản lý đơn hàng.
4. Các nhận định được giữ ở mức thực tế và phù hợp với phạm vi đồ án.

**Ghi chú ảnh:** Ảnh /AdminProducts và /AdminOrders

## Slide 27 — Kết quả đạt được

1. Kết quả được lượng hóa trực tiếp từ cấu trúc source code.
2. ApplicationDbContext hiện khai báo 22 DbSet và thư mục Controllers có 20 controller.
3. Hệ thống phục vụ khách vãng lai cùng ba vai trò được seed: Customer, Staff và Admin.
4. Tám nhóm công nghệ chính đã được áp dụng xuyên suốt ứng dụng.
5. Các con số này không dựa trên dữ liệu giả lập hay ước lượng ngoài source.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 28 — Hạn chế

1. Nội dung hạn chế được chia thành hai nhóm để dễ đối chiếu.
2. Cột bên trái tập trung vào hiện tại.
3. Cột bên phải tập trung vào cần cải thiện.
4. Các nhận định được giữ ở mức thực tế và phù hợp với phạm vi đồ án.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 29 — Hướng phát triển

1. Quy trình hướng phát triển được mô tả theo thứ tự từ trái sang phải.
2. Mỗi bước tương ứng với một trạng thái hoặc thao tác có trong luồng xử lý thực tế.
3. Roadmap ưu tiên tự động hóa, thông minh hóa và mở rộng vận hành.
4. Timeline giúp hội đồng theo dõi luồng nghiệp vụ mà không cần đọc nhiều bullet.

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 30 — Xin chân thành cảm ơn

1. Phần trình bày của em xin được kết thúc tại đây.
2. Em xin chân thành cảm ơn giảng viên hướng dẫn và hội đồng đã lắng nghe.
3. Em mong nhận được các góp ý để tiếp tục hoàn thiện hệ thống.
4. Em xin phép được trả lời các câu hỏi của thầy cô.

**Ghi chú ảnh:** Không cần chèn ảnh.
