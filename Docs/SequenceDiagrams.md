# Sơ đồ tuần tự PlantUML - PC Store

Tài liệu này được lập sau khi rà soát controller, service, DbContext, model, view và cấu hình khởi động của dự án ASP.NET Core MVC PC Store.

## Tóm tắt logic hệ thống

- Ứng dụng là ASP.NET Core MVC, hiển thị giao diện bằng Razor View, định tuyến mặc định `{controller=Home}/{action=Index}/{id?}` và dùng xác thực bằng cookie với lược đồ `PcStoreCookie`.
- `ApplicationDbContext` là lớp truy cập dữ liệu chính; chưa có repository riêng. Controller gọi trực tiếp `ApplicationDbContext` hoặc đi qua service như `CartService`, `AuthService`, `ShippingService`, `OrderExpirationService`, `ProductReviewService`.
- Danh mục dữ liệu bán hàng gồm `Products`, `Categories`, `ProductImages`, `Banners`, `Articles`, `SiteSettings`.
- Giỏ hàng có hai nhánh: khách vãng lai lưu trong khóa phiên `guest_cart`; người dùng đăng nhập lưu bảng `Carts`/`CartItems`.
- Quy trình thanh toán tạo `Orders` và `OrderDetails` trong giao dịch, kiểm tra tồn kho, trừ tồn kho sản phẩm, lưu địa chỉ/phí giao hàng/phương thức thanh toán.
- Hệ thống chỉ có hai phương thức thanh toán đang được dùng trong thanh toán: COD và chuyển khoản ngân hàng. Chuyển khoản tạo đơn `chờ thanh toán`, có hạn thanh toán và trang `chuyển khoản ngân hàng`; khách xác nhận đã chuyển khoản, quản trị viên xác nhận thanh toán.
- Quản trị viên dùng `[Authorize(Roles = "Admin")]` cho quản lý sản phẩm, đơn hàng, danh mục, banner, người dùng, cài đặt, bảo hành, đánh giá. Bài viết cho phép `Admin,Staff` tạo/sửa và chỉ `Admin` xóa.

## Luồng nghiệp vụ tìm thấy

1. Khách hàng xem trang chủ, danh sách sản phẩm, chi tiết sản phẩm và đánh giá.
2. Khách hàng thêm/cập nhật/xóa/xóa toàn bộ giỏ hàng; có phân nhánh giỏ phiên của khách vãng lai và giỏ trong Database của người dùng.
3. Đăng ký, đăng nhập, đăng xuất, cập nhật hồ sơ, đổi mật khẩu, quên mật khẩu bằng OTP email.
4. Thanh toán, tính phí giao hàng, tạo đơn COD hoặc chuyển khoản, theo dõi đơn, tra cứu đơn khách vãng lai, xem đơn của tôi.
5. Quản trị viên quản lý đơn hàng, cập nhật trạng thái, xác nhận chuyển khoản.
6. Quản trị viên quản lý sản phẩm: danh sách, tạo, sửa, xóa, ảnh sản phẩm, trạng thái tồn kho.
7. Bài viết/tin tức: người dùng công khai xem danh sách/chi tiết; Quản trị viên/Nhân viên tạo/sửa; Quản trị viên xóa.
8. Đánh giá sản phẩm: người dùng đã đăng nhập đánh giá sản phẩm thuộc đơn hoàn thành; quản trị viên duyệt/ẩn/trả lời/xóa.
9. Bảo hành, hỗ trợ chat, build PC, so sánh sản phẩm có controller/service riêng; không đưa hết vào sơ đồ tổng thể để tránh quá rối.

## Sơ đồ 1 - Khách hàng duyệt sản phẩm, giỏ hàng, đặt hàng và thanh toán

