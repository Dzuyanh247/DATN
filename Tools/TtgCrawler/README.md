# TTGShop BuildPC Crawler

Crawler thủ công để lấy dữ liệu linh kiện từ `https://ttgshop.vn/buildpc` (ưu tiên API JSON, fallback HTML), sau đó import vào SQL Server của project DATN.

## Chạy crawler

```bash
cd Tools/TtgCrawler
python crawl_ttgshop.py
```

## Cấu hình

Chỉnh file `appsettings.Crawler.json`:

- `ConnectionString`: SQL Server connection string.
- `BaseUrl`: mặc định `https://ttgshop.vn`.
- `DelayMs`: delay giữa request (khuyến nghị 1000-2000ms).
- `MaxPagesPerCategory`: giới hạn page/category.

## Lưu ý

- Crawler chỉ chạy thủ công, không gọi realtime từ controller.
- Nếu website trả về 403/429 crawler sẽ dừng ngay, không bypass.
- Upsert theo `Slug` (parse từ ProductUrl) hoặc `Name`, không xóa dữ liệu cũ.
