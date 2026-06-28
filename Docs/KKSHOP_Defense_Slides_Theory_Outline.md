# Outline 25 slide bảo vệ đồ án KKSHOP – định hướng lý thuyết, phân tích và thiết kế

> Đề tài: **Xây dựng website bán máy tính và linh kiện KKSHOP**  
> Định hướng trình bày: chuyên nghiệp, không phóng đại; ưu tiên các cụm từ “trong phạm vi đồ án”, “hệ thống hiện tại đáp ứng”, “có thể mở rộng trong tương lai”.  
> Căn cứ source code: ASP.NET Core MVC .NET 8, Entity Framework Core SQL Server, cookie authentication, SignalR chat, dịch vụ giỏ hàng/đơn hàng/voucher/bảo hành/đánh giá/build PC/AI chat, cấu hình GHN - OpenRouteService - Cloudinary - Gemini.

---

## SLIDE 1 – Trang bìa

### A. Nội dung đưa lên slide
- Trường/khoa, tên đề tài
- “Xây dựng website bán máy tính và linh kiện KKSHOP”
- Sinh viên, giảng viên hướng dẫn
- Logo/ảnh nền đỏ - trắng - xám

### B. Lời thuyết trình
Em xin trình bày đồ án tốt nghiệp với đề tài xây dựng website bán máy tính và linh kiện KKSHOP. Đồ án tập trung vào bài toán thương mại điện tử cho nhóm sản phẩm máy tính, linh kiện, kèm các chức năng hỗ trợ như giỏ hàng, đặt hàng, quản trị, bảo hành, đánh giá, chat hỗ trợ và build PC.

### C. Câu hỏi dễ bị hỏi
- Vì sao chọn đề tài này?
- Điểm khác so với website bán hàng thông thường là gì?

### D. Câu trả lời an toàn
- Em chọn đề tài vì nhóm sản phẩm máy tính có nhiều nghiệp vụ đặc thù như cấu hình, linh kiện, bảo hành và tư vấn trước khi mua.
- Điểm khác là ngoài bán hàng cơ bản, hệ thống có thêm module linh kiện, build PC, so sánh, bảo hành, đánh giá và chat hỗ trợ.

### E. Ghi chú tránh bị hỏi khó
- Không nói đây là sản phẩm thương mại hoàn chỉnh.
- Nên nói: “Trong phạm vi đồ án, em mô phỏng tương đối đầy đủ quy trình bán hàng máy tính.”

---

## SLIDE 2 – Lý do chọn đề tài

### A. Nội dung đưa lên slide
- Nhu cầu mua máy tính/linh kiện trực tuyến tăng
- Sản phẩm kỹ thuật cần thông tin cấu hình rõ ràng
- Người dùng cần tư vấn, so sánh, bảo hành
- Doanh nghiệp cần quản trị sản phẩm, đơn hàng, nội dung

### B. Lời thuyết trình
Website thương mại điện tử không chỉ hiển thị sản phẩm mà còn cần hỗ trợ người dùng ra quyết định. Với máy tính và linh kiện, người mua thường quan tâm cấu hình, giá, bảo hành, khả năng lựa chọn linh kiện. Vì vậy em xây dựng KKSHOP để mô phỏng một hệ thống bán hàng chuyên cho máy tính, có cả phần khách hàng và quản trị.

### C. Câu hỏi dễ bị hỏi
- Website này giải quyết vấn đề gì?
- Có gì phù hợp với đồ án tốt nghiệp?
- Có khảo sát thực tế không?

### D. Câu trả lời an toàn
- Hệ thống giải quyết quy trình từ xem sản phẩm, chọn linh kiện, giỏ hàng, đặt hàng đến quản trị và chăm sóc sau bán.
- Đề tài phù hợp vì có đủ phân tích nghiệp vụ, cơ sở dữ liệu, phân quyền, xử lý đơn hàng và kiểm thử.
- Trong phạm vi đồ án, em tham khảo luồng nghiệp vụ phổ biến của các website bán máy tính và triển khai theo mô hình học thuật.

### E. Ghi chú tránh bị hỏi khó
- Không khẳng định đã khảo sát thị trường quy mô lớn nếu không có tài liệu.
- Nhấn mạnh tính phù hợp về mặt học thuật và kỹ thuật.

---

## SLIDE 3 – Mục tiêu đề tài

### A. Nội dung đưa lên slide
- Xây dựng website bán máy tính và linh kiện
- Quản lý sản phẩm, danh mục, banner, bài viết
- Hỗ trợ giỏ hàng, đặt hàng, thanh toán COD/chuyển khoản
- Quản trị đơn hàng, người dùng, đánh giá, bảo hành
- Tích hợp hỗ trợ: chat, AI tư vấn, build PC

### B. Lời thuyết trình
Mục tiêu chính là xây dựng một hệ thống có thể phục vụ khách hàng mua sản phẩm và quản trị viên quản lý hoạt động bán hàng. Về mặt kỹ thuật, đồ án áp dụng ASP.NET Core MVC, EF Core, SQL Server, phân quyền bằng cookie authentication và tổ chức theo các service để tách nghiệp vụ.

### C. Câu hỏi dễ bị hỏi
- Mục tiêu nào là quan trọng nhất?
- Hệ thống đã hoàn thiện đến mức nào?
- Có thanh toán online thật không?

### D. Câu trả lời an toàn
- Mục tiêu quan trọng nhất là hoàn chỉnh luồng thương mại điện tử từ sản phẩm đến đơn hàng.
- Hệ thống đáp ứng mức đồ án tốt nghiệp, chưa phải hệ thống thương mại vận hành thực tế quy mô lớn.
- Hiện hệ thống hỗ trợ COD và chuyển khoản ngân hàng theo luồng xác nhận, chưa tích hợp cổng thanh toán thật như VNPay/Momo.

### E. Ghi chú tránh bị hỏi khó
- Không gọi chuyển khoản là “payment gateway” nếu chưa tích hợp cổng thật.
- Nên nói rõ: “Chuyển khoản được xử lý theo trạng thái chờ xác nhận.”

---

## SLIDE 4 – Phạm vi và đối tượng sử dụng