```plantuml
@startuml
title PC Store - Khách hàng duyệt sản phẩm, giỏ hàng, đặt hàng và thanh toán

actor "Khách hàng" as C
participant "Trình duyệt / Giao diện Razor" as UI
participant "ProductsController" as Products
participant "CartController" as CartCtrl
participant "OrdersController" as Orders
participant "ShippingController\nAPI tính phí giao hàng" as ShipApi
participant "CartService" as CartSvc
participant "ShippingService" as ShipSvc
participant "OrderExpirationService" as ExpSvc
participant "ProductReviewService" as ReviewSvc
participant "Dịch vụ GHN\n(bên ngoài)" as GHN
participant "ApplicationDbContext" as DbCtx
database "SQL Server" as DB
collections "Phiên làm việc" as Session

note over DbCtx
Dự án không có repository riêng.
Controller/service truy cập dữ liệu qua ApplicationDbContext + EF Core.
end note

group Xem danh sách sản phẩm
C -> UI: Mở trang danh sách sản phẩm kèm bộ lọc
UI -> Products: Gọi xử lý danh sách với từ khóa, danh mục, hãng, giá và cấu hình
activate Products
Products -> DbCtx: Lấy danh sách sản phẩm kèm danh mục
DbCtx -> DB: Truy vấn sản phẩm và danh mục
DB --> DbCtx: Trả về các sản phẩm phù hợp ban đầu
DbCtx --> Products: Trả về dữ liệu Entity
Products -> Products: Áp dụng bộ lọc từ khóa, giá, hãng, CPU/RAM/GPU
Products -> DbCtx: Lấy danh mục để hiển thị bộ lọc
DbCtx -> DB: Truy vấn danh mục
DB --> DbCtx: Trả về danh mục
Products --> UI: Trả về trang danh sách sản phẩm
deactivate Products
UI --> C: Hiển thị danh sách + bộ lọc
end

group Xem chi tiết sản phẩm và đánh giá
C -> UI: Mở trang chi tiết sản phẩm
UI -> Products: Gọi xử lý chi tiết sản phẩm với mã sản phẩm và mức sao nếu có
activate Products
Products -> DbCtx: Tải sản phẩm kèm danh mục và hình ảnh
DbCtx -> DB: Truy vấn chi tiết sản phẩm
DB --> DbCtx: Trả về sản phẩm
alt Không tìm thấy sản phẩm
Products --> UI: Trả về lỗi không tìm thấy
else Tìm thấy sản phẩm
Products -> DbCtx: Tải gợi ý nâng cấp cùng danh mục và giá cao hơn
DbCtx -> DB: Truy vấn sản phẩm gợi ý
Products -> ReviewSvc: Lấy khu vực đánh giá của sản phẩm
activate ReviewSvc
ReviewSvc -> DbCtx: Lấy đánh giá đã duyệt và dữ liệu kiểm tra quyền đánh giá
DbCtx -> DB: Truy vấn đánh giá sản phẩm và đơn hàng liên quan
DB --> DbCtx: Trả về dữ liệu đánh giá
ReviewSvc --> Products: Trả về dữ liệu hiển thị đánh giá
deactivate ReviewSvc
Products --> UI: Trả về trang chi tiết sản phẩm
end
deactivate Products
UI --> C: Hiển thị chi tiết, gợi ý nâng cấp, đánh giá
end

group Thêm sản phẩm vào giỏ hàng
C -> UI: Gửi yêu cầu thêm sản phẩm vào giỏ hàng
UI -> CartCtrl: Gọi xử lý thêm vào giỏ hàng
activate CartCtrl
CartCtrl -> CartSvc: Thêm sản phẩm vào giỏ theo người dùng hoặc phiên
activate CartSvc
CartSvc -> DbCtx: Tìm sản phẩm đang hoạt động
DbCtx -> DB: Truy vấn sản phẩm đang hoạt động
DB --> DbCtx: Trả về sản phẩm hoặc không có dữ liệu
alt Không có sản phẩm hoặc không đủ tồn kho
CartSvc --> CartCtrl: Trả về lỗi nghiệp vụ
CartCtrl --> UI: Quay lại trang trước và hiển thị lỗi
else Người dùng đã đăng nhập
CartSvc -> DbCtx: Lấy hoặc tạo giỏ hàng của người dùng
DbCtx -> DB: Truy vấn hoặc tạo giỏ hàng
CartSvc -> DbCtx: Tìm hoặc thêm dòng giỏ hàng và giới hạn theo tồn kho
DbCtx -> DB: Truy vấn, thêm hoặc cập nhật dòng giỏ hàng
CartSvc -> DbCtx: Lưu thay đổi
DbCtx -> DB: Ghi nhận giao dịch
CartSvc --> CartCtrl: Trả về thành công
CartCtrl --> UI: Chuyển đến trang giỏ hàng
else Khách vãng lai chưa đăng nhập
CartSvc -> Session: Đọc giỏ hàng trong phiên
CartSvc -> Session: Thêm hoặc cập nhật dòng giỏ hàng trong phiên
CartSvc --> CartCtrl: Trả về thành công
CartCtrl --> UI: Chuyển đến trang giỏ hàng
end
deactivate CartSvc
deactivate CartCtrl
UI --> C: Cập nhật giỏ hàng
end

group Cập nhật hoặc xóa giỏ hàng
C -> UI: Gửi yêu cầu cập nhật, xóa một sản phẩm hoặc xóa toàn bộ giỏ hàng
UI -> CartCtrl: Gọi xử lý cập nhật, xóa hoặc xóa toàn bộ
activate CartCtrl
CartCtrl -> CartSvc: Cập nhật số lượng, xóa dòng hoặc xóa toàn bộ giỏ hàng
activate CartSvc
alt Người dùng đã đăng nhập
CartSvc -> DbCtx: Tải giỏ hàng, kiểm tra chủ sở hữu và tồn kho
DbCtx -> DB: Truy vấn, cập nhật hoặc xóa dòng giỏ hàng
CartSvc -> DbCtx: Lưu thay đổi
DbCtx -> DB: Ghi nhận giao dịch
else Khách vãng lai
CartSvc -> Session: Đọc hoặc ghi giỏ hàng trong phiên
end
CartSvc --> CartCtrl: Trả về thành công hoặc lỗi kiểm tra
CartCtrl --> UI: Chuyển đến trang giỏ hàng
deactivate CartSvc
deactivate CartCtrl
UI --> C: Hiển thị giỏ mới
end

group Tính phí giao hàng tại thanh toán
C -> UI: Mở trang thanh toán
UI -> Orders: Hiển thị form thanh toán
activate Orders
opt Người dùng đã đăng nhập
Orders -> DbCtx: Tải người dùng để điền tên, email và số điện thoại
DbCtx -> DB: Truy vấn người dùng
end
Orders -> CartSvc: Lấy giỏ hàng theo người dùng hoặc phiên
CartSvc -> DbCtx: Tải giỏ hàng trong Database nếu đã đăng nhập
CartSvc -> Session: Tải giỏ hàng trong phiên nếu là khách vãng lai
Orders --> UI: Trả về trang thanh toán kèm giỏ hàng
deactivate Orders

C -> UI: Chọn địa chỉ, bấm tính phí
UI -> ShipApi: Gửi yêu cầu tính phí giao hàng
activate ShipApi
ShipApi -> CartSvc: Lấy giỏ hàng theo phiên hiện tại
note right of ShipApi
Mã nguồn hiện tính phí giao hàng dựa trên giỏ hàng khách vãng lai (không truyền mã người dùng),
kể cả khi thanh toán bằng tài khoản đã đăng nhập.
end note
ShipApi -> ShipSvc: Tính phí giao hàng theo địa chỉ, khối lượng và kích thước
activate ShipSvc
ShipSvc -> DbCtx: Tải cấu hình giao hàng đang hoạt động
DbCtx -> DB: Truy vấn cấu hình giao hàng
alt Cùng khu vực cửa hàng theo chính sách
ShipSvc --> ShipApi: Trả về phí giao hàng miễn phí theo khu vực nội bộ
else Ngoài khu vực và bật GHN
ShipSvc -> GHN: Yêu cầu GHN tính phí giao hàng
alt GHN tính phí thành công
GHN --> ShipSvc: Trả về phí và thời gian giao dự kiến
ShipSvc --> ShipApi: Trả về phí giao hàng từ GHN
else GHN lỗi hoặc bị tắt
ShipSvc -> ShipSvc: Tính phí bằng công thức nội bộ dự phòng
ShipSvc --> ShipApi: Trả về phí giao hàng dự phòng
end
end
deactivate ShipSvc
ShipApi --> UI: Trả về kết quả tính phí, nhà vận chuyển và công thức
deactivate ShipApi
UI --> C: Cập nhật phí giao hàng vào form
end

group Đặt hàng
C -> UI: Gửi form đặt hàng
UI -> Orders: Xử lý thông tin thanh toán
activate Orders
Orders -> Orders: Kiểm tra họ tên, số điện thoại, email, địa chỉ, phí giao hàng và phương thức thanh toán
Orders -> CartSvc: Lấy giỏ hàng theo người dùng hoặc phiên
CartSvc -> DbCtx: Tải giỏ hàng trong Database nếu là người dùng
CartSvc -> Session: Tải giỏ hàng trong phiên nếu là khách vãng lai
alt Dữ liệu không hợp lệ hoặc giỏ hàng trống
Orders --> UI: Trả về trang thanh toán và hiển thị lỗi
else Dữ liệu hợp lệ
Orders -> DbCtx: Bắt đầu giao dịch tạo đơn hàng
DbCtx -> DB: Mở giao dịch Database
loop Mỗi sản phẩm trong giỏ
Orders -> DbCtx: Tải sản phẩm đang hoạt động và kiểm tra tồn kho
DbCtx -> DB: Truy vấn sản phẩm
Orders -> Orders: Tạo chi tiết đơn hàng và tính tạm tính
end
alt Chuyển khoản ngân hàng và đã có đơn chờ thanh toán chưa hết hạn
Orders -> ExpSvc: Kiểm tra và hết hạn đơn chờ thanh toán nếu cần
ExpSvc -> DbCtx: Cập nhật nếu đơn đã hết hạn
Orders --> UI: Chuyển đến trang chuyển khoản của đơn hiện có
else Tạo đơn mới
Orders -> DbCtx: Thêm đơn hàng và chi tiết đơn hàng
loop Mỗi sản phẩm
Orders -> DbCtx: Trừ tồn kho và cập nhật trạng thái còn hàng
end
DbCtx -> DB: Thêm đơn hàng, chi tiết đơn hàng và cập nhật sản phẩm
alt Phương thức thanh toán là chuyển khoản ngân hàng
Orders -> DbCtx: Lưu đơn hàng và đặt nội dung chuyển khoản
Orders -> Session: Lưu mã đơn chờ thanh toán và mã đơn gần nhất vào phiên
note right of Orders
Mã nguồn không xóa giỏ ngay khi tạo đơn chuyển khoản.
Giỏ được xóa khi quản trị viên xác nhận chuyển khoản cho người dùng đã đăng nhập.
end note
Orders -> DbCtx: Hoàn tất giao dịch
Orders --> UI: Chuyển đến trang chuyển khoản
else Phương thức thanh toán là thanh toán khi nhận hàng
Orders -> CartSvc: Xóa giỏ hàng sau khi đặt hàng
CartSvc -> DbCtx: Xóa dòng giỏ hàng nếu là người dùng
CartSvc -> Session: Xóa giỏ hàng trong phiên nếu là khách vãng lai
Orders -> DbCtx: Hoàn tất giao dịch
Orders --> UI: Chuyển đến trang đặt hàng thành công
end
end
end
deactivate Orders
UI --> C: Hiển thị thành công hoặc trang chuyển khoản
end

group Thanh toán chuyển khoản và theo dõi đơn
C -> UI: Mở trang hướng dẫn chuyển khoản
UI -> Orders: Hiển thị thông tin chuyển khoản
activate Orders
Orders -> DbCtx: Tải đơn hàng và chi tiết
DbCtx -> DB: Truy vấn đơn hàng và chi tiết
Orders -> Orders: Kiểm tra quyền xem đơn hàng
alt Không có quyền
Orders --> UI: Trả về lỗi không có quyền truy cập
else Có quyền
Orders -> ExpSvc: Kiểm tra và hết hạn đơn nếu cần
alt Hết hạn thanh toán
Orders --> UI: Chuyển về trang thanh toán và hiển thị lỗi
else Còn hạn
Orders --> UI: Hiển thị trang chuyển khoản và thời gian còn lại
end
end
deactivate Orders

C -> UI: Gửi xác nhận đã chuyển khoản
UI -> Orders: Xử lý xác nhận đã chuyển khoản
activate Orders
Orders -> DbCtx: Tải đơn hàng
Orders -> Orders: Kiểm tra quyền truy cập, phương thức chuyển khoản và hạn thanh toán
alt Hợp lệ
Orders -> ExpSvc: Đánh dấu khách đã xác nhận chuyển khoản
Orders -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng sang chờ xác nhận chuyển khoản
Orders --> UI: Chuyển đến trang theo dõi đơn hàng
else Không hợp lệ
Orders --> UI: Trả về lỗi hoặc chuyển trang kèm thông báo lỗi
end
deactivate Orders
end
@enduml
```

