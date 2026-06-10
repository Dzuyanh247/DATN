# Ghi chú thuyết trình — DATN PC Store

> Nội dung được đối chiếu trực tiếp với source code. Các gợi ý animation dùng hiệu ứng PowerPoint an toàn, ngắn và tiết chế.

## Slide 01 — DATN PC STORE

**Lời thuyết trình:**
1. Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.
2. Đề tài xây dựng website bán PC và linh kiện máy tính trên ASP.NET Core MVC.
3. Hệ thống kết nối trải nghiệm mua hàng, vận chuyển, hỗ trợ và quản trị.
4. Bài trình bày đi từ vấn đề thực tế đến thiết kế, triển khai và đánh giá.

**Animation suggestion:**
- Tiêu đề Fade trước trong 0,6 giây.
- Các chip công nghệ xuất hiện lần lượt sau tiêu đề.
- Khối quy trình bên phải Wipe từ trên xuống trong 0,8 giây.

**Thời lượng gợi ý:** 35–45 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 02 — Đặt vấn đề

**Lời thuyết trình:**
1. Thị trường linh kiện có nhiều lựa chọn và thông số kỹ thuật phức tạp.
2. Người mua cần tìm nhanh nhưng vẫn phải chọn đúng thành phần phù hợp nhu cầu.
3. Khả năng so sánh và theo dõi đơn giúp giảm sự không chắc chắn trong quyết định mua.
4. Đây là vấn đề trung tâm mà hệ thống PC Store hướng tới giải quyết.

**Animation suggestion:**
- Biểu tượng cảnh báo Zoom nhẹ trong 0,3 giây.
- Câu statement Fade trong 0,6 giây.
- Ba từ khóa xuất hiện nối tiếp bằng Wipe, mỗi mục 0,25 giây.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 03 — Những khó khăn thực tế

**Lời thuyết trình:**
1. Khó khăn đầu tiên là tìm đúng sản phẩm trong một danh mục linh kiện lớn.
2. Khó khăn thứ hai là so sánh các thông số được trình bày theo nhiều cách khác nhau.
3. Khó khăn thứ ba là theo dõi xuyên suốt thanh toán, xử lý và giao hàng.
4. Ba điểm đau này định hướng trực tiếp cho các module cốt lõi của đề tài.

**Animation suggestion:**
- Card 01 Float In từ trái trong 0,4 giây.
- Card 02 Fade sau 0,2 giây.
- Card 03 Float In từ phải trong 0,4 giây.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 04 — Lý do chọn đề tài

**Lời thuyết trình:**
1. Về thực tế, đề tài giải quyết một hành trình mua linh kiện có nhiều điểm ra quyết định.
2. Về kỹ thuật, bài toán đủ rộng để áp dụng MVC, dữ liệu quan hệ và tích hợp dịch vụ.
3. Source code hiện có cả luồng khách hàng, quản trị và hỗ trợ thời gian thực.
4. Vì vậy đề tài vừa phù hợp nhu cầu thực tiễn vừa thể hiện năng lực phát triển web.

**Animation suggestion:**
- Cột thực tế Wipe từ trái trong 0,5 giây.
- Cột kỹ thuật Wipe từ phải trong 0,5 giây.
- Badge ASP.NET Core MVC Pulse nhẹ ở cuối.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 05 — Mục tiêu hệ thống

**Lời thuyết trình:**
1. Mục tiêu là xây dựng một website PC Store có luồng nghiệp vụ thống nhất.
2. Phần sản phẩm hỗ trợ khám phá, lọc, xem chi tiết và so sánh.
3. Phần giao dịch bao gồm giỏ hàng, checkout, thanh toán và theo dõi đơn.
4. Phần vận hành gồm quản trị dữ liệu, chat hỗ trợ và tiếp nhận bảo hành.

**Animation suggestion:**
- Vòng tròn trung tâm Zoom trong 0,45 giây.
- Năm mục tiêu xuất hiện theo chiều kim đồng hồ.
- Thông điệp cuối Fade sau cùng trong 0,35 giây.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 06 — Đối tượng & phạm vi

**Lời thuyết trình:**
1. Hệ thống phục vụ bốn nhóm tương tác: khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.
2. Nhân viên hỗ trợ là vai trò Staff hoặc Admin truy cập màn hình Admin Chat.
3. Phạm vi hiện tại tập trung vào website thương mại điện tử và các tích hợp có trong source.
4. Mobile native, AI và cổng thanh toán production được xác định là ngoài phạm vi hiện tại.