### A. Nội dung đưa lên slide
- Khách vãng lai: xem sản phẩm, tìm kiếm, chat, giỏ hàng phiên
- Khách hàng: tài khoản, đơn hàng, đánh giá, bảo hành
- Nhân viên: sản phẩm, đơn hàng, hóa đơn, báo cáo
- Admin: người dùng, phân quyền, cấu hình, banner, voucher
- Nhân viên hỗ trợ: chat, bảo hành, đánh giá

### B. Lời thuyết trình
Hệ thống được thiết kế cho nhiều nhóm người dùng. Khách hàng có thể xem sản phẩm, mua hàng và theo dõi đơn. Nhân viên và admin có giao diện quản trị để xử lý dữ liệu. Phân quyền trong code được gắn bằng attribute Authorize theo các vai trò như Admin, Staff, SupportStaff và CustomerSupport.

### C. Câu hỏi dễ bị hỏi
- Có bao nhiêu vai trò?
- Khách không đăng nhập có mua được không?
- Admin và Staff khác nhau thế nào?

### D. Câu trả lời an toàn
- Trong code có các vai trò chính như Admin, Staff, SupportStaff, CustomerSupport và người dùng thường.
- Hệ thống có xử lý giỏ hàng khách và đơn hàng có thể có UserId nullable, vì vậy có hỗ trợ tình huống khách chưa đăng nhập ở một số luồng.
- Admin có quyền cao hơn, ví dụ quản lý người dùng/cấu hình; Staff tập trung nghiệp vụ sản phẩm, đơn hàng.

### E. Ghi chú tránh bị hỏi khó
- Không nói phân quyền dạng permission matrix quá phức tạp nếu không có.
- Nên nói: “Phân quyền hiện tại dựa trên role ở controller.”

---

## SLIDE 5 – Cơ sở lý thuyết về website thương mại điện tử

### A. Nội dung đưa lên slide
- Catalog sản phẩm
- Tìm kiếm/lọc/sắp xếp
- Giỏ hàng và đặt hàng
- Thanh toán và trạng thái đơn
- Quản trị nội dung và chăm sóc sau bán

### B. Lời thuyết trình
Một website thương mại điện tử thường gồm ba lớp nghiệp vụ: trước bán, trong bán và sau bán. Trước bán là hiển thị, tìm kiếm, tư vấn sản phẩm. Trong bán là giỏ hàng, đơn hàng, thanh toán. Sau bán là theo dõi đơn, bảo hành, đánh giá và hỗ trợ khách hàng. KKSHOP được xây dựng bám theo các nhóm nghiệp vụ này.

### C. Câu hỏi dễ bị hỏi
- E-commerce khác website giới thiệu sản phẩm thế nào?
- Vì sao cần trạng thái đơn hàng?
- Vì sao cần đánh giá và bảo hành?

### D. Câu trả lời an toàn
- Website giới thiệu chủ yếu cung cấp thông tin, còn e-commerce phải xử lý giao dịch và trạng thái nghiệp vụ.
- Trạng thái đơn hàng giúp quản lý vòng đời đơn từ chờ xác nhận, chờ thanh toán đến hoàn tất/hủy/hết hạn.
- Đánh giá và bảo hành là phần chăm sóc sau bán, tăng độ tin cậy cho sản phẩm.

### E. Ghi chú tránh bị hỏi khó
- Không trình bày quá sâu về mô hình kinh doanh.
- Gắn lý thuyết với module thật trong source code.

---

## SLIDE 6 – Quy trình mua hàng trực tuyến

### A. Nội dung đưa lên slide
1. Tìm kiếm/xem sản phẩm
2. Thêm vào giỏ hoặc mua ngay
3. Nhập thông tin nhận hàng
4. Tính phí vận chuyển/voucher
5. Chọn COD hoặc chuyển khoản
6. Theo dõi/xác nhận đơn

### B. Lời thuyết trình
Quy trình mua hàng trong KKSHOP bắt đầu từ việc khách xem danh sách và chi tiết sản phẩm. Sau đó khách thêm sản phẩm vào giỏ hoặc mua ngay. Ở bước checkout, hệ thống nhận thông tin giao hàng, tính phí vận chuyển, áp dụng voucher nếu có và tạo đơn hàng. Với chuyển khoản, đơn ở trạng thái chờ thanh toán hoặc chờ xác nhận.

### C. Câu hỏi dễ bị hỏi
- Nếu hết hàng thì sao?
- Nếu chuyển khoản nhưng không xác nhận thì sao?
- Có áp dụng voucher nhiều lần không?

### D. Câu trả lời an toàn
- Khi thêm giỏ hoặc tạo đơn, hệ thống có kiểm tra sản phẩm, số lượng và tồn kho ở mức nghiệp vụ đồ án.
- Đơn chuyển khoản có thời hạn thanh toán và service xử lý hết hạn.
- Voucher có bảng VoucherUsage và điều kiện số lượng, thời gian, giới hạn theo người dùng.

### E. Ghi chú tránh bị hỏi khó
- Không nói kiểm soát đồng thời tồn kho đã đạt mức enterprise.
- Nên nói: “Đủ cho mô phỏng nghiệp vụ, có thể mở rộng bằng transaction/locking chặt hơn.”

---

## SLIDE 7 – Công nghệ sử dụng

### A. Nội dung đưa lên slide
- ASP.NET Core MVC .NET 8
- Entity Framework Core + SQL Server
- Razor View, Bootstrap/CSS/JavaScript
- Cookie Authentication + Role-based Authorization
- SignalR cho chat hỗ trợ
- Tích hợp tùy chọn: GHN, OpenRouteService, Cloudinary, Gemini

### B. Lời thuyết trình
Em chọn ASP.NET Core MVC vì phù hợp mô hình học thuật, dễ tách controller, model và view. EF Core hỗ trợ thao tác cơ sở dữ liệu theo hướng ORM. SQL Server dùng để lưu dữ liệu nghiệp vụ. Ngoài ra, hệ thống có SignalR cho chat thời gian thực và một số cấu hình tích hợp ngoài như GHN, OpenRouteService, Cloudinary và Gemini nhưng đều ở mức phục vụ phạm vi đồ án.