## Sơ đồ 2 - Đăng ký, đăng nhập, quên mật khẩu

```plantuml
@startuml
title PC Store - Đăng ký, đăng nhập và đặt lại mật khẩu

actor "Khách truy cập" as V
participant "Trình duyệt / Giao diện Razor" as UI
participant "AccountController" as Account
participant "AuthService" as Auth
participant "CartService" as CartSvc
participant "IEmailSender\nSmtpEmailSender" as Email
participant "AccountPasswordResetService" as ResetSvc
participant "ApplicationDbContext" as DbCtx
database "SQL Server" as DB
collections "Phiên làm việc" as Session

note over Account
Xác thực bằng cookie dùng lược đồ PcStoreCookie.
Middleware phân quyền chuyển hướng trang đăng nhập hoặc trả mã 401/403 cho yêu cầu dữ liệu.
end note

group Đăng ký tài khoản
V -> UI: Mở trang đăng ký
UI -> Account: Hiển thị form đăng ký
Account --> UI: Trả về trang đăng ký
V -> UI: Gửi form đăng ký
UI -> Account: Xử lý đăng ký tài khoản
activate Account
Account -> Account: Kiểm tra dữ liệu form
alt Dữ liệu form không hợp lệ
Account --> UI: Trả về trang hiện tại và hiển thị lỗi
else Dữ liệu hợp lệ
Account -> DbCtx: Kiểm tra email đã tồn tại
DbCtx -> DB: Truy vấn người dùng theo email
alt Email đã tồn tại
Account --> UI: Trả về form và hiển thị lỗi email
else Email chưa tồn tại
Account -> DbCtx: Tải vai trò khách hàng
DbCtx -> DB: Truy vấn vai trò người dùng
Account -> Auth: Băm mật khẩu
Auth --> Account: Trả về mật khẩu đã băm
Account -> DbCtx: Thêm tài khoản khách hàng đang hoạt động
DbCtx -> DB: Ghi tài khoản người dùng mới
Account --> UI: Chuyển đến trang đăng nhập và thông báo thành công
end
end
deactivate Account
end

group Đăng nhập và gộp giỏ khách vãng lai
V -> UI: Gửi form đăng nhập
UI -> Account: Xử lý đăng nhập
activate Account
Account -> Auth: Kiểm tra email và mật khẩu
activate Auth
Auth -> DbCtx: Tải người dùng và vai trò theo email
DbCtx -> DB: Truy vấn người dùng kèm vai trò
alt Không có người dùng hoặc mật khẩu sai
Auth --> Account: Không có dữ liệu người dùng hợp lệ
Account --> UI: Trả về trang đăng nhập và báo sai thông tin
else Tài khoản bị khóa
Auth --> Account: Trả về người dùng
Account --> UI: Trả về trang đăng nhập và báo tài khoản bị khóa
else Hợp lệ
Auth --> Account: Trả về người dùng
Account -> Account: Tạo thông tin định danh và vai trò cho cookie
Account -> UI: Ghi cookie đăng nhập
Account -> CartSvc: Gộp giỏ hàng khách vãng lai vào tài khoản
activate CartSvc
CartSvc -> Session: Đọc giỏ hàng trong phiên
loop Mỗi sản phẩm trong giỏ khách vãng lai
CartSvc -> DbCtx: Thêm sản phẩm từ giỏ phiên vào giỏ tài khoản
DbCtx -> DB: Truy vấn sản phẩm, giỏ hàng và cập nhật dòng giỏ hàng
end
CartSvc -> Session: Xóa giỏ hàng khách vãng lai trong phiên
CartSvc --> Account: Hoàn tất gộp giỏ hàng
deactivate CartSvc
Account --> UI: Chuyển về trang chủ
end
deactivate Auth
deactivate Account
end

group Đăng xuất
V -> UI: Gửi yêu cầu đăng xuất
UI -> Account: Xử lý đăng xuất
Account -> UI: Xóa cookie đăng nhập
Account --> UI: Chuyển về trang chủ
end

group Quên mật khẩu bằng OTP email
V -> UI: Mở trang quên mật khẩu
UI -> Account: Hiển thị form quên mật khẩu
Account --> UI: Trả về trang nhập thông tin
V -> UI: Gửi email yêu cầu đặt lại mật khẩu
UI -> Account: Xử lý yêu cầu quên mật khẩu
activate Account
Account -> DbCtx: Tìm người dùng theo email
DbCtx -> DB: Truy vấn người dùng
alt Người dùng tồn tại
Account -> DbCtx: Đánh dấu các mã OTP cũ là đã dùng
Account -> Account: Sinh mã OTP 6 số và băm mã
Account -> DbCtx: Lưu mã OTP đặt lại mật khẩu có hạn 10 phút
DbCtx -> DB: Cập nhật mã OTP cũ và ghi mã OTP mới
Account -> Email: Gửi email chứa mã OTP
alt Dịch vụ gửi email lỗi
Email --> Account: Trả về lỗi gửi email
Account -> Account: Ghi nhận lỗi và vẫn trả thông báo chung
else Gửi được
Email --> Account: Gửi email thành công
end
else Người dùng không tồn tại
Account -> Account: Không tiết lộ email có tồn tại hay không
end
Account --> UI: Chuyển đến trang nhập mã và thông báo chung
deactivate Account

V -> UI: Gửi email, mã OTP và mật khẩu mới
UI -> Account: Xử lý mã xác nhận đặt lại mật khẩu
activate Account
Account -> DbCtx: Tìm người dùng và mã OTP phù hợp
DbCtx -> DB: Truy vấn người dùng và mã OTP đặt lại mật khẩu
alt OTP không tồn tại, đã dùng, hết hạn hoặc form không hợp lệ
Account --> UI: Trả về trang nhập mã và hiển thị lỗi
else OTP hợp lệ
Account -> ResetSvc: Tạo mã nội bộ để đặt lại mật khẩu
ResetSvc --> Account: Trả về mã nội bộ
Account -> ResetSvc: Đặt lại mật khẩu mới
ResetSvc -> DbCtx: Cập nhật mật khẩu đã băm
DbCtx -> DB: Cập nhật tài khoản người dùng
alt Đặt lại mật khẩu thất bại
ResetSvc --> Account: Trả về kết quả thất bại
Account --> UI: Trả về trang hiện tại và hiển thị lỗi
else Đặt lại mật khẩu thành công
Account -> DbCtx: Đánh dấu mã OTP đã sử dụng
DbCtx -> DB: Cập nhật trạng thái mã OTP
Account --> UI: Chuyển đến trang đăng nhập và thông báo thành công
end
end
deactivate Account
end
@enduml
```

