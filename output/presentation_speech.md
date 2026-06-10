# Lời thuyết trình — DATN PC Store

> Tài liệu gồm đúng 30 phần, khớp thứ tự và tiêu đề trong PowerPoint.
> Animation chỉ là gợi ý thao tác thủ công, không nhúng hiệu ứng phức tạp vào file.

## Slide 01 — DATN PC STORE

### Lời thuyết trình

Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.
Đề tài xây dựng website bán PC và linh kiện máy tính trên ASP.NET Core MVC.
Hệ thống kết nối trải nghiệm mua hàng, vận chuyển, hỗ trợ và quản trị.
Bài trình bày đi từ vấn đề thực tế đến thiết kế, triển khai và đánh giá.

### Ý chính cần nhấn mạnh

* Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.
* Bài trình bày đi từ vấn đề thực tế đến thiết kế, triển khai và đánh giá.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ đặt vấn đề.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Tiêu đề Fade trước trong 0,6 giây.
* Các chip công nghệ xuất hiện lần lượt sau tiêu đề.
* Khối quy trình bên phải Wipe từ trên xuống trong 0,8 giây.

* Thời lượng gợi ý: 35–45 giây.

## Slide 02 — Đặt vấn đề

### Lời thuyết trình

Thị trường linh kiện có nhiều lựa chọn và thông số kỹ thuật phức tạp.
Người mua cần tìm nhanh nhưng vẫn phải chọn đúng thành phần phù hợp nhu cầu.
Khả năng so sánh và theo dõi đơn giúp giảm sự không chắc chắn trong quyết định mua.
Đây là vấn đề trung tâm mà hệ thống PC Store hướng tới giải quyết.

### Ý chính cần nhấn mạnh

* Thị trường linh kiện có nhiều lựa chọn và thông số kỹ thuật phức tạp.
* Đây là vấn đề trung tâm mà hệ thống PC Store hướng tới giải quyết.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ những khó khăn thực tế.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Biểu tượng cảnh báo Zoom nhẹ trong 0,3 giây.
* Câu statement Fade trong 0,6 giây.
* Ba từ khóa xuất hiện nối tiếp bằng Wipe, mỗi mục 0,25 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 03 — Những khó khăn thực tế

### Lời thuyết trình

Khó khăn đầu tiên là tìm đúng sản phẩm trong một danh mục linh kiện lớn.
Khó khăn thứ hai là so sánh các thông số được trình bày theo nhiều cách khác nhau.
Khó khăn thứ ba là theo dõi xuyên suốt thanh toán, xử lý và giao hàng.
Ba điểm đau này định hướng trực tiếp cho các module cốt lõi của đề tài.

### Ý chính cần nhấn mạnh

* Khó khăn đầu tiên là tìm đúng sản phẩm trong một danh mục linh kiện lớn.
* Ba điểm đau này định hướng trực tiếp cho các module cốt lõi của đề tài.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ lý do chọn đề tài.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Card 01 Float In từ trái trong 0,4 giây.
* Card 02 Fade sau 0,2 giây.
* Card 03 Float In từ phải trong 0,4 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 04 — Lý do chọn đề tài

### Lời thuyết trình

Về thực tế, đề tài giải quyết một hành trình mua linh kiện có nhiều điểm ra quyết định.
Về kỹ thuật, bài toán đủ rộng để áp dụng MVC, dữ liệu quan hệ và tích hợp dịch vụ.
Source code hiện có cả luồng khách hàng, quản trị và hỗ trợ thời gian thực.
Vì vậy đề tài vừa phù hợp nhu cầu thực tiễn vừa thể hiện năng lực phát triển web.

### Ý chính cần nhấn mạnh

* Về thực tế, đề tài giải quyết một hành trình mua linh kiện có nhiều điểm ra quyết định.
* Vì vậy đề tài vừa phù hợp nhu cầu thực tiễn vừa thể hiện năng lực phát triển web.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ mục tiêu hệ thống.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Cột thực tế Wipe từ trái trong 0,5 giây.
* Cột kỹ thuật Wipe từ phải trong 0,5 giây.
* Badge ASP.NET Core MVC Pulse nhẹ ở cuối.

* Thời lượng gợi ý: 40–50 giây.

## Slide 05 — Mục tiêu hệ thống

### Lời thuyết trình