### C. Câu hỏi dễ bị hỏi
- Vì sao không dùng API + React?
- Vì sao dùng EF Core?
- Tích hợp AI có bắt buộc không?

### D. Câu trả lời an toàn
- MVC phù hợp đồ án vì triển khai nhanh, dễ kiểm soát luồng dữ liệu server-side.
- EF Core giúp giảm thao tác SQL thủ công, quản lý entity và migration thuận tiện.
- AI chat là chức năng hỗ trợ tư vấn, không phải lõi bắt buộc của quy trình bán hàng.

### E. Ghi chú tránh bị hỏi khó
- Không so sánh “MVC tốt hơn React” tuyệt đối.
- Nên nói lựa chọn công nghệ phù hợp quy mô, thời gian và mục tiêu đồ án.

---

## SLIDE 8 – Kiến trúc hệ thống ASP.NET Core MVC

### A. Nội dung đưa lên slide
- Request → Routing → Controller
- Controller gọi Service/DbContext
- EF Core thao tác SQL Server
- ViewModel trả dữ liệu ra Razor View
- Middleware: StaticFiles, Session, Authentication, Authorization

### B. Lời thuyết trình
Kiến trúc của hệ thống đi theo mô hình MVC. Khi người dùng gửi request, routing trong Program.cs điều hướng đến controller. Controller xử lý input, gọi service hoặc DbContext, sau đó trả ViewModel ra view. Middleware được cấu hình theo thứ tự: static files, routing, session, authentication và authorization.

### C. Câu hỏi dễ bị hỏi
- Vì sao cần service layer?
- Middleware nào quan trọng nhất?
- DbContext nằm ở đâu trong kiến trúc?

### D. Câu trả lời an toàn
- Service layer giúp tách nghiệp vụ như giỏ hàng, voucher, vận chuyển, AI chat khỏi controller.
- Authentication/Authorization quan trọng cho bảo vệ khu vực quản trị; Session quan trọng cho giỏ hàng khách.
- DbContext là lớp truy cập dữ liệu, ánh xạ entity với bảng SQL Server.

### E. Ghi chú tránh bị hỏi khó
- Không nói dự án là Clean Architecture đầy đủ.
- Nên gọi là MVC có tách service nghiệp vụ.

---

## SLIDE 9 – Mô hình MVC trong dự án

### A. Nội dung đưa lên slide
- Model/Entity: Product, Order, User, Voucher, WarrantyRequest…
- ViewModel: CartVm, CheckoutVm, BuildPcVm, AdminDashboardVm…
- Controller: Products, Cart, Orders, Account, Admin*
- View: Razor pages cho khách và quản trị

### B. Lời thuyết trình
Trong dự án, entity đại diện cho dữ liệu lưu trữ, ViewModel đại diện cho dữ liệu đưa lên giao diện, controller điều phối nghiệp vụ. Ví dụ ProductsController xử lý danh sách và chi tiết sản phẩm, CartController xử lý giỏ hàng, OrdersController xử lý checkout, còn các controller Admin xử lý quản trị.

### C. Câu hỏi dễ bị hỏi
- Entity và ViewModel khác nhau thế nào?
- Vì sao không đưa thẳng entity ra view?
- Controller có bị quá tải không?

### D. Câu trả lời an toàn
- Entity gắn với database, ViewModel gắn với nhu cầu hiển thị/nhập liệu.
- Tách ViewModel giúp giảm lộ dữ liệu không cần thiết và dễ validate giao diện.
- Một số controller còn nhiều logic, đây là điểm có thể refactor thêm sang service nếu phát triển tiếp.

### E. Ghi chú tránh bị hỏi khó
- Nếu bị hỏi controller dài, thừa nhận có thể cải tiến.
- Nhấn mạnh đã có nhiều service tách nghiệp vụ chính.

---

## SLIDE 10 – Phân tích tác nhân và chức năng chính

### A. Nội dung đưa lên slide
- Khách hàng: xem, tìm kiếm, so sánh, giỏ hàng, đặt hàng
- Thành viên: quản lý tài khoản, đơn hàng, đánh giá, bảo hành
- Nhân viên: quản lý sản phẩm, đơn hàng, hóa đơn, báo cáo
- Hỗ trợ: chat, bảo hành, đánh giá
- Admin: tài khoản, phân quyền, cấu hình, voucher, banner

### B. Lời thuyết trình
Các tác nhân được xác định dựa trên nghiệp vụ thương mại điện tử. Khách hàng dùng chức năng mua hàng. Nhân viên xử lý dữ liệu vận hành. Admin quản lý các phần ảnh hưởng toàn hệ thống. Nhân viên hỗ trợ tập trung vào chăm sóc khách hàng sau bán và chat.

### C. Câu hỏi dễ bị hỏi
- Tác nhân nào quan trọng nhất?
- Có actor “AI” không?
- Có phân biệt khách và thành viên không?

### D. Câu trả lời an toàn
- Tác nhân trung tâm là khách hàng, vì mọi nghiệp vụ hướng tới mua hàng.
- AI không phải tác nhân nghiệp vụ chính, chỉ là dịch vụ hỗ trợ tương tác.
- Khách chưa đăng nhập có thể xem/mua ở một số luồng; thành viên có thêm lịch sử đơn, đánh giá và bảo hành thuận tiện hơn.

### E. Ghi chú tránh bị hỏi khó
- Không biến quá nhiều module nhỏ thành actor độc lập.
- Nên dùng actor gọn để sơ đồ use case dễ bảo vệ.

---

## SLIDE 11 – Use case tổng quát

### A. Nội dung đưa lên slide
- Sơ đồ use case tổng quát
- Nhóm use case: mua hàng, quản trị, hỗ trợ sau bán
- Quan hệ include: đặt hàng gồm tính phí, voucher, chọn thanh toán
- Quan hệ extend: đánh giá sau khi mua, bảo hành sau khi có đơn