**Animation suggestion:**
- Bốn persona Fade đồng thời trong 0,5 giây.
- Khung trong phạm vi Wipe từ trái.
- Khung ngoài phạm vi Wipe từ phải, trễ 0,2 giây.

**Thời lượng gợi ý:** 45–55 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 07 — Phân tích & thiết kế

**Lời thuyết trình:**
1. Tiếp theo là phần phân tích & thiết kế.
2. Từ yêu cầu nghiệp vụ đến kiến trúc và dữ liệu.
3. Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
4. Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

**Animation suggestion:**
- Số section Fade trong 0,5 giây.
- Tiêu đề Wipe từ trái trong 0,6 giây.
- Biểu tượng section Zoom nhẹ sau cùng.

**Thời lượng gợi ý:** 15–20 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 08 — Yêu cầu chức năng

**Lời thuyết trình:**
1. Yêu cầu được chia theo hai phía chính là khách hàng và quản trị viên.
2. Khách hàng đi qua chuỗi khám phá, lựa chọn, giao dịch và hậu mãi.
3. Quản trị viên chịu trách nhiệm dữ liệu, đơn hàng, người dùng và cấu hình vận hành.
4. Các yêu cầu này ánh xạ trực tiếp tới controller, view và service trong source.

**Animation suggestion:**
- Hai tiêu đề cột xuất hiện đồng thời.
- Các yêu cầu khách hàng Wipe theo nhóm trong 0,6 giây.
- Các yêu cầu quản trị xuất hiện sau, trễ 0,2 giây.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 09 — Use case tổng quan

**Lời thuyết trình:**
1. Sơ đồ đặt PC Store ở trung tâm với hai tác nhân chính.
2. Khách hàng tương tác với tìm kiếm, giỏ hàng, checkout, Build PC và hỗ trợ.
3. Admin tương tác với quản lý dữ liệu, đơn hàng, chat và bảo hành.
4. Một số use case dùng chung nhưng quyền truy cập được kiểm soát theo vai trò.

**Animation suggestion:**
- Node PC Store Zoom trước.
- Các use case bung ra theo quỹ đạo bằng Fade lần lượt.
- Hai actor và đường kết nối xuất hiện sau cùng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 10 — Kiến trúc hệ thống

**Lời thuyết trình:**
1. Kiến trúc chính đi từ trình duyệt qua Controller, Service, EF Core tới SQL Server.
2. MVC đảm nhiệm routing, xác thực và phối hợp dữ liệu cho Razor View.
3. Service chứa các nghiệp vụ như giỏ hàng, tương thích Build PC, vận chuyển và email.
4. Các nhánh SignalR, GHN, SMTP, QR và định tuyến đều có đăng ký hoặc luồng xử lý thật trong source.

**Animation suggestion:**
- Năm lớp chính Wipe từ trái sang phải trong 1 giây.
- Mũi tên luồng chính xuất hiện cùng từng node.
- Năm tích hợp Fade theo nhóm ở cuối.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 11 — Công nghệ sử dụng

**Lời thuyết trình:**
1. Nền tảng chính là .NET 8 với ASP.NET Core MVC và Razor View.
2. EF Core làm việc với SQL Server để quản lý dữ liệu quan hệ.
3. Bootstrap, CSS và JavaScript tạo giao diện responsive và tương tác phía client.
4. SignalR, GHN và SMTP mở rộng hệ thống cho chat, vận chuyển và email.

**Animation suggestion:**
- Bento grid xuất hiện theo hai hàng.
- Bốn card hàng trên Fade trong 0,5 giây.
- Bốn card hàng dưới Fade sau 0,2 giây.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 12 — Cơ sở dữ liệu

**Lời thuyết trình:**
1. ApplicationDbContext hiện khai báo 22 DbSet.
2. Sơ đồ gom các bảng thành tám cụm nghiệp vụ để dễ quan sát.
3. Các cụm chính gồm tài khoản, sản phẩm, giỏ, đơn, vận chuyển, chat, bảo hành và Build PC.
4. Ngoài ra source còn có banner, bài viết, feedback và cấu hình website.

**Animation suggestion:**
- Khối SQL Server Zoom trước trong 0,4 giây.
- Tám cụm dữ liệu xuất hiện theo cặp đối xứng.
- Các đường liên kết Fade cùng từng cụm.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 13 — Quan hệ dữ liệu chính

