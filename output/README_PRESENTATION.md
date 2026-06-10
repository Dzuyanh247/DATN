# Hướng dẫn tạo PowerPoint DATN PC Store

Bài thuyết trình bảo vệ đồ án gồm **30 slide** theo phong cách “clean IT thesis
deck”. File PowerPoint là sản phẩm build local và không được lưu trong Git.

## Nội dung đã xác minh

Generator rà soát các marker chức năng trong `Controllers`, `Models`,
`ViewModels`, `Services`, `Views`, `Program.cs`, `ApplicationDbContext`,
`wwwroot/js`, `wwwroot/css` và migration trước khi tạo deck. Nội dung chỉ dùng
các chức năng có trong source: sản phẩm, tài khoản, giỏ hàng, đặt hàng,
COD/chuyển khoản QR, GHN, Build PC, so sánh, chat SignalR, bảo hành, báo giá và
quản trị.

## Cách tạo

Từ thư mục gốc repository, chạy:

```bash
python Scripts/generate_presentation.py
```

Kết quả local:

- `output/DATN_PC_Store_Gioi_Thieu.pptx`
- `output/presentation_speech.md`

Script không tải ảnh, không tạo base64/PDF/video và chỉ dùng shape, text cùng
OOXML chuẩn để không phụ thuộc package ngoài. Sau khi sinh, script tự kiểm tra
ZIP, parse XML của 30 slide và đối chiếu đủ 30 phần lời thuyết trình.

## Chèn ảnh giao diện thật

Các slide **14–23, 25 và 26** có placeholder nét đứt và URL cần chụp. Hãy chạy
ứng dụng, chụp đúng màn hình thật rồi thay placeholder trong PowerPoint.

## Lưu ý Git

`output/*.pptx` nằm trong `.gitignore`. Không dùng `git add -f` để commit file
PowerPoint. Chỉ commit script và các file Markdown liên quan.