### B. Lời thuyết trình
Sơ đồ use case thể hiện các chức năng chính theo tác nhân. Với khách hàng, use case trọng tâm là tìm kiếm sản phẩm, thêm giỏ, đặt hàng. Với admin và nhân viên là quản lý dữ liệu. Các chức năng như đánh giá, bảo hành phụ thuộc vào thông tin đơn hàng nên có thể đặt sau luồng mua hàng.

### C. Câu hỏi dễ bị hỏi
- Include và extend khác gì?
- Đánh giá có bắt buộc sau mua không?
- Bảo hành có cần đơn hàng không?

### D. Câu trả lời an toàn
- Include là bước con gần như bắt buộc trong use case chính; extend là chức năng mở rộng theo điều kiện.
- Đánh giá không bắt buộc, nhưng trong hệ thống được kiểm soát theo đơn hàng/sản phẩm để tránh đánh giá không liên quan.
- Bảo hành ưu tiên gắn đơn hàng/chi tiết đơn, nhưng vẫn có trường thông tin khách để xử lý trong phạm vi đồ án.

### E. Ghi chú tránh bị hỏi khó
- Không vẽ use case quá chi tiết gây khó giải thích.
- Tập trung 3 cụm: mua hàng, quản trị, hỗ trợ.

---

## SLIDE 12 – Thiết kế cơ sở dữ liệu tổng quan

### A. Nội dung đưa lên slide
- SQL Server + EF Core DbContext
- Nhóm bảng người dùng/phân quyền
- Nhóm bảng sản phẩm/danh mục/ảnh/linh kiện
- Nhóm bảng giỏ hàng/đơn hàng/voucher
- Nhóm bảng hậu mãi: bảo hành, đánh giá, chat, bài viết

### B. Lời thuyết trình
Cơ sở dữ liệu được thiết kế quanh các nhóm nghiệp vụ. ApplicationDbContext khai báo các DbSet như Users, Roles, Products, Categories, Orders, OrderDetails, Vouchers, WarrantyRequests, ProductReviews, ChatConversations và ChatMessages. Các quan hệ được cấu hình trong OnModelCreating bằng Fluent API.

### C. Câu hỏi dễ bị hỏi
- Vì sao dùng Fluent API?
- Có khóa ngoại không?
- Có audit CreatedAt/UpdatedAt không?

### D. Câu trả lời an toàn
- Fluent API giúp cấu hình quan hệ, độ dài, index và precision rõ hơn attribute.
- Có khóa ngoại giữa product-category, order-detail, review-order, warranty-order, chat-message.
- BaseEntity có CreatedAt/UpdatedAt và DbContext tự cập nhật khi SaveChanges.

### E. Ghi chú tránh bị hỏi khó
- Không nói ERD đã tối ưu chuẩn cao nhất.
- Nên nói: “Thiết kế đủ phục vụ nghiệp vụ và có thể chuẩn hóa thêm khi mở rộng.”

---

## SLIDE 13 – Các bảng dữ liệu chính

### A. Nội dung đưa lên slide
- Users, Roles
- Categories, Products, ProductImages, ComponentBrands
- Carts, CartItems
- Orders, OrderDetails
- Vouchers, VoucherUsages
- ProductReviews, WarrantyRequests
- Articles, Banners, ChatConversations, ChatMessages

### B. Lời thuyết trình
Các bảng chính phản ánh các thực thể của hệ thống. Product lưu thông tin sản phẩm và linh kiện, Order lưu thông tin đơn hàng, OrderDetail lưu từng sản phẩm trong đơn. Voucher và VoucherUsage phục vụ khuyến mãi. ProductReview và WarrantyRequest phục vụ chăm sóc sau bán. ChatConversation và ChatMessage phục vụ hỗ trợ trực tuyến.

### C. Câu hỏi dễ bị hỏi
- Vì sao tách Order và OrderDetail?
- Vì sao cần ProductImages riêng?
- Vì sao có VoucherUsage?

### D. Câu trả lời an toàn
- Một đơn hàng có nhiều sản phẩm, nên cần OrderDetail để biểu diễn quan hệ 1-n.
- Một sản phẩm có thể có nhiều ảnh, nên tách ProductImages giúp quản lý ảnh chính và thứ tự.
- VoucherUsage giúp theo dõi lượt dùng, kiểm soát giới hạn sử dụng.

### E. Ghi chú tránh bị hỏi khó
- Không nói tất cả bảng đều đã đạt chuẩn 3NF tuyệt đối.
- Nên nêu quan hệ chính và lý do tách bảng.

---

## SLIDE 14 – Luồng quản lý sản phẩm và linh kiện

### A. Nội dung đưa lên slide
- Admin/Staff thêm, sửa, xóa, bật/tắt sản phẩm
- Phân biệt PC và Component
- ComponentType: CPU, RAM, VGA, MAINBOARD, PSU…
- Thông số kỹ thuật, ảnh, giá, tồn kho, bảo hành
- Tìm kiếm, lọc, so sánh sản phẩm

### B. Lời thuyết trình
Sản phẩm trong KKSHOP được thiết kế để hỗ trợ cả máy tính nguyên bộ và linh kiện. Entity Product có ProductType, ComponentType, thông số kỹ thuật, giá, giá khuyến mãi, số lượng tồn và thời gian bảo hành. Khu quản trị có controller riêng cho sản phẩm và linh kiện, giúp nhân viên nhập dữ liệu phù hợp từng nhóm.

### C. Câu hỏi dễ bị hỏi
- Linh kiện khác sản phẩm thường ở đâu?
- Có kiểm tra dữ liệu kỹ thuật không?
- Có quản lý nhiều ảnh không?

### D. Câu trả lời an toàn
- Linh kiện được phân loại thêm bằng ComponentType và các trường hỗ trợ như CpuSocket, RamType.
- Hệ thống có lưu thông số và chuẩn hóa loại linh kiện ở mức phục vụ lọc/tư vấn, chưa kiểm tra kỹ thuật chuyên sâu mọi trường hợp.
- Có bảng ProductImages để quản lý nhiều ảnh và ảnh chính.

### E. Ghi chú tránh bị hỏi khó
- Không nói hệ thống hiểu đầy đủ mọi chuẩn phần cứng.
- Nên nói: “Dữ liệu linh kiện được thiết kế mở để bổ sung luật tương thích sau này.”