## Sơ đồ 3 - Quản trị viên quản lý sản phẩm và bài viết

```plantuml
@startuml
title PC Store - Quản trị viên quản lý sản phẩm và bài viết

actor "Quản trị viên" as A
participant "Trình duyệt / Giao diện quản trị" as UI
participant "Middleware phân quyền" as Authz
participant "AdminProductsController" as AdminProducts
participant "ArticlesController" as Articles
participant "ProductPromotionHelper\nProductComponentSpecHelper" as Helpers
participant "ApplicationDbContext" as DbCtx
database "SQL Server" as DB

note over Authz
AdminProductsController yêu cầu vai trò quản trị viên.
Tạo/sửa bài viết yêu cầu quản trị viên hoặc nhân viên; xóa bài viết yêu cầu quản trị viên.
end note

group Vào trang quản lý sản phẩm
A -> UI: Mở trang quản lý sản phẩm
UI -> Authz: Kiểm tra cookie và vai trò quản trị viên
alt Chưa đăng nhập hoặc sai vai trò
Authz --> UI: Chuyển đến đăng nhập hoặc trang từ chối truy cập
else Được phép truy cập
Authz -> AdminProducts: Lấy danh sách sản phẩm theo từ khóa và danh mục
activate AdminProducts
AdminProducts -> DbCtx: Lấy danh sách sản phẩm kèm danh mục và hình ảnh
DbCtx -> DB: Truy vấn sản phẩm, danh mục và hình ảnh
AdminProducts -> DbCtx: Lấy danh sách danh mục
DbCtx -> DB: Truy vấn danh mục
AdminProducts --> UI: Trả về trang danh sách sản phẩm
deactivate AdminProducts
end
end

group Tạo sản phẩm
A -> UI: Mở form tạo sản phẩm
UI -> AdminProducts: Hiển thị form tạo sản phẩm
AdminProducts -> DbCtx: Tải danh mục
DbCtx -> DB: Truy vấn danh mục
AdminProducts --> UI: Trả về form sản phẩm
A -> UI: Gửi form tạo sản phẩm
UI -> AdminProducts: Xử lý tạo sản phẩm
activate AdminProducts
AdminProducts -> AdminProducts: Kiểm tra dữ liệu form, đường dẫn ảnh và ảnh đại diện bắt buộc
AdminProducts -> Helpers: Tạo nội dung khuyến mại, thông số linh kiện, đường dẫn và mô tả
alt Dữ liệu không hợp lệ
AdminProducts -> DbCtx: Tải lại danh mục
AdminProducts --> UI: Trả về form và hiển thị lỗi
else Dữ liệu hợp lệ
AdminProducts -> DbCtx: Thêm sản phẩm và hình ảnh từ đường dẫn
DbCtx -> DB: Ghi sản phẩm và hình ảnh sản phẩm
AdminProducts --> UI: Chuyển về danh sách và thông báo thành công
end
deactivate AdminProducts
end

group Sửa sản phẩm
A -> UI: Mở form sửa sản phẩm
UI -> AdminProducts: Hiển thị form sửa sản phẩm
activate AdminProducts
AdminProducts -> DbCtx: Tải sản phẩm, hình ảnh và danh mục
DbCtx -> DB: Truy vấn sản phẩm, hình ảnh và danh mục
alt Không tìm thấy sản phẩm
AdminProducts --> UI: Trả về lỗi không tìm thấy
else Tìm thấy dữ liệu
AdminProducts --> UI: Trả về form sửa sản phẩm
end
deactivate AdminProducts

A -> UI: Gửi form sửa sản phẩm
UI -> AdminProducts: Xử lý cập nhật sản phẩm
activate AdminProducts
AdminProducts -> DbCtx: Tải sản phẩm và hình ảnh
DbCtx -> DB: Truy vấn sản phẩm và hình ảnh
alt Không tìm thấy sản phẩm
AdminProducts --> UI: Trả về lỗi không tìm thấy
else Dữ liệu không hợp lệ
AdminProducts -> DbCtx: Tải lại danh mục và hình ảnh hiện có
AdminProducts --> UI: Trả về form và hiển thị lỗi
else Dữ liệu hợp lệ
AdminProducts -> Helpers: Tính lại giá, tồn kho, bảo hành, khuyến mại, thông số và đường dẫn
AdminProducts -> DbCtx: Cập nhật thông tin sản phẩm
opt Có yêu cầu xóa hình ảnh
AdminProducts -> DbCtx: Xóa hình ảnh sản phẩm đã chọn
end
AdminProducts -> DbCtx: Thêm hình ảnh từ đường dẫn và đảm bảo ảnh chính
DbCtx -> DB: Cập nhật sản phẩm và đồng bộ hình ảnh
AdminProducts --> UI: Chuyển về danh sách và thông báo thành công
end
deactivate AdminProducts
end

group Xóa sản phẩm
A -> UI: Gửi yêu cầu xóa sản phẩm
UI -> AdminProducts: Xử lý xóa sản phẩm
activate AdminProducts
AdminProducts -> DbCtx: Tải sản phẩm và hình ảnh
DbCtx -> DB: Truy vấn sản phẩm và hình ảnh
alt Không tìm thấy sản phẩm
AdminProducts --> UI: Chuyển về danh sách sản phẩm
else Tìm thấy dữ liệu
AdminProducts -> DbCtx: Xóa sản phẩm và hình ảnh theo cấu hình quan hệ
DbCtx -> DB: Xóa sản phẩm và hình ảnh sản phẩm
AdminProducts --> UI: Chuyển về danh sách và thông báo thành công
end
deactivate AdminProducts
end

group Quản lý bài viết / tin tức
A -> UI: Mở form tạo hoặc sửa bài viết
UI -> Authz: Kiểm tra vai trò quản trị viên hoặc nhân viên
alt Không đủ quyền
Authz --> UI: Chuyển đến trang từ chối truy cập
else Đủ quyền
Authz -> Articles: Hiển thị form tạo hoặc sửa bài viết
Articles -> DbCtx: Tải bài viết khi sửa
DbCtx -> DB: Truy vấn bài viết
Articles --> UI: Trả về form bài viết
end

A -> UI: Gửi form tạo hoặc sửa bài viết
UI -> Articles: Xử lý tạo hoặc sửa bài viết
activate Articles
Articles -> Articles: Kiểm tra dữ liệu form
alt Dữ liệu không hợp lệ
Articles --> UI: Trả về form và hiển thị lỗi
else Tạo mới hợp lệ
Articles -> DbCtx: Thêm bài viết
DbCtx -> DB: Ghi bài viết mới
Articles --> UI: Chuyển về danh sách bài viết
else Cập nhật hợp lệ
Articles -> DbCtx: Cập nhật bài viết
DbCtx -> DB: Cập nhật bài viết
Articles --> UI: Chuyển về danh sách bài viết
end
deactivate Articles

A -> UI: Gửi yêu cầu xóa bài viết
UI -> Authz: Kiểm tra vai trò quản trị viên
Authz -> Articles: Xử lý xóa bài viết
Articles -> DbCtx: Tìm bài viết và xóa nếu tồn tại
DbCtx -> DB: Truy vấn và xóa bài viết
Articles --> UI: Chuyển về danh sách bài viết
end
@enduml
```