Mục tiêu là xây dựng một website PC Store có luồng nghiệp vụ thống nhất.
Phần sản phẩm hỗ trợ khám phá, lọc, xem chi tiết và so sánh.
Phần giao dịch bao gồm giỏ hàng, checkout, thanh toán và theo dõi đơn.
Phần vận hành gồm quản trị dữ liệu, chat hỗ trợ và tiếp nhận bảo hành.

### Ý chính cần nhấn mạnh

* Mục tiêu là xây dựng một website PC Store có luồng nghiệp vụ thống nhất.
* Phần vận hành gồm quản trị dữ liệu, chat hỗ trợ và tiếp nhận bảo hành.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ đối tượng & phạm vi.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Vòng tròn trung tâm Zoom trong 0,45 giây.
* Năm mục tiêu xuất hiện theo chiều kim đồng hồ.
* Thông điệp cuối Fade sau cùng trong 0,35 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 06 — Đối tượng & phạm vi

### Lời thuyết trình

Hệ thống phục vụ bốn nhóm tương tác: khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.
Nhân viên hỗ trợ là vai trò Staff hoặc Admin truy cập màn hình Admin Chat.
Phạm vi hiện tại tập trung vào website thương mại điện tử và các tích hợp có trong source.
Mobile native, AI và cổng thanh toán production được xác định là ngoài phạm vi hiện tại.

### Ý chính cần nhấn mạnh

* Hệ thống phục vụ bốn nhóm tương tác: khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.
* Mobile native, AI và cổng thanh toán production được xác định là ngoài phạm vi hiện tại.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ phân tích & thiết kế.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Bốn persona Fade đồng thời trong 0,5 giây.
* Khung trong phạm vi Wipe từ trái.
* Khung ngoài phạm vi Wipe từ phải, trễ 0,2 giây.

* Thời lượng gợi ý: 45–55 giây.

## Slide 07 — Phân tích & thiết kế

### Lời thuyết trình

Tiếp theo là phần phân tích & thiết kế.
Từ yêu cầu nghiệp vụ đến kiến trúc và dữ liệu.
Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Ý chính cần nhấn mạnh

* Tiếp theo là phần phân tích & thiết kế.
* Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ yêu cầu chức năng.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Số section Fade trong 0,5 giây.
* Tiêu đề Wipe từ trái trong 0,6 giây.
* Biểu tượng section Zoom nhẹ sau cùng.

* Thời lượng gợi ý: 15–20 giây.

## Slide 08 — Yêu cầu chức năng

### Lời thuyết trình

Yêu cầu được chia theo hai phía chính là khách hàng và quản trị viên.
Khách hàng đi qua chuỗi khám phá, lựa chọn, giao dịch và hậu mãi.
Quản trị viên chịu trách nhiệm dữ liệu, đơn hàng, người dùng và cấu hình vận hành.
Các yêu cầu này ánh xạ trực tiếp tới controller, view và service trong source.

### Ý chính cần nhấn mạnh

* Yêu cầu được chia theo hai phía chính là khách hàng và quản trị viên.
* Các yêu cầu này ánh xạ trực tiếp tới controller, view và service trong source.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ use case tổng quan.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Hai tiêu đề cột xuất hiện đồng thời.
* Các yêu cầu khách hàng Wipe theo nhóm trong 0,6 giây.
* Các yêu cầu quản trị xuất hiện sau, trễ 0,2 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 09 — Use case tổng quan

### Lời thuyết trình

Sơ đồ đặt PC Store ở trung tâm với hai tác nhân chính.
Khách hàng tương tác với tìm kiếm, giỏ hàng, checkout, Build PC và hỗ trợ.
Admin tương tác với quản lý dữ liệu, đơn hàng, chat và bảo hành.
Một số use case dùng chung nhưng quyền truy cập được kiểm soát theo vai trò.

### Ý chính cần nhấn mạnh

* Sơ đồ đặt PC Store ở trung tâm với hai tác nhân chính.
* Một số use case dùng chung nhưng quyền truy cập được kiểm soát theo vai trò.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ kiến trúc hệ thống.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Node PC Store Zoom trước.
* Các use case bung ra theo quỹ đạo bằng Fade lần lượt.
* Hai actor và đường kết nối xuất hiện sau cùng.

* Thời lượng gợi ý: 40–50 giây.

## Slide 10 — Kiến trúc hệ thống

### Lời thuyết trình