---

## SLIDE 15 – Luồng giỏ hàng và đặt hàng

### A. Nội dung đưa lên slide
- AddToCart qua CartService
- Hỗ trợ user đăng nhập và session guest
- Cập nhật/xóa sản phẩm trong giỏ
- Checkout từ giỏ hoặc mua ngay
- Tạo Order và OrderDetail

### B. Lời thuyết trình
Giỏ hàng được xử lý qua CartService để tách logic khỏi controller. Với người dùng đăng nhập, giỏ hàng lưu theo UserId trong database. Với khách chưa đăng nhập, hệ thống có thể dùng session. Khi checkout, dữ liệu giỏ được chuyển thành Order và OrderDetail, đồng thời lưu snapshot tên, ảnh, giá, bảo hành để đơn hàng không bị phụ thuộc hoàn toàn vào thay đổi sản phẩm sau này.

### C. Câu hỏi dễ bị hỏi
- Vì sao cần snapshot thông tin sản phẩm trong OrderDetail?
- Nếu giá sản phẩm đổi sau khi đặt thì sao?
- Mua ngay khác giỏ hàng thế nào?

### D. Câu trả lời an toàn
- Snapshot giúp đơn hàng giữ lại thông tin tại thời điểm mua.
- Nếu giá đổi sau khi đặt, đơn vẫn hiển thị giá đã ghi trong OrderDetail.
- Mua ngay dùng checkout mode riêng, phù hợp khi khách muốn đặt một nhóm sản phẩm nhanh mà không dùng toàn bộ giỏ.

### E. Ghi chú tránh bị hỏi khó
- Không nói đã xử lý mọi race condition tồn kho.
- Nhấn mạnh thiết kế snapshot đơn hàng là điểm nên nói kỹ.

---

## SLIDE 16 – Luồng thanh toán và xử lý đơn hàng

### A. Nội dung đưa lên slide
- Phương thức: COD, chuyển khoản ngân hàng
- Trạng thái thanh toán: unpaid, pending, pending confirmation, paid
- Trạng thái đơn: pending, processing, delivering, completed, cancelled, expired
- Admin xác nhận chuyển khoản
- Service xử lý đơn hết hạn

### B. Lời thuyết trình
Trong phạm vi đồ án, thanh toán được thiết kế theo hai hình thức: COD và chuyển khoản. Với COD, đơn chờ xác nhận. Với chuyển khoản, đơn có nội dung chuyển khoản, thời hạn thanh toán và trạng thái chờ xác nhận. Admin hoặc nhân viên có quyền xác nhận thanh toán, sau đó đơn chuyển sang xử lý tiếp.

### C. Câu hỏi dễ bị hỏi
- Có tích hợp ngân hàng thật không?
- Làm sao biết khách đã chuyển khoản?
- Vì sao cần hết hạn thanh toán?

### D. Câu trả lời an toàn
- Chưa tích hợp API ngân hàng thật; trong phạm vi đồ án, chuyển khoản được xử lý bằng quy trình admin xác nhận.
- Admin kiểm tra giao dịch thực tế bên ngoài và xác nhận trong hệ thống.
- Hết hạn giúp tránh giữ đơn chờ thanh toán quá lâu.

### E. Ghi chú tránh bị hỏi khó
- Không nói “thanh toán online tự động” nếu không có gateway.
- Nên nói đây là mô phỏng nghiệp vụ chuyển khoản thủ công có trạng thái.

---

## SLIDE 17 – Luồng tài khoản, đăng nhập, phân quyền

### A. Nội dung đưa lên slide
- Đăng ký, đăng nhập, đăng xuất
- Cookie Authentication
- Role-based Authorization
- Quên mật khẩu bằng OTP email
- Trang cá nhân, đổi thông tin, đổi mật khẩu

### B. Lời thuyết trình
Hệ thống sử dụng cookie authentication với scheme riêng PcStoreCookie. Người dùng có RoleId liên kết với bảng Roles. Các controller quản trị được bảo vệ bằng Authorize Roles. Ngoài đăng nhập đăng ký, hệ thống có chức năng quên mật khẩu bằng OTP email và quản lý thông tin tài khoản.

### C. Câu hỏi dễ bị hỏi
- Mật khẩu lưu thế nào?
- Phân quyền nằm ở đâu?
- OTP có hết hạn không?

### D. Câu trả lời an toàn
- Mật khẩu được lưu dạng hash thông qua AuthService, không lưu plain text.
- Phân quyền được áp dụng ở controller bằng Authorize(Roles=...).
- OTP có thời gian hết hạn và trạng thái IsUsed trong bảng PasswordResetOtps.

### E. Ghi chú tránh bị hỏi khó
- Không nói bảo mật tuyệt đối.
- Nên nói: “Hệ thống áp dụng các cơ chế cơ bản phù hợp đồ án; khi triển khai thật cần hardening thêm.”

---

## SLIDE 18 – Quản trị hệ thống

### A. Nội dung đưa lên slide
- Dashboard doanh thu/đơn hàng
- Quản lý sản phẩm, linh kiện, danh mục
- Quản lý đơn hàng, hóa đơn, báo cáo
- Quản lý người dùng, banner, bài viết, voucher
- Quản lý chat, bảo hành, đánh giá

### B. Lời thuyết trình
Khu quản trị giúp admin và nhân viên vận hành hệ thống. Các controller AdminDashboard, AdminProducts, AdminOrders, AdminInvoices, AdminReports, AdminUsers, AdminBanners, AdminArticles, AdminVouchers, AdminWarranty và AdminReviews thể hiện các nhóm chức năng quản trị chính.

### C. Câu hỏi dễ bị hỏi
- Dashboard thống kê gì?
- Nhân viên có được quản lý người dùng không?
- Có log thao tác không?

### D. Câu trả lời an toàn
- Dashboard tập trung số liệu tổng quan như đơn hàng, doanh thu, trạng thái cần xử lý.
- Quản lý người dùng là quyền Admin.
- Hệ thống hiện chưa xây dựng audit log thao tác admin chi tiết; đây là hướng phát triển.

