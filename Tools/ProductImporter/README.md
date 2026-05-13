# Product Importer cho Build PC

Tool chạy thủ công để nhập linh kiện vào DB nội bộ, không chạy realtime trên website.

## Cách chạy

```bash
cd Tools/ProductImporter
dotnet run
```

## Luồng nguồn dữ liệu

1. **Ưu tiên Hura API** (nếu cấu hình `Importer:HuraApiEndpoint`).
2. Nếu API lỗi hoặc trả thông báo `You need to use Hura.Ajax.post/get` => **không bypass**, tự chuyển qua crawl HTML danh mục.
3. Nếu HTML không lấy được dữ liệu => fallback import từ `JSON/CSV`.

## Dữ liệu hỗ trợ

- Name, Price, ImageUrl, ProductUrl, Category, Brand, Warranty, Stock, Description.

## Upsert vào DB

- Tạo category nếu chưa tồn tại.
- Không xoá dữ liệu cũ.
- Nếu đã có sản phẩm theo `SourceUrl(ProductUrl)` hoặc `Name` => update giá / ảnh / tồn kho.
- Nếu chưa có => insert mới.
- Không insert trùng.

## Logging

Tool có log các thông tin:

- nguồn đang lấy
- category đang crawl
- số sản phẩm tìm được
- số insert
- số update
- lỗi nếu có