Kiến trúc chính đi từ trình duyệt qua Controller, Service, EF Core tới SQL Server.
MVC đảm nhiệm routing, xác thực và phối hợp dữ liệu cho Razor View.
Service chứa các nghiệp vụ như giỏ hàng, tương thích Build PC, vận chuyển và email.
Các nhánh SignalR, GHN, SMTP, QR và định tuyến đều có đăng ký hoặc luồng xử lý thật trong source.

### Ý chính cần nhấn mạnh

* Kiến trúc chính đi từ trình duyệt qua Controller, Service, EF Core tới SQL Server.
* Các nhánh SignalR, GHN, SMTP, QR và định tuyến đều có đăng ký hoặc luồng xử lý thật trong source.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ công nghệ sử dụng.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Năm lớp chính Wipe từ trái sang phải trong 1 giây.
* Mũi tên luồng chính xuất hiện cùng từng node.
* Năm tích hợp Fade theo nhóm ở cuối.

* Thời lượng gợi ý: 40–50 giây.

## Slide 11 — Công nghệ sử dụng

### Lời thuyết trình

Nền tảng chính là .NET 8 với ASP.NET Core MVC và Razor View.
EF Core làm việc với SQL Server để quản lý dữ liệu quan hệ.
Bootstrap, CSS và JavaScript tạo giao diện responsive và tương tác phía client.
SignalR, GHN và SMTP mở rộng hệ thống cho chat, vận chuyển và email.

### Ý chính cần nhấn mạnh

* Nền tảng chính là .NET 8 với ASP.NET Core MVC và Razor View.
* SignalR, GHN và SMTP mở rộng hệ thống cho chat, vận chuyển và email.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ cơ sở dữ liệu.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Bento grid xuất hiện theo hai hàng.
* Bốn card hàng trên Fade trong 0,5 giây.
* Bốn card hàng dưới Fade sau 0,2 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 12 — Cơ sở dữ liệu

### Lời thuyết trình

ApplicationDbContext hiện khai báo 22 DbSet.
Sơ đồ gom các bảng thành tám cụm nghiệp vụ để dễ quan sát.
Các cụm chính gồm tài khoản, sản phẩm, giỏ, đơn, vận chuyển, chat, bảo hành và Build PC.
Ngoài ra source còn có banner, bài viết, feedback và cấu hình website.

### Ý chính cần nhấn mạnh

* ApplicationDbContext hiện khai báo 22 DbSet.
* Ngoài ra source còn có banner, bài viết, feedback và cấu hình website.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ quan hệ dữ liệu chính.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Khối SQL Server Zoom trước trong 0,4 giây.
* Tám cụm dữ liệu xuất hiện theo cặp đối xứng.
* Các đường liên kết Fade cùng từng cụm.

* Thời lượng gợi ý: 40–50 giây.

## Slide 13 — Quan hệ dữ liệu chính

### Lời thuyết trình

Luồng dữ liệu giao dịch đi từ User đến Order, OrderDetail và Product.
Product thuộc Category và có thể có nhiều ProductImage.
CartItem và BuildPcItem đều tham chiếu về sản phẩm.
Chat và Warranty liên kết với người dùng hoặc sản phẩm theo đúng cấu hình EF Core.

### Ý chính cần nhấn mạnh

* Luồng dữ liệu giao dịch đi từ User đến Order, OrderDetail và Product.
* Chat và Warranty liên kết với người dùng hoặc sản phẩm theo đúng cấu hình EF Core.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ demo website khách hàng.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Hàng entity giao dịch xuất hiện trước.
* Các quan hệ chính Wipe từ trái sang phải.
* Hàng dữ liệu bổ trợ và đường nét đứt Fade sau cùng.

* Thời lượng gợi ý: 40–50 giây.

## Slide 14 — Demo website khách hàng

### Lời thuyết trình

Tiếp theo là phần demo website khách hàng.
Từ khám phá sản phẩm đến đặt hàng và theo dõi.
Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Ý chính cần nhấn mạnh

* Tiếp theo là phần demo website khách hàng.
* Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ trang chủ.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Số section Fade trong 0,5 giây.
* Tiêu đề Wipe từ trái trong 0,6 giây.
* Biểu tượng section Zoom nhẹ sau cùng.

* Thời lượng gợi ý: 15–20 giây.

## Slide 15 — Trang chủ

### Lời thuyết trình