**Lời thuyết trình:**
1. Luồng dữ liệu giao dịch đi từ User đến Order, OrderDetail và Product.
2. Product thuộc Category và có thể có nhiều ProductImage.
3. CartItem và BuildPcItem đều tham chiếu về sản phẩm.
4. Chat và Warranty liên kết với người dùng hoặc sản phẩm theo đúng cấu hình EF Core.

**Animation suggestion:**
- Hàng entity giao dịch xuất hiện trước.
- Các quan hệ chính Wipe từ trái sang phải.
- Hàng dữ liệu bổ trợ và đường nét đứt Fade sau cùng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 14 — Triển khai website

**Lời thuyết trình:**
1. Tiếp theo là phần triển khai website.
2. Trải nghiệm người dùng, giao dịch và hỗ trợ realtime.
3. Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
4. Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

**Animation suggestion:**
- Số section Fade trong 0,5 giây.
- Tiêu đề Wipe từ trái trong 0,6 giây.
- Biểu tượng section Zoom nhẹ sau cùng.

**Thời lượng gợi ý:** 15–20 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 15 — Trang chủ

**Lời thuyết trình:**
1. Trang chủ là điểm chạm đầu tiên của hành trình mua hàng.
2. HomeIndexVm cung cấp banner, danh mục và nhiều nhóm sản phẩm nổi bật.
3. Giao diện hướng người dùng tới khuyến mãi và các nhóm sản phẩm chính.
4. Ảnh thực tế nên thể hiện rõ banner và ít nhất hai section sản phẩm.

**Animation suggestion:**
- Khung trình duyệt Push từ trái trong 0,6 giây.
- Placeholder ảnh Fade ngay sau khung.
- Ba điểm nhấn bên phải xuất hiện lần lượt.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh thật trang chủ tại /Home/Index, ưu tiên khung nhìn desktop đầy đủ banner và nhóm sản phẩm.

## Slide 16 — Danh sách sản phẩm

**Lời thuyết trình:**
1. Trang sản phẩm hỗ trợ tìm kiếm theo từ khóa và lọc theo nhiều tiêu chí.
2. ProductFilterVm có danh mục, hãng, khoảng giá, CPU, RAM và GPU.
3. Kết quả được trình bày dạng lưới để người dùng quét nhanh thông tin chính.
4. Ảnh chụp nên mở sidebar bộ lọc để thể hiện rõ khả năng thu hẹp lựa chọn.

**Animation suggestion:**
- Sidebar Wipe từ trái trong 0,4 giây.
- Lưới sản phẩm Wipe từ phải trong 0,6 giây.
- Các chip tiêu chí Fade đồng thời.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Products với sidebar bộ lọc và lưới sản phẩm; chọn dữ liệu có nhiều hãng và mức giá.

## Slide 17 — Chi tiết sản phẩm

**Lời thuyết trình:**
1. Trang chi tiết tập trung các thông tin ra quyết định của một sản phẩm.
2. Model Product có giá, giá giảm, tồn kho, thông số, khuyến mãi và bảo hành.
3. Người dùng có thể chọn số lượng và thêm sản phẩm vào giỏ.
4. Ảnh thực tế nên chọn sản phẩm có đủ khuyến mãi và thông số để slide giàu thông tin.

**Animation suggestion:**
- Ảnh sản phẩm Push từ trái.
- Khối thông tin Push từ phải.
- Nút thêm vào giỏ Pulse nhẹ sau cùng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh trang /Products/Detail/{id}; chọn sản phẩm có giá, khuyến mãi, thông số và bảo hành.

## Slide 18 — Giỏ hàng

**Lời thuyết trình:**
1. Giỏ hàng tiếp nhận sản phẩm từ trang chi tiết hoặc thao tác mua ngay.
2. Người dùng có thể cập nhật số lượng, xóa từng dòng hoặc làm trống giỏ.
3. CartService tính lại thành tiền dựa trên sản phẩm và số lượng hiện tại.
4. Từ giỏ hàng, người dùng chuyển sang checkout để nhập thông tin nhận hàng.

**Animation suggestion:**
- Placeholder giỏ Fade trước.
- Bốn bước xuất hiện từ trên xuống, mỗi bước 0,25 giây.
- Card tổng đơn Zoom nhẹ sau bước cuối.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Cart có ít nhất hai sản phẩm, số lượng và tổng tiền.

## Slide 19 — Checkout & vận chuyển

**Lời thuyết trình:**
1. Checkout được tổ chức thành năm bước logic từ thông tin đến theo dõi.
2. Địa chỉ được chọn theo tỉnh, huyện và xã qua dịch vụ địa chỉ GHN.
3. Phí vận chuyển dùng GHN hoặc công thức nội bộ tùy chính sách cấu hình.
4. Khi xác nhận, hệ thống tạo Order, OrderDetail và chuyển tới trạng thái phù hợp.

