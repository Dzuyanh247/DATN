# Hướng dẫn tạo PowerPoint DATN PC Store

File PowerPoint là sản phẩm sinh tự động nên không được lưu trong Git repository.
Repository chỉ lưu source generator và các tệp nội dung Markdown.

## Tệp đầu vào

Script sử dụng các tệp sau:

- `output/presentation_outline.md`: tiêu đề và nội dung bullet của 20 slide.
- `output/presentation_script.md`: lời thuyết trình cho từng slide.
- `output/slide_image_placeholders.md`: danh sách slide cần chèn ảnh chụp thật.

## Yêu cầu

- Python 3.10 trở lên.
- Khuyến nghị cài thư viện `python-pptx`:

```bash
python -m pip install python-pptx
```

Script có phương án OOXML dự phòng cho môi trường ngoại tuyến không cài được
`python-pptx`, nhưng nên sử dụng `python-pptx` trên máy local để có khả năng tương
thích tốt nhất với Microsoft PowerPoint.

## Tạo bài thuyết trình

Chạy lệnh sau từ thư mục gốc của repository:

```bash
python Scripts/generate_presentation.py
```

Sau khi chạy thành công, file PowerPoint sẽ được sinh tại:

```text
output/DATN_PC_Store_Gioi_Thieu.pptx
```

Script đồng thời cập nhật file lời thuyết trình:

```text
output/presentation_notes.md
```

## Chèn ảnh giao diện thật

Các slide cần ảnh đã có khung placeholder. Danh sách đầy đủ nằm trong:

```text
output/slide_image_placeholders.md
```

Mở file PowerPoint vừa tạo và thay các khung `[THÊM ẢNH ...]` bằng ảnh chụp giao
diện thực tế. Không cần sửa hoặc nhúng ảnh vào source generator.

## Lưu ý về Git

Các file `*.pptx` đã được khai báo trong `.gitignore`. Không dùng `git add -f` để
đưa file PowerPoint đã sinh vào repository.
