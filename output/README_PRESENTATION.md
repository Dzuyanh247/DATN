# PowerPoint bảo vệ đồ án DATN PC Store

Bộ PowerPoint gồm **30 slide** theo phong cách đồ án tốt nghiệp CNTT hiện đại, được sinh trực tiếp từ source code hiện tại.

## Điểm chính của phiên bản thiết kế lại

- Nội dung đi theo hành trình: bài toán → kiến trúc → khám phá → tư vấn → giao dịch → hậu mãi → quản trị → đánh giá.
- Bao phủ các chức năng lớn có trong source: catalog, tài khoản/OTP, giỏ hàng guest/user, checkout GHN, COD/chuyển khoản QR, tracking, báo giá/Excel, Build PC/CSV, so sánh, SignalR chat, feedback, bảo hành, articles và toàn bộ các nhóm quản trị chính.
- Các slide demo dùng **khung giao diện vector bám theo View/Controller thật**, không còn placeholder trống lặp lại.
- Mỗi slide có một shape nền full-slide riêng, mang tên `BACKGROUND — replaceable full-slide layer` và nằm đầu z-order để có thể thay nền an toàn.
- Bố cục, decor và nhịp trình bày thay đổi giữa các slide nhưng dùng chung hệ màu navy–blue–cyan–purple–orange–green.

## Tạo PowerPoint

Từ thư mục gốc repository:

```bash
python3 Scripts/generate_presentation.py
```

Kết quả:

- `output/DATN_PC_Store_Gioi_Thieu.pptx` — file PowerPoint 16:9, 30 slide.
- `output/presentation_speech.md` — lời thuyết trình 40–50 giây/slide.
- `output/presentation_outline.md` — dàn ý đồng bộ với deck.
- `output/presentation_notes.md` — ghi chú thiết kế và vị trí giao diện.
- `output/presentation_script.md` — kịch bản rút gọn.
- `output/slide_image_placeholders.md` — hướng dẫn thay khung vector bằng screenshot thật khi có môi trường chạy ứng dụng.

Generator tự kiểm tra marker chức năng trong source, số slide, thứ tự slide, lớp background, biên canvas, ZIP/OOXML và số phần lời thuyết trình.

## Lưu ý

File `.pptx` được tạo local và nằm trong `.gitignore`; repository lưu generator cùng các tài liệu Markdown để có thể tái tạo deck nhất quán trên máy khác.
