# PowerPoint bảo vệ đồ án — DATN PC Store

Bài thuyết trình gồm **11 slide**, được thiết kế theo phong cách bảo vệ đồ án
tốt nghiệp CNTT: hiện đại, dễ đọc, dùng card/khối nội dung thay cho danh sách
gạch đầu dòng dài và đồng bộ với bảng màu của website (`#002b36`, `#14d9ff`,
`#ff6b00`).

## Cấu trúc nội dung

1. Trang bìa
2. Giới thiệu đề tài
3. Tổng quan hệ thống
4. Công nghệ sử dụng
5. Chức năng dành cho khách hàng
6. Chức năng quản trị
7. Giao diện trang chủ
8. Giao diện chi tiết sản phẩm
9. Giỏ hàng và thanh toán
10. Ưu điểm của hệ thống
11. Kết luận và hướng phát triển

Mỗi slide có nội dung đủ để trình bày khoảng **30–60 giây**. Lời nói gợi ý được
sinh tại `output/presentation_speech.md`.

## Cách tạo PowerPoint

Từ thư mục gốc repository, chạy:

```bash
python3 Scripts/generate_presentation.py
```

Kết quả local:

- `output/DATN_PC_Store_Gioi_Thieu.pptx`
- `output/presentation_speech.md`

Generator không cần package Python ngoài. File PPTX được dựng bằng OOXML và tự
kiểm tra số slide, tọa độ vùng an toàn, tính hợp lệ của ZIP/XML và số phần lời
thuyết trình.

## Hoàn thiện trước khi bảo vệ

- Điền họ tên sinh viên, lớp và khoa ở slide 1.
- Chạy website và chụp ảnh thật cho slide 7, 8 và 9.
- Thay khung nét đứt trong PowerPoint bằng ảnh chụp đúng URL ghi trên khung.
- Giữ ảnh theo tỷ lệ khung, dùng crop thay vì kéo méo ảnh.

`output/*.pptx` được bỏ qua bởi Git, vì vậy cần chạy generator để tạo file trên
máy trình bày.
