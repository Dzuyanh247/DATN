# Hướng dẫn tạo PowerPoint DATN PC Store

Bài thuyết trình bảo vệ đồ án gồm **30 slide**, được sinh hoàn toàn từ
`Scripts/generate_presentation.py`. File PowerPoint là sản phẩm build local và
không được lưu trong Git.

## Nội dung đã xác minh

Nội dung slide được đối chiếu với `Controllers`, `Models`, `ViewModels`, `Views`,
`Services`, `wwwroot/js`, `wwwroot/css`, `Program.cs`, `ApplicationDbContext` và
các migration. Deck chỉ trình bày chức năng có trong source: sản phẩm, tài khoản,
giỏ hàng, đặt hàng, COD/chuyển khoản QR, GHN, Build PC, so sánh, bảo hành, báo
giá, SignalR chat và quản trị.

## Cách tạo

Từ thư mục gốc repository, chạy:

```bash
python Scripts/generate_presentation.py
```

Script không cần tải ảnh và không dùng base64. Trong môi trường không có
`python-pptx`, generator tạo trực tiếp gói OOXML chuẩn 16:9.

Kết quả:

- `output/DATN_PC_Store_Gioi_Thieu.pptx`
- `output/presentation_notes.md`

## Chèn ảnh giao diện thật

Các slide 14, 15, 16, 17, 19, 20, 21, 22, 23, 24, 25 và 26 có placeholder nét
đứt, kèm URL cần chụp. Hãy chạy ứng dụng, chụp đúng màn hình thật và thay
placeholder trong PowerPoint.

## Lưu ý Git

`*.pptx` nằm trong `.gitignore`. Không dùng `git add -f` để commit file PowerPoint.
Chỉ commit script và các file Markdown liên quan.