**Animation suggestion:**
- Thông điệp mở đầu Fade trong 0,4 giây.
- Timeline chạy Wipe từ trái sang phải trong 1 giây.
- Khối thông tin kỹ thuật xuất hiện sau timeline.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 20 — Thanh toán QR / chuyển khoản

**Lời thuyết trình:**
1. Với chuyển khoản, hệ thống tạo trang hướng dẫn thanh toán và mã QR ngân hàng.
2. Thông tin gồm mã đơn, số tiền và nội dung chuyển khoản duy nhất.
3. Order lưu PaymentExpireAt để giới hạn thời gian thanh toán.
4. Khách có thể xác nhận đã chuyển khoản, sau đó admin kiểm tra và xử lý.

**Animation suggestion:**
- Cột thông tin đơn Wipe từ trái.
- Khung QR Push từ phải trong 0,6 giây.
- Cảnh báo thời hạn Fade và Pulse nhẹ.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Orders/BankTransfer?id={id}, hiển thị QR, thông tin chuyển khoản và đồng hồ thời hạn.

## Slide 21 — Theo dõi đơn hàng

**Lời thuyết trình:**
1. Theo dõi đơn sử dụng các trạng thái được khai báo trong enum OrderStatus.
2. Luồng chính đi từ chờ thanh toán hoặc chờ xác nhận đến xử lý, giao hàng và hoàn thành.
3. Cancelled và Expired là hai nhánh kết thúc ngoài luồng thành công.
4. Trang tracking có endpoint cập nhật trạng thái để giao diện phản ánh tiến trình hiện tại.

**Animation suggestion:**
- Ảnh tracking Fade trước.
- Đường trạng thái Wipe từ trái sang phải trong 1 giây.
- Cancelled và Expired xuất hiện cuối như hai nhánh phụ.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Order/Tracking/{id} hoặc /Order/Lookup với timeline trạng thái đơn thực tế.

## Slide 22 — Build PC

**Lời thuyết trình:**
1. Build PC trình bày linh kiện theo từng vị trí cấu hình.
2. Source hỗ trợ CPU, mainboard, RAM, lưu trữ, GPU, nguồn và case.
3. BuildCompatibilityService kiểm tra socket CPU với mainboard và loại RAM.
4. Tổng giá được cập nhật từ các sản phẩm đã chọn và cấu hình có thể đưa vào giỏ.

**Animation suggestion:**
- Các ô linh kiện xuất hiện theo hàng bằng Fade.
- Placeholder Build PC Push từ trái.
- Card tổng giá Zoom và Pulse nhẹ sau cùng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /BuildPc với một cấu hình đã chọn CPU, mainboard, RAM, SSD, VGA, PSU và case.

## Slide 23 — So sánh sản phẩm

**Lời thuyết trình:**
1. Module Compare lưu tối đa hai sản phẩm trong session.
2. Màn hình đối chiếu giá, thông số, khuyến mãi và tồn kho theo cùng hàng.
3. Compare ViewModel chuẩn hóa các dòng thông số để dễ đọc giữa hai sản phẩm.
4. Ảnh chụp nên chọn hai sản phẩm cùng loại để thể hiện giá trị của phép so sánh.

**Animation suggestion:**
- Hai tiêu đề sản phẩm Wipe từ hai phía.
- Các hàng tiêu chí xuất hiện từ trên xuống.
- Placeholder ảnh Fade ở cuối.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Compare với hai sản phẩm cùng nhóm để các hàng thông số có ý nghĩa.

## Slide 24 — Chat hỗ trợ SignalR

**Lời thuyết trình:**
1. Chat hỗ trợ kết nối widget khách hàng với màn hình Admin Chat qua SignalR Hub.
2. Hệ thống lưu Conversation và Message trong cơ sở dữ liệu.
3. Khách vãng lai dùng access token, còn người dùng đăng nhập được liên kết bằng UserId.
4. Cùng với bảo hành và feedback, chat tạo thành nhóm dịch vụ sau bán hàng.

**Animation suggestion:**
- Ba node hệ thống Fade đồng thời.
- Hai chiều mũi tên Wipe trong 0,5 giây.
- Ảnh chat và ba card hậu mãi xuất hiện sau.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh widget chat phía khách và, nếu đủ chỗ, ảnh màn hình /AdminChat ở cùng hội thoại.