### E. Ghi chú tránh bị hỏi khó
- Không nói có audit/history nếu chưa có.
- Nên nói rõ ranh giới quyền Admin/Staff.

---

## SLIDE 19 – Chức năng hỗ trợ: bảo hành, đánh giá, bài viết, banner

### A. Nội dung đưa lên slide
- Bảo hành: tạo yêu cầu, tra cứu, admin xử lý
- Đánh giá: gắn sản phẩm, đơn hàng, trạng thái duyệt
- Bài viết: tin công nghệ, tư vấn build PC, hướng dẫn
- Banner: quản lý ảnh, vị trí, trạng thái hiển thị

### B. Lời thuyết trình
Ngoài luồng mua hàng, KKSHOP có các chức năng hỗ trợ tăng tính hoàn chỉnh. Bảo hành cho phép khách gửi yêu cầu và admin cập nhật trạng thái. Đánh giá sản phẩm có thể được quản lý, phản hồi. Bài viết và banner giúp quản trị nội dung marketing và thông tin tư vấn.

### C. Câu hỏi dễ bị hỏi
- Đánh giá có chống spam không?
- Bảo hành dựa vào gì?
- Bài viết có phân loại không?

### D. Câu trả lời an toàn
- Đánh giá được gắn với Product, User, Order và có ràng buộc unique theo ProductId/UserId/OrderId để hạn chế đánh giá trùng.
- Bảo hành dựa vào thông tin đơn, sản phẩm, ngày mua và thời hạn bảo hành trong phạm vi dữ liệu hệ thống.
- Bài viết có Type như tin công nghệ, tư vấn build PC, phần mềm, khuyến mãi, hướng dẫn.

### E. Ghi chú tránh bị hỏi khó
- Không nói đã chống spam hoàn toàn.
- Nên nhấn mạnh đây là chức năng hậu mãi và nội dung, không phải lõi thanh toán.

---

## SLIDE 20 – Chat hỗ trợ / AI tư vấn

### A. Nội dung đưa lên slide
- Chat hỗ trợ lưu Conversation/Message
- SignalR cho realtime
- Admin/SupportStaff trả lời khách
- AI chat dùng cấu hình Gemini
- AI lấy ngữ cảnh sản phẩm từ database

### B. Lời thuyết trình
Hệ thống có hai hướng hỗ trợ: chat trực tiếp và AI tư vấn. Chat trực tiếp lưu cuộc hội thoại và tin nhắn, nhân viên hỗ trợ có thể trả lời từ trang admin. AI chat dùng service riêng, cấu hình provider Gemini và lấy dữ liệu sản phẩm làm ngữ cảnh để đưa gợi ý. Tuy nhiên đây là chức năng hỗ trợ, không thay thế tư vấn kỹ thuật chuyên sâu.

### C. Câu hỏi dễ bị hỏi
- AI có luôn trả lời đúng không?
- AI dựa vào dữ liệu nào?
- Nếu không có API key thì sao?

### D. Câu trả lời an toàn
- Không khẳng định AI luôn đúng; AI chỉ hỗ trợ gợi ý trong phạm vi dữ liệu sản phẩm và chính sách hệ thống.
- AI dùng ProductSearchForAiService để lấy sản phẩm liên quan từ database, sau đó mới tạo phản hồi.
- Nếu không có API key hoặc lỗi ngoài, hệ thống cần fallback/thông báo phù hợp; AI không phải luồng bắt buộc để mua hàng.

### E. Ghi chú tránh bị hỏi khó
- Không dùng cụm “AI thông minh hoàn toàn”.
- Nên nói: “AI là kênh hỗ trợ tư vấn sơ bộ.”

---

## SLIDE 21 – Kiểm thử chức năng

### A. Nội dung đưa lên slide
- Kiểm thử luồng khách hàng: xem, tìm kiếm, giỏ hàng, đặt hàng
- Kiểm thử luồng admin: CRUD sản phẩm, đơn hàng, voucher
- Kiểm thử phân quyền: admin/staff/customer
- Kiểm thử bảo hành, đánh giá, chat
- Kiểm thử build PC và cảnh báo tương thích

### B. Lời thuyết trình
Kiểm thử trong đồ án chủ yếu theo dạng kiểm thử chức năng và kiểm thử thủ công theo kịch bản. Em chia thành các nhóm: khách hàng, quản trị, phân quyền và chức năng hỗ trợ. Mỗi nhóm kiểm thử theo đầu vào, thao tác, kết quả mong đợi và kết quả thực tế.

### C. Câu hỏi dễ bị hỏi
- Có unit test tự động không?
- Kiểm thử quan trọng nhất là gì?
- Làm sao kiểm thử phân quyền?

### D. Câu trả lời an toàn
- Hiện đồ án tập trung kiểm thử chức năng thủ công; unit/integration test tự động là hướng phát triển tiếp.
- Quan trọng nhất là luồng đặt hàng vì liên quan nhiều bảng dữ liệu.
- Kiểm thử phân quyền bằng cách đăng nhập các vai trò khác nhau và truy cập route quản trị.

### E. Ghi chú tránh bị hỏi khó
- Không bịa số lượng test case nếu không có bảng test.
- Nên chuẩn bị bảng test case ngắn cho slide phụ/backup.

---

## SLIDE 22 – Kiểm thử giao diện và trải nghiệm người dùng

### A. Nội dung đưa lên slide
- Responsive cơ bản trên desktop/mobile
- Giao diện tone đỏ - trắng - xám
- Form validate dữ liệu bắt buộc
- Thông báo lỗi/thành công
- Tối ưu thao tác mua hàng đơn giản

### B. Lời thuyết trình
Bên cạnh chức năng, giao diện được kiểm thử theo trải nghiệm người dùng. Các màn hình chính như trang chủ, danh sách sản phẩm, chi tiết, giỏ hàng, checkout và admin cần dễ thao tác. Hệ thống dùng Razor, CSS và JavaScript để hỗ trợ tương tác như giỏ hàng, checkout, build PC và chat.

### C. Câu hỏi dễ bị hỏi
- Có chuẩn UX nào không?
- Mobile đã hoàn thiện chưa?
- Có kiểm thử accessibility không?