Trang chủ là điểm chạm đầu tiên của hành trình mua hàng.
HomeIndexVm cung cấp banner, danh mục và nhiều nhóm sản phẩm nổi bật.
Giao diện hướng người dùng tới khuyến mãi và các nhóm sản phẩm chính.
Ảnh thực tế nên thể hiện rõ banner và ít nhất hai section sản phẩm.

### Ý chính cần nhấn mạnh

* Trang chủ là điểm chạm đầu tiên của hành trình mua hàng.
* Ảnh thực tế nên thể hiện rõ banner và ít nhất hai section sản phẩm.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ danh sách sản phẩm.

### Ảnh cần chèn

* Chèn ảnh thật trang chủ tại /Home/Index, ưu tiên khung nhìn desktop đầy đủ banner và nhóm sản phẩm.

### Gợi ý animation thủ công

* Khung trình duyệt Push từ trái trong 0,6 giây.
* Placeholder ảnh Fade ngay sau khung.
* Ba điểm nhấn bên phải xuất hiện lần lượt.

* Thời lượng gợi ý: 40–50 giây.

## Slide 16 — Danh sách sản phẩm

### Lời thuyết trình

Trang sản phẩm hỗ trợ tìm kiếm theo từ khóa và lọc theo nhiều tiêu chí.
ProductFilterVm có danh mục, hãng, khoảng giá, CPU, RAM và GPU.
Kết quả được trình bày dạng lưới để người dùng quét nhanh thông tin chính.
Ảnh chụp nên mở sidebar bộ lọc để thể hiện rõ khả năng thu hẹp lựa chọn.

### Ý chính cần nhấn mạnh

* Trang sản phẩm hỗ trợ tìm kiếm theo từ khóa và lọc theo nhiều tiêu chí.
* Ảnh chụp nên mở sidebar bộ lọc để thể hiện rõ khả năng thu hẹp lựa chọn.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ chi tiết sản phẩm.

### Ảnh cần chèn

* Chèn ảnh /Products với sidebar bộ lọc và lưới sản phẩm; chọn dữ liệu có nhiều hãng và mức giá.

### Gợi ý animation thủ công

* Sidebar Wipe từ trái trong 0,4 giây.
* Lưới sản phẩm Wipe từ phải trong 0,6 giây.
* Các chip tiêu chí Fade đồng thời.

* Thời lượng gợi ý: 40–50 giây.

## Slide 17 — Chi tiết sản phẩm

### Lời thuyết trình

Trang chi tiết tập trung các thông tin ra quyết định của một sản phẩm.
Model Product có giá, giá giảm, tồn kho, thông số, khuyến mãi và bảo hành.
Người dùng có thể chọn số lượng và thêm sản phẩm vào giỏ.
Ảnh thực tế nên chọn sản phẩm có đủ khuyến mãi và thông số để slide giàu thông tin.

### Ý chính cần nhấn mạnh

* Trang chi tiết tập trung các thông tin ra quyết định của một sản phẩm.
* Ảnh thực tế nên chọn sản phẩm có đủ khuyến mãi và thông số để slide giàu thông tin.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ giỏ hàng.

### Ảnh cần chèn

* Chèn ảnh trang /Products/Detail/{id}; chọn sản phẩm có giá, khuyến mãi, thông số và bảo hành.

### Gợi ý animation thủ công

* Ảnh sản phẩm Push từ trái.
* Khối thông tin Push từ phải.
* Nút thêm vào giỏ Pulse nhẹ sau cùng.

* Thời lượng gợi ý: 40–50 giây.

## Slide 18 — Giỏ hàng

### Lời thuyết trình

Giỏ hàng tiếp nhận sản phẩm từ trang chi tiết hoặc thao tác mua ngay.
Người dùng có thể cập nhật số lượng, xóa từng dòng hoặc làm trống giỏ.
CartService tính lại thành tiền dựa trên sản phẩm và số lượng hiện tại.
Từ giỏ hàng, người dùng chuyển sang checkout để nhập thông tin nhận hàng.

### Ý chính cần nhấn mạnh

* Giỏ hàng tiếp nhận sản phẩm từ trang chi tiết hoặc thao tác mua ngay.
* Từ giỏ hàng, người dùng chuyển sang checkout để nhập thông tin nhận hàng.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ checkout & vận chuyển.

### Ảnh cần chèn