## Slide 25 — Đánh giá & phát triển

**Lời thuyết trình:**
1. Tiếp theo là phần đánh giá & phát triển.
2. Nhìn lại kết quả, giới hạn và lộ trình tiếp theo.
3. Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
4. Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

**Animation suggestion:**
- Số section Fade trong 0,5 giây.
- Tiêu đề Wipe từ trái trong 0,6 giây.
- Biểu tượng section Zoom nhẹ sau cùng.

**Thời lượng gợi ý:** 15–20 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 26 — Trang quản trị

**Lời thuyết trình:**
1. Khu vực quản trị tập trung hoạt động vận hành của cửa hàng.
2. Dashboard tổng hợp số liệu sản phẩm, đơn hàng, người dùng và doanh thu theo ViewModel.
3. Các controller riêng quản lý sản phẩm, danh mục, đơn, người dùng, bảo hành, banner và cài đặt.
4. Quyền truy cập được giới hạn cho vai trò Admin hoặc Staff tùy màn hình.

**Animation suggestion:**
- Khung dashboard Push từ trái trong 0,6 giây.
- Sidebar mô phỏng xuất hiện cùng khung.
- Năm card quản lý Wipe từ trên xuống.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Chèn ảnh /Admin (AdminDashboard/Index) có các KPI hoặc bảng đơn hàng gần đây.

## Slide 27 — Kết quả đạt được

**Lời thuyết trình:**
1. Kết quả source hiện tại có 22 DbSet và 21 controller.
2. Deck nhóm công nghệ thành tám khối đại diện cho nền tảng, dữ liệu, UI và tích hợp.
3. Bốn nhóm người dùng được thể hiện gồm khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.
4. Quan trọng hơn số lượng là hệ thống đã nối được hành trình mua hàng với vận hành sau bán.

**Animation suggestion:**
- Thông điệp kết quả Fade trước.
- Bốn stats card Count Up theo thứ tự trái sang phải.
- Thanh kết quả nghiệp vụ Wipe sau cùng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 28 — Hạn chế

**Lời thuyết trình:**
1. Các hạn chế được nhìn nhận ở mức phù hợp với phạm vi đồ án.
2. Luồng QR hiện là chuyển khoản và xác nhận, chưa phải đối soát gateway tự động.
3. Các tích hợp ngoài như GHN, SMTP và định tuyến phụ thuộc khóa cấu hình và môi trường.
4. Báo cáo, mobile UX và khả năng quan sát lỗi là những điểm có thể tiếp tục hoàn thiện.

**Animation suggestion:**
- Cột hiện tại Wipe từ trái.
- Cột cần cải thiện Wipe từ phải sau 0,2 giây.
- Các cặp nội dung xuất hiện theo từng hàng.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 29 — Hướng phát triển

**Lời thuyết trình:**
1. Lộ trình phát triển bắt đầu bằng payment gateway và đối soát tự động.
2. AI Build PC có thể tư vấn cấu hình theo ngân sách và nhu cầu.
3. Báo cáo nâng cao giúp admin hiểu doanh thu, tồn kho và hành vi mua.
4. SEO và mobile UX hoàn thiện khả năng tiếp cận và trải nghiệm đa thiết bị.

**Animation suggestion:**
- Đường roadmap Wipe theo hướng 01 đến 05 trong 1,2 giây.
- Mỗi mốc Zoom khi đường đi tới vị trí tương ứng.
- Tên roadmap Fade ngay từ đầu.

**Thời lượng gợi ý:** 40–50 giây

**Ghi chú ảnh:** Không cần chèn ảnh.

## Slide 30 — Xin chân thành cảm ơn

**Lời thuyết trình:**
1. Em xin chân thành cảm ơn thầy cô và hội đồng đã lắng nghe phần trình bày.
2. Đề tài đã hoàn thành các luồng chính của một website PC Store trong phạm vi đồ án.
3. Em mong nhận được nhận xét để tiếp tục cải thiện cả kỹ thuật và trải nghiệm người dùng.
4. Em xin sẵn sàng trả lời các câu hỏi của hội đồng.

**Animation suggestion:**
- Khung kính Fade trong 0,5 giây.
- Tiêu đề Zoom rất nhẹ trong 0,6 giây.
- Dòng Q&A Fade sau cùng; giữ slide tĩnh khi trả lời.

**Thời lượng gợi ý:** 20–30 giây trước phần hỏi đáp

**Ghi chú ảnh:** Không cần chèn ảnh.