### D. Câu trả lời an toàn
- Em áp dụng nguyên tắc UX cơ bản: dễ nhìn, ít bước, phản hồi rõ ràng.
- Mobile được hỗ trợ ở mức responsive cơ bản, có thể tối ưu thêm nếu triển khai thực tế.
- Accessibility chuyên sâu chưa phải trọng tâm, có thể bổ sung như alt text, keyboard navigation, contrast check.

### E. Ghi chú tránh bị hỏi khó
- Không nói đã đạt chuẩn WCAG nếu chưa kiểm chứng.
- Nên nói giao diện đủ dùng cho demo và có hướng cải thiện.

---

## SLIDE 23 – Kết quả đạt được

### A. Nội dung đưa lên slide
- Hoàn thành website bán máy tính/linh kiện theo MVC
- Có phân hệ khách hàng và quản trị
- Có cơ sở dữ liệu tương đối đầy đủ
- Có đơn hàng, voucher, vận chuyển, bảo hành, đánh giá
- Có chat hỗ trợ, AI tư vấn, build PC ở mức đồ án

### B. Lời thuyết trình
Kết quả của đồ án là một website có thể chạy demo end-to-end. Người dùng có thể xem sản phẩm, thêm giỏ, đặt hàng, theo dõi đơn, đánh giá và gửi bảo hành. Admin có thể quản lý sản phẩm, đơn hàng, người dùng, nội dung và hỗ trợ khách. Về mặt kỹ thuật, hệ thống áp dụng MVC, EF Core, SQL Server, phân quyền và service layer.

### C. Câu hỏi dễ bị hỏi
- Chức năng nào em tự tin nhất?
- Chức năng nào khó nhất?
- Nếu demo lỗi thì xử lý sao?

### D. Câu trả lời an toàn
- Em tự tin nhất ở luồng sản phẩm - giỏ hàng - đặt hàng - quản trị đơn.
- Khó nhất là các chức năng liên quan nhiều nghiệp vụ như checkout, phí vận chuyển, voucher, trạng thái thanh toán.
- Nếu demo lỗi do môi trường, em có thể trình bày source code, database và ảnh minh chứng các luồng chính.

### E. Ghi chú tránh bị hỏi khó
- Nên nói kỹ luồng lõi thay vì khoe quá nhiều module phụ.
- Chuẩn bị dữ liệu demo ổn định.

---

## SLIDE 24 – Hạn chế của đề tài

### A. Nội dung đưa lên slide
- Chưa tích hợp cổng thanh toán tự động thật
- Build PC chưa kiểm tra tương thích phần cứng chuyên sâu
- AI chỉ hỗ trợ tư vấn sơ bộ
- Kiểm thử tự động còn hạn chế
- Chưa có audit log/admin activity chi tiết
- Bảo mật cần hardening thêm khi triển khai thật

### B. Lời thuyết trình
Vì đây là đồ án tốt nghiệp nên hệ thống vẫn có một số hạn chế. Thanh toán chuyển khoản hiện là quy trình admin xác nhận, chưa tích hợp ngân hàng hoặc ví điện tử. Build PC mới hỗ trợ chọn linh kiện và cảnh báo tổng quát, chưa kiểm tra đầy đủ socket, RAM, công suất nguồn. AI chat chỉ là kênh hỗ trợ sơ bộ. Nếu triển khai thực tế, cần bổ sung kiểm thử tự động, audit log và bảo mật nâng cao.

### C. Câu hỏi dễ bị hỏi
- Hạn chế lớn nhất là gì?
- Nếu có thêm thời gian em làm gì trước?
- Build PC có sai tư vấn không?

### D. Câu trả lời an toàn
- Hạn chế lớn nhất là các tích hợp thực tế như thanh toán tự động và kiểm tra tương thích phần cứng chuyên sâu.
- Nếu có thêm thời gian, em ưu tiên hoàn thiện kiểm tra tương thích build PC và tích hợp cổng thanh toán.
- Build PC hiện chỉ hỗ trợ chọn linh kiện và cảnh báo, người dùng vẫn cần kiểm tra/tư vấn trước khi mua.

### E. Ghi chú tránh bị hỏi khó
- Chủ động nêu hạn chế để giảm bị bắt bẻ.
- Không phủ nhận hạn chế; biến thành hướng phát triển.

---

## SLIDE 25 – Hướng phát triển và lời kết

### A. Nội dung đưa lên slide
- Tích hợp VNPay/Momo/ngân hàng tự động
- Luật tương thích build PC chuyên sâu
- Tối ưu tìm kiếm và gợi ý sản phẩm
- Bổ sung unit/integration test
- Audit log và bảo mật nâng cao
- Triển khai cloud, CI/CD, monitoring

### B. Lời thuyết trình
Trong tương lai, KKSHOP có thể phát triển theo hướng gần với sản phẩm thực tế hơn. Các hướng quan trọng là tích hợp thanh toán tự động, hoàn thiện luật tương thích linh kiện, nâng cấp tìm kiếm và gợi ý sản phẩm, bổ sung kiểm thử tự động, audit log và triển khai cloud. Em xin kết thúc phần trình bày và mong nhận được góp ý từ hội đồng.

### C. Câu hỏi dễ bị hỏi
- Hướng phát triển nào khả thi nhất?
- Nếu triển khai thật cần làm gì đầu tiên?
- Em học được gì từ đồ án?

### D. Câu trả lời an toàn
- Khả thi nhất là bổ sung kiểm thử tự động và hoàn thiện luật build PC vì dựa trên nền code hiện có.
- Nếu triển khai thật, cần hardening bảo mật, cấu hình secret an toàn, backup database và kiểm thử tải.
- Em học được cách phân tích nghiệp vụ, thiết kế database, triển khai MVC và xử lý luồng thương mại điện tử.

### E. Ghi chú tránh bị hỏi khó
- Kết thúc bằng thái độ cầu thị.
- Không hứa hệ thống đã sẵn sàng production ngay.

---

# 10 câu hỏi phản biện dễ gặp và câu trả lời mẫu an toàn