* Chèn ảnh /Cart có ít nhất hai sản phẩm, số lượng và tổng tiền.

### Gợi ý animation thủ công

* Placeholder giỏ Fade trước.
* Bốn bước xuất hiện từ trên xuống, mỗi bước 0,25 giây.
* Card tổng đơn Zoom nhẹ sau bước cuối.

* Thời lượng gợi ý: 40–50 giây.

## Slide 19 — Checkout & vận chuyển

### Lời thuyết trình

Checkout được tổ chức thành năm bước logic từ thông tin đến theo dõi.
Địa chỉ được chọn theo tỉnh, huyện và xã qua dịch vụ địa chỉ GHN.
Phí vận chuyển dùng GHN hoặc công thức nội bộ tùy chính sách cấu hình.
Khi xác nhận, hệ thống tạo Order, OrderDetail và chuyển tới trạng thái phù hợp.

### Ý chính cần nhấn mạnh

* Checkout được tổ chức thành năm bước logic từ thông tin đến theo dõi.
* Khi xác nhận, hệ thống tạo Order, OrderDetail và chuyển tới trạng thái phù hợp.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ thanh toán qr / chuyển khoản.

### Ảnh cần chèn

* Có, URL: /Checkout; chụp biểu mẫu địa chỉ, vận chuyển và thanh toán.

### Gợi ý animation thủ công

* Thông điệp mở đầu Fade trong 0,4 giây.
* Timeline chạy Wipe từ trái sang phải trong 1 giây.
* Khối thông tin kỹ thuật xuất hiện sau timeline.

* Thời lượng gợi ý: 40–50 giây.

## Slide 20 — Thanh toán QR / chuyển khoản

### Lời thuyết trình

Với chuyển khoản, hệ thống tạo trang hướng dẫn thanh toán và mã QR ngân hàng.
Thông tin gồm mã đơn, số tiền và nội dung chuyển khoản duy nhất.
Order lưu PaymentExpireAt để giới hạn thời gian thanh toán.
Khách có thể xác nhận đã chuyển khoản, sau đó admin kiểm tra và xử lý.

### Ý chính cần nhấn mạnh

* Với chuyển khoản, hệ thống tạo trang hướng dẫn thanh toán và mã QR ngân hàng.
* Khách có thể xác nhận đã chuyển khoản, sau đó admin kiểm tra và xử lý.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ theo dõi đơn hàng.

### Ảnh cần chèn

* Chèn ảnh /Orders/BankTransfer?id={id}, hiển thị QR, thông tin chuyển khoản và đồng hồ thời hạn.

### Gợi ý animation thủ công

* Cột thông tin đơn Wipe từ trái.
* Khung QR Push từ phải trong 0,6 giây.
* Cảnh báo thời hạn Fade và Pulse nhẹ.

* Thời lượng gợi ý: 40–50 giây.

## Slide 21 — Theo dõi đơn hàng

### Lời thuyết trình

Theo dõi đơn sử dụng các trạng thái được khai báo trong enum OrderStatus.
Luồng chính đi từ chờ thanh toán hoặc chờ xác nhận đến xử lý, giao hàng và hoàn thành.
Cancelled và Expired là hai nhánh kết thúc ngoài luồng thành công.
Trang tracking có endpoint cập nhật trạng thái để giao diện phản ánh tiến trình hiện tại.

### Ý chính cần nhấn mạnh

* Theo dõi đơn sử dụng các trạng thái được khai báo trong enum OrderStatus.
* Trang tracking có endpoint cập nhật trạng thái để giao diện phản ánh tiến trình hiện tại.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ build pc.

### Ảnh cần chèn

* Chèn ảnh /Order/Tracking/{id} hoặc /Order/Lookup với timeline trạng thái đơn thực tế.

### Gợi ý animation thủ công

* Ảnh tracking Fade trước.
* Đường trạng thái Wipe từ trái sang phải trong 1 giây.
* Cancelled và Expired xuất hiện cuối như hai nhánh phụ.

* Thời lượng gợi ý: 40–50 giây.

## Slide 22 — Build PC

### Lời thuyết trình

Build PC trình bày linh kiện theo từng vị trí cấu hình.
Source hỗ trợ CPU, mainboard, RAM, lưu trữ, GPU, nguồn và case.
BuildCompatibilityService kiểm tra socket CPU với mainboard và loại RAM.
Tổng giá được cập nhật từ các sản phẩm đã chọn và cấu hình có thể đưa vào giỏ.

