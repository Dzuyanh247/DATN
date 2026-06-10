# Hướng dẫn thay khung giao diện bằng ảnh chụp thật

Deck hiện đã có khung giao diện vector thay cho placeholder trống. Nếu có môi trường SQL Server chạy được, nên thay các khung này bằng screenshot thật, giữ nguyên phần mô tả bên cạnh.

- Slide 10 — Trang chủ — điểm vào hành trình mua sắm: Có. Khung giao diện vector dựa trên Views/Home/Index.cshtml.
- Slide 11 — Danh sách sản phẩm — tìm kiếm và thu hẹp lựa chọn: Có. Khung giao diện vector dựa trên Views/Products/Index.cshtml.
- Slide 12 — Chi tiết sản phẩm — đủ dữ liệu để ra quyết định: Có. Khung giao diện vector dựa trên Views/Products/Detail.cshtml.
- Slide 13 — So sánh sản phẩm — đối chiếu tối đa 2 lựa chọn: Có. Khung giao diện vector dựa trên Views/Compare/Index.cshtml.
- Slide 14 — Build PC — chọn linh kiện và kiểm tra tương thích: Có. Khung giao diện vector dựa trên Views/BuildPc/Index.cshtml.
- Slide 16 — Giỏ hàng — hợp nhất khách vãng lai và người đăng nhập: Có. Khung giao diện vector dựa trên Views/Cart/Index.cshtml.
- Slide 17 — Checkout — địa chỉ GHN và phí vận chuyển: Có. Nội dung dựa trên Views/Orders/Checkout.cshtml và ShippingController.
- Slide 18 — Thanh toán — COD và chuyển khoản QR có thời hạn: Có. Nội dung dựa trên Views/Orders/BankTransfer.cshtml.
- Slide 19 — Đơn hàng — theo dõi, lịch sử và xuất chứng từ: Có. Nội dung dựa trên nhóm Views/Orders.
- Slide 20 — Hỗ trợ realtime — chat khách hàng và feedback: Có. Nội dung dựa trên _SupportChatBox, SupportChatController và ContactController.
- Slide 22 — Quản trị hệ thống — trung tâm vận hành cửa hàng: Có. Nội dung dựa trên Views/AdminDashboard/Index.cshtml.
- Slide 23 — Quản trị catalog — sản phẩm, ảnh và danh mục: Có. Nội dung dựa trên AdminProducts và AdminCategories.
- Slide 25 — Quản trị tài khoản — vai trò và trạng thái hoạt động: Có. Nội dung dựa trên Views/AdminUsers.
- Slide 27 — Trung tâm hỗ trợ — chat, feedback và bảo hành: Có. Nội dung dựa trên AdminChat, Contact/Manage và AdminWarranty.