1. **Vì sao chọn ASP.NET Core MVC thay vì React/API?**  
   MVC phù hợp với quy mô đồ án, dễ triển khai server-side rendering, dễ trình bày mô hình Controller - Model - View và vẫn đủ cho nghiệp vụ thương mại điện tử.

2. **Hệ thống đã có thanh toán online thật chưa?**  
   Chưa. Trong phạm vi đồ án, hệ thống hỗ trợ COD và chuyển khoản ngân hàng theo quy trình admin xác nhận. Tích hợp VNPay/Momo là hướng phát triển.

3. **Build PC có kiểm tra tương thích phần cứng không?**  
   Có cảnh báo tổng quát như socket CPU/mainboard, chuẩn RAM/mainboard và công suất PSU, nhưng chưa kiểm tra tự động chuyên sâu. Đây là phần em xác định mở rộng tiếp.

4. **AI tư vấn dựa vào đâu?**  
   AI lấy ngữ cảnh từ dữ liệu sản phẩm trong database và chính sách shop, sau đó tạo gợi ý. AI chỉ hỗ trợ tư vấn sơ bộ, không thay thế nhân viên kỹ thuật.

5. **Bảo mật mật khẩu thế nào?**  
   Hệ thống lưu mật khẩu dạng hash, dùng cookie authentication và phân quyền role-based ở controller. Nếu triển khai thật cần bổ sung hardening, secret manager, HTTPS bắt buộc và audit log.

6. **Có chống người dùng đánh giá ảo không?**  
   Đánh giá được gắn với sản phẩm, người dùng và đơn hàng, có ràng buộc tránh trùng trên cùng bộ Product/User/Order. Chống spam nâng cao là hướng phát triển.

7. **Tại sao cần OrderDetail lưu ProductName/ProductImage/UnitPrice?**  
   Đây là snapshot tại thời điểm mua, giúp đơn hàng vẫn đúng dù sau này sản phẩm đổi tên, đổi ảnh hoặc đổi giá.

8. **Nếu nhiều người đặt cùng lúc gây hết hàng thì sao?**  
   Hiện hệ thống kiểm tra tồn kho ở mức nghiệp vụ đồ án. Khi triển khai thực tế cần bổ sung transaction/locking chặt hơn để xử lý cạnh tranh.

9. **Có kiểm thử tự động không?**  
   Đồ án chủ yếu kiểm thử chức năng thủ công theo kịch bản. Unit test và integration test tự động là hướng phát triển tiếp.

10. **Điểm mạnh nhất của đồ án là gì?**  
    Điểm mạnh là luồng thương mại điện tử tương đối đầy đủ: sản phẩm, linh kiện, giỏ hàng, đặt hàng, voucher, quản trị, bảo hành, đánh giá, chat và build PC.

# Chức năng nên nói kỹ

- **Luồng sản phẩm → giỏ hàng → checkout → đơn hàng**: đây là lõi e-commerce và có nhiều bằng chứng code.
- **Thiết kế database Order/OrderDetail/Product/User/Voucher**: dễ trình bày lý thuyết ERD.
- **Phân quyền role-based trong admin**: rõ ràng, dễ bảo vệ.
- **Snapshot đơn hàng và trạng thái đơn/thanh toán**: thể hiện hiểu nghiệp vụ.
- **Bảo hành và đánh giá**: thể hiện phần sau bán.

# Chức năng nên nói vừa phải

- **Phí vận chuyển GHN/OpenRouteService**: nói là tích hợp/cấu hình hỗ trợ, tránh đi sâu nếu API key/môi trường không ổn định.
- **Cloudinary upload ảnh**: nói là tùy chọn upload ảnh, không coi là lõi.
- **Search keyword/suggestion**: nói vừa phải như hỗ trợ trải nghiệm tìm kiếm.
- **Báo cáo doanh thu**: trình bày mức tổng hợp cơ bản theo đơn hàng.

# Chức năng không nên khoe quá sâu

- **AI chat**: không nói AI chính xác tuyệt đối hoặc tư vấn kỹ thuật chuyên sâu.
- **Build PC compatibility**: không nói tự động xác định tương thích toàn bộ phần cứng.
- **Thanh toán chuyển khoản**: không gọi là cổng thanh toán tự động.
- **Bảo mật**: không nói bảo mật tuyệt đối; chỉ nói các cơ chế cơ bản đã áp dụng.
- **Tối ưu hiệu năng**: không nói chịu tải lớn nếu chưa benchmark.

# Gợi ý slide nên chèn ảnh giao diện thật

- Slide 1: ảnh trang chủ/banner KKSHOP.
- Slide 6: ảnh quy trình giỏ hàng/checkout.
- Slide 14: ảnh danh sách sản phẩm hoặc quản trị sản phẩm.
- Slide 15: ảnh giỏ hàng và checkout.
- Slide 18: ảnh dashboard/admin orders.
- Slide 19: ảnh bảo hành/đánh giá/bài viết/banner.
- Slide 20: ảnh chat hỗ trợ hoặc AI chat.
- Slide 23: ảnh tổng hợp 3–4 màn hình kết quả.

# Gợi ý slide nên chèn sơ đồ

- Slide 8: sơ đồ kiến trúc ASP.NET Core MVC.
- Slide 9: sơ đồ MVC trong dự án.
- Slide 10–11: use case tổng quát.
- Slide 12–13: ERD tổng quan.
- Slide 15: sequence giỏ hàng → checkout → tạo đơn.
- Slide 16: state diagram trạng thái đơn hàng/thanh toán.
- Slide 17: flow đăng nhập/phân quyền.
- Slide 20: sequence user → chat/AI service → database → response.

# Gợi ý màu và bố cục slide

- Tông chính: đỏ đậm `#C1121F`, trắng `#FFFFFF`, xám đậm `#2B2D42`, xám nhạt `#F2F2F2`.
- Mỗi slide nên có 3–5 bullet, không nhồi chữ.
- Dùng icon nhất quán: product, cart, order, shield, database, chat.
- Các slide lý thuyết dùng sơ đồ đơn giản; các slide kết quả dùng ảnh giao diện thật.
- Chuẩn bị phụ lục backup: ERD chi tiết, bảng test case, bảng phân quyền, ảnh demo chức năng.