### Ý chính cần nhấn mạnh

* Build PC trình bày linh kiện theo từng vị trí cấu hình.
* Tổng giá được cập nhật từ các sản phẩm đã chọn và cấu hình có thể đưa vào giỏ.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ so sánh sản phẩm.

### Ảnh cần chèn

* Chèn ảnh /BuildPc với một cấu hình đã chọn CPU, mainboard, RAM, SSD, VGA, PSU và case.

### Gợi ý animation thủ công

* Các ô linh kiện xuất hiện theo hàng bằng Fade.
* Placeholder Build PC Push từ trái.
* Card tổng giá Zoom và Pulse nhẹ sau cùng.

* Thời lượng gợi ý: 40–50 giây.

## Slide 23 — So sánh sản phẩm

### Lời thuyết trình

Module Compare lưu tối đa hai sản phẩm trong session.
Màn hình đối chiếu giá, thông số, khuyến mãi và tồn kho theo cùng hàng.
Compare ViewModel chuẩn hóa các dòng thông số để dễ đọc giữa hai sản phẩm.
Ảnh chụp nên chọn hai sản phẩm cùng loại để thể hiện giá trị của phép so sánh.

### Ý chính cần nhấn mạnh

* Module Compare lưu tối đa hai sản phẩm trong session.
* Ảnh chụp nên chọn hai sản phẩm cùng loại để thể hiện giá trị của phép so sánh.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ quản trị hệ thống.

### Ảnh cần chèn

* Chèn ảnh /Compare với hai sản phẩm cùng nhóm để các hàng thông số có ý nghĩa.

### Gợi ý animation thủ công

* Hai tiêu đề sản phẩm Wipe từ hai phía.
* Các hàng tiêu chí xuất hiện từ trên xuống.
* Placeholder ảnh Fade ở cuối.

* Thời lượng gợi ý: 40–50 giây.

## Slide 24 — Quản trị hệ thống

### Lời thuyết trình

Tiếp theo là phần quản trị hệ thống.
Theo dõi vận hành và xử lý nghiệp vụ cửa hàng.
Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.
Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Ý chính cần nhấn mạnh

* Tiếp theo là phần quản trị hệ thống.
* Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ dashboard quản trị.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Số section Fade trong 0,5 giây.
* Tiêu đề Wipe từ trái trong 0,6 giây.
* Biểu tượng section Zoom nhẹ sau cùng.

* Thời lượng gợi ý: 15–20 giây.

## Slide 25 — Dashboard quản trị

### Lời thuyết trình

Phần quản trị bắt đầu bằng dashboard tổng quan vận hành cửa hàng.
ViewModel hiện thống kê sản phẩm, đơn hàng, người dùng và yêu cầu bảo hành.
Các số liệu được truy vấn trực tiếp từ cơ sở dữ liệu bằng EF Core.
Ảnh thật nên thể hiện đồng thời KPI và menu quản trị bên trái.

### Ý chính cần nhấn mạnh

* Phần quản trị bắt đầu bằng dashboard tổng quan vận hành cửa hàng.
* Ảnh thật nên thể hiện đồng thời KPI và menu quản trị bên trái.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ quản lý sản phẩm & đơn hàng.

### Ảnh cần chèn

* Có, URL: /AdminDashboard (route convention; cần xác minh route khi chạy web).

### Gợi ý animation thủ công

* Khung dashboard Push nhẹ từ trái.
* Bốn KPI Fade lần lượt, mỗi mục 0,2 giây.

* Thời lượng gợi ý: 40–50 giây.

## Slide 26 — Quản lý sản phẩm & đơn hàng

### Lời thuyết trình

Hai nghiệp vụ vận hành chính là quản lý sản phẩm và quản lý đơn hàng.
AdminProducts hỗ trợ tạo, sửa, xóa, lưu ảnh và thông số linh kiện.
AdminOrders cho phép lọc, xem chi tiết và cập nhật trạng thái đơn.
Đơn chuyển khoản có thao tác xác nhận riêng trước khi tiếp tục xử lý.

### Ý chính cần nhấn mạnh

* Hai nghiệp vụ vận hành chính là quản lý sản phẩm và quản lý đơn hàng.
* Đơn chuyển khoản có thao tác xác nhận riêng trước khi tiếp tục xử lý.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ hỗ trợ & dịch vụ sau bán.