## Sơ đồ 4 - Quản trị viên xử lý đơn hàng và đánh giá

```plantuml
@startuml
title PC Store - Quản trị viên xử lý đơn hàng và đánh giá sản phẩm

actor "Quản trị viên" as A
participant "Trình duyệt / Giao diện quản trị" as UI
participant "Middleware phân quyền" as Authz
participant "AdminOrdersController" as AdminOrders
participant "AdminReviewsController" as AdminReviews
participant "OrderExpirationService" as ExpSvc
participant "ApplicationDbContext" as DbCtx
database "SQL Server" as DB

note over Authz
Các controller trong sơ đồ này yêu cầu vai trò quản trị viên.
end note

group Danh sách và chi tiết đơn hàng
A -> UI: Mở trang quản lý đơn hàng với bộ lọc
UI -> Authz: Kiểm tra quyền quản trị viên
alt Không đủ quyền
Authz --> UI: Chuyển đến đăng nhập hoặc trang từ chối truy cập
else Được phép truy cập
Authz -> AdminOrders: Lấy danh sách đơn hàng theo bộ lọc
activate AdminOrders
AdminOrders -> ExpSvc: Hết hạn các đơn đang chờ thanh toán quá hạn
ExpSvc -> DbCtx: Tìm đơn chờ thanh toán đã quá hạn
DbCtx -> DB: Truy vấn và cập nhật đơn hết hạn nếu có
AdminOrders -> DbCtx: Lấy đơn hàng kèm người dùng, chi tiết và hình ảnh theo bộ lọc
DbCtx -> DB: Truy vấn đơn hàng, người dùng, chi tiết và sản phẩm
AdminOrders -> DbCtx: Lấy thống kê trạng thái đơn hàng
DbCtx -> DB: Truy vấn trạng thái đơn hàng và thanh toán
AdminOrders --> UI: Trả về trang danh sách đơn hàng
deactivate AdminOrders
end

A -> UI: Mở trang chi tiết đơn hàng
UI -> AdminOrders: Hiển thị chi tiết đơn hàng
activate AdminOrders
AdminOrders -> DbCtx: Tải đơn hàng kèm người dùng, chi tiết và hình ảnh
DbCtx -> DB: Truy vấn đầy đủ thông tin chi tiết đơn hàng
alt Không tìm thấy dữ liệu
AdminOrders --> UI: Trả về lỗi không tìm thấy
else Tìm thấy dữ liệu
AdminOrders -> ExpSvc: Kiểm tra và hết hạn đơn nếu cần
AdminOrders --> UI: Trả về trang chi tiết đơn hàng
end
deactivate AdminOrders
end

group Cập nhật trạng thái đơn
A -> UI: Gửi yêu cầu cập nhật trạng thái đơn hàng
UI -> AdminOrders: Xử lý cập nhật trạng thái đơn hàng
activate AdminOrders
AdminOrders -> DbCtx: Tải đơn hàng và chi tiết
DbCtx -> DB: Truy vấn đơn hàng và chi tiết
alt Không tìm thấy đơn hàng hoặc trạng thái không hợp lệ
AdminOrders --> UI: Trả về lỗi không tìm thấy hoặc yêu cầu không hợp lệ
else Chuyển sang chờ thanh toán
AdminOrders -> ExpSvc: Chuẩn bị trạng thái chờ thanh toán và đặt lại hạn thanh toán
AdminOrders -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng
AdminOrders --> UI: Chuyển trang và thông báo thành công
else Chuyển sang đang xử lý
AdminOrders -> ExpSvc: Đánh dấu đã thanh toán bởi quản trị viên
AdminOrders -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng
AdminOrders --> UI: Chuyển trang và thông báo thành công
else Chuyển sang đang giao hoặc hoàn thành
AdminOrders -> AdminOrders: Cập nhật trạng thái đơn hàng và thanh toán khi cần
AdminOrders -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng
AdminOrders --> UI: Chuyển trang và thông báo thành công
else Chuyển sang đã hủy
AdminOrders -> ExpSvc: Đánh dấu đơn hàng đã hủy
ExpSvc -> DbCtx: Khôi phục tồn kho và hủy trạng thái thanh toán theo logic hiện có
DbCtx -> DB: Cập nhật đơn hàng và sản phẩm
AdminOrders -> DbCtx: Lưu thay đổi
AdminOrders --> UI: Chuyển trang và thông báo thành công
else Chuyển sang hết hạn
AdminOrders -> ExpSvc: Đánh dấu đơn hàng hết hạn bởi quản trị viên
ExpSvc -> DbCtx: Cập nhật đơn hàng và thanh toán sang hết hạn theo logic hiện có
DbCtx -> DB: Cập nhật đơn hàng và sản phẩm
AdminOrders -> DbCtx: Lưu thay đổi
AdminOrders --> UI: Chuyển trang và thông báo thành công
else Trạng thái khác
AdminOrders -> DbCtx: Gán trạng thái đơn hàng và lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng
AdminOrders --> UI: Chuyển trang và thông báo thành công
end
deactivate AdminOrders
end

group Quản trị viên xác nhận chuyển khoản
A -> UI: Gửi yêu cầu xác nhận chuyển khoản
UI -> AdminOrders: Xử lý xác nhận chuyển khoản
activate AdminOrders
AdminOrders -> DbCtx: Tải đơn hàng và chi tiết
DbCtx -> DB: Truy vấn đơn hàng và chi tiết
alt Không tìm thấy đơn hàng
AdminOrders --> UI: Trả về lỗi không tìm thấy
else Không phải chuyển khoản hoặc chưa được khách xác nhận chuyển khoản
AdminOrders --> UI: Trả về lỗi yêu cầu không hợp lệ
else Hợp lệ
AdminOrders -> ExpSvc: Đánh dấu đã thanh toán bởi quản trị viên
opt Đơn hàng có tài khoản người dùng
AdminOrders -> DbCtx: Tải giỏ hàng và các dòng giỏ hàng của người dùng
DbCtx -> DB: Truy vấn giỏ hàng và dòng giỏ hàng
AdminOrders -> DbCtx: Xóa dòng giỏ hàng nếu có
end
AdminOrders -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đơn hàng và xóa dòng giỏ hàng nếu có
AdminOrders --> UI: Chuyển về trang chi tiết và thông báo thành công
end
deactivate AdminOrders
end

group Quản trị viên quản lý đánh giá
A -> UI: Mở trang quản lý đánh giá với bộ lọc
UI -> AdminReviews: Lấy danh sách đánh giá theo bộ lọc
activate AdminReviews
AdminReviews -> DbCtx: Lấy đánh giá kèm sản phẩm, người dùng và đơn hàng
DbCtx -> DB: Truy vấn đánh giá, sản phẩm, người dùng và đơn hàng
AdminReviews -> DbCtx: Tải sản phẩm cho bộ lọc
DbCtx -> DB: Truy vấn sản phẩm
AdminReviews --> UI: Trả về trang danh sách đánh giá
deactivate AdminReviews

A -> UI: Gửi yêu cầu cập nhật đánh giá và phản hồi
UI -> AdminReviews: Xử lý cập nhật đánh giá
activate AdminReviews
AdminReviews -> DbCtx: Tìm đánh giá sản phẩm
DbCtx -> DB: Truy vấn đánh giá sản phẩm
alt Không tìm thấy đánh giá
AdminReviews --> UI: Trả về lỗi không tìm thấy
else Phản hồi dài hơn 1000 ký tự
AdminReviews --> UI: Chuyển về trang chi tiết và hiển thị lỗi
else Dữ liệu hợp lệ
AdminReviews -> AdminReviews: Cập nhật trạng thái, phản hồi và thông tin người xử lý
AdminReviews -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đánh giá sản phẩm
AdminReviews --> UI: Chuyển về trang chi tiết và thông báo thành công
end
deactivate AdminReviews

A -> UI: Gửi yêu cầu ẩn/hiện, đổi trạng thái hoặc xóa đánh giá
UI -> AdminReviews: Xử lý ẩn/hiện, đổi trạng thái hoặc xóa đánh giá
activate AdminReviews
AdminReviews -> DbCtx: Tìm đánh giá sản phẩm
DbCtx -> DB: Truy vấn đánh giá sản phẩm
alt Không tìm thấy đánh giá
AdminReviews --> UI: Trả về lỗi không tìm thấy hoặc chuyển trang kèm lỗi
else Ẩn/hiện hoặc đổi trạng thái
AdminReviews -> AdminReviews: Cập nhật trạng thái và thông tin người xử lý
AdminReviews -> DbCtx: Lưu thay đổi
DbCtx -> DB: Cập nhật đánh giá sản phẩm
AdminReviews --> UI: Chuyển về danh sách đánh giá
else Xóa đánh giá
AdminReviews -> DbCtx: Xóa đánh giá sản phẩm
DbCtx -> DB: Xóa đánh giá sản phẩm
AdminReviews --> UI: Chuyển về danh sách đánh giá
end
deactivate AdminReviews
end
@enduml
```

## Điểm nghi ngờ / chưa chắc chắn khi đọc code

- Không thấy repository layer riêng; sơ đồ dùng `ApplicationDbContext` thay cho repository.
- `ShippingController.Calculate` gọi `_cartService.GetCartAsync(null)`, tức luôn lấy giỏ phiên/khách vãng lai để tính kích thước cân nặng, chưa truyền mã người dùng đăng nhập.
- Checkout chuyển khoản tạo đơn và trừ tồn kho nhưng không xóa giỏ ngay; code chỉ xóa giỏ người dùng trong `AdminOrdersController.ConfirmBankTransfer`. Giỏ hàng khách vãng lai chuyển khoản không có bước xóa rõ ràng tại thời điểm tạo đơn.
- Có trường `PaymentUrl` và các cột payment gateway trong migration, nhưng thanh toán hiện chỉ chấp nhận `COD` và `chuyển khoản ngân hàng`; không vẽ cổng thanh toán online như luồng đang tồn tại.
- Có các module Build PC, Warranty, Support Chat, Compare, Contact, Banner, Category, User, Settings; đã rà soát ở mức xác định luồng, nhưng không đưa đầy đủ vào sơ đồ chính để tránh quá tải.