### Ảnh cần chèn

* Có, URL: /AdminProducts và /AdminOrders (route convention; cần xác minh khi chạy web).

### Gợi ý animation thủ công

* Hai cột Wipe từ hai phía.
* Các ý nghiệp vụ Fade sau ảnh giao diện.

* Thời lượng gợi ý: 40–50 giây.

## Slide 27 — Hỗ trợ & dịch vụ sau bán

### Lời thuyết trình

Hệ thống có ba nhóm dịch vụ sau bán đã được xác minh từ source.
Chat dùng SignalR, đồng thời lưu hội thoại và tin nhắn vào database.
Khách đăng nhập có thể gửi yêu cầu bảo hành theo sản phẩm đã mua.
Từ chi tiết đơn, người dùng có thể mở báo giá và xuất Excel.

### Ý chính cần nhấn mạnh

* Hệ thống có ba nhóm dịch vụ sau bán đã được xác minh từ source.
* Từ chi tiết đơn, người dùng có thể mở báo giá và xuất Excel.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ kết quả đạt được.

### Ảnh cần chèn

* Có, URL: /AdminChat, /Warranty, /Orders/Quotation?orderId={id}.

### Gợi ý animation thủ công

* Ba card Fade lần lượt từ trái sang phải.
* Icon Zoom nhẹ; không dùng hiệu ứng lặp.

* Thời lượng gợi ý: 40–50 giây.

## Slide 28 — Kết quả đạt được

### Lời thuyết trình

Source hiện có 22 DbSet và 21 controller nghiệp vụ.
Bài trình bày nhóm công nghệ thành tám khối dễ theo dõi.
Bốn nhóm sử dụng gồm khách vãng lai, khách hàng, admin và staff.
Kết quả quan trọng nhất là kết nối được mua hàng với hậu mãi.

### Ý chính cần nhấn mạnh

* Source hiện có 22 DbSet và 21 controller nghiệp vụ.
* Kết quả quan trọng nhất là kết nối được mua hàng với hậu mãi.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ hạn chế & hướng phát triển.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Bốn thẻ số liệu Fade theo thứ tự.
* Thanh module Wipe nhẹ từ trái sang phải.

* Thời lượng gợi ý: 40–50 giây.

## Slide 29 — Hạn chế & hướng phát triển

### Lời thuyết trình

Các hạn chế được nhìn nhận đúng phạm vi một đồ án sinh viên.
QR hiện hỗ trợ chuyển khoản nhưng vẫn cần admin xác nhận thủ công.
Các dịch vụ ngoài phụ thuộc khóa cấu hình và môi trường triển khai.
Hướng tiếp theo là payment gateway, AI Build PC, báo cáo và mobile UX.

### Ý chính cần nhấn mạnh

* Các hạn chế được nhìn nhận đúng phạm vi một đồ án sinh viên.
* Hướng tiếp theo là payment gateway, AI Build PC, báo cáo và mobile UX.

### Gợi ý chuyển slide

* Chuyển sang phần tiếp theo để làm rõ xin chân thành cảm ơn.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Hai nửa slide Wipe từ hai phía.
* Các cặp hiện tại và tương lai Fade theo hàng.

* Thời lượng gợi ý: 40–50 giây.

## Slide 30 — Xin chân thành cảm ơn

### Lời thuyết trình

Em xin chân thành cảm ơn thầy cô và hội đồng đã lắng nghe.
Đề tài đã hoàn thành các luồng chính của website PC Store.
Em mong nhận được góp ý để tiếp tục cải thiện sản phẩm.
Sau đây em xin sẵn sàng trả lời các câu hỏi của hội đồng.

### Ý chính cần nhấn mạnh

* Em xin chân thành cảm ơn thầy cô và hội đồng đã lắng nghe.
* Sau đây em xin sẵn sàng trả lời các câu hỏi của hội đồng.

### Gợi ý chuyển slide

* Mời hội đồng đặt câu hỏi và trao đổi.

### Ảnh cần chèn

* Không cần chèn ảnh.

### Gợi ý animation thủ công

* Toàn slide Fade trong 0,5 giây.
* Giữ slide tĩnh trong phần hỏi đáp.

* Thời lượng gợi ý: 20–30 giây trước phần hỏi đáp.
