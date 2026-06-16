# Hướng dẫn setup project DATN_PCStore trên máy mới

> Bối cảnh: bạn vừa clone project từ GitHub về máy mới và cần chạy được full hệ thống.

## 0) Xác định stack thực tế của dự án

Dự án này là **ASP.NET Core MVC (.NET 8)**, dùng:
- Entity Framework Core 8 + SQL Server.
- Razor Views (server-rendered), **không có frontend Node.js tách riêng**.
- Xác thực bằng **Cookie Authentication** (không dùng JWT).
- Có seed dữ liệu mẫu khi app khởi động.

---

## 1) Cần cài những phần mềm gì trước?

## Bắt buộc

1. **Git** (để clone/pull code)
   - Windows: cài Git for Windows.
   - Kiểm tra:
     ```bash
     git --version
     ```

2. **.NET SDK 8.0**
   - Dự án target `net8.0`, nên bắt buộc cài .NET 8 SDK.
   - Kiểm tra:
     ```bash
     dotnet --info
     dotnet --list-sdks
     ```

3. **SQL Server**
   - Dự án cấu hình provider `UseSqlServer(...)`, vì vậy cần SQL Server.
   - Mặc định repo dùng **SQL Server LocalDB** để không phụ thuộc tên máy/instance riêng.
   - Có thể dùng:
     - SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, khuyến nghị cho máy dev Windows)
     - SQL Server Express
     - SQL Server Developer
   - Nên cài thêm **SQL Server Management Studio (SSMS)** để thao tác DB.
   - Nếu máy chưa có LocalDB hoặc LocalDB chưa chạy, kiểm tra và khởi động bằng:
     ```powershell
     sqllocaldb info
     sqllocaldb start MSSQLLocalDB
     ```

## Khuyến nghị

4. **IDE / Editor**
   - Khuyến nghị: **Visual Studio 2022** (workload ASP.NET and web development).
   - Hoặc: VS Code + C# extension.

## Không bắt buộc trong dự án này

5. **Node.js / npm / yarn / pnpm**
   - Không có `package.json`, không có frontend build bằng Node.
   - => **Không cần cài** để chạy dự án hiện tại.

6. **Java/JDK**
   - Không có thành phần Java.
   - => **Không cần cài**.

7. **Docker**
   - Repo chưa có Dockerfile/docker-compose.
   - => **Không bắt buộc**.

---

## 2) Cách clone/pull project về máy

```bash
git clone <URL_REPO_GITHUB>
cd DATN
```

Nếu đã clone từ trước và muốn cập nhật:
```bash
git pull origin <branch-name>
```

---

## 3) Cách cài dependencies cho frontend và backend

## Backend (.NET)

Dự án dùng NuGet, chỉ cần restore:

```bash
dotnet restore
```

## Frontend

Không có frontend tách riêng (React/Vue/Angular), nên **không có bước `npm install`**.

---

## 4) File môi trường (.env) cần tạo như thế nào?

Dự án này đọc config mặc định từ `appsettings.json` (và có thể override bằng `appsettings.Development.json` hoặc biến môi trường).

## Cách 1 (nhanh nhất): sửa trực tiếp connection string trong `appsettings.json`

Mẫu:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Cách 2 (khuyến nghị): dùng biến môi trường (không commit secret)

Biến bắt buộc:
- `ConnectionStrings__DefaultConnection`

Ví dụ PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\\MSSQLLocalDB;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;"
```

Ví dụ CMD:

```cmd
set ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;
```

> Lưu ý: repo hiện chưa có cơ chế đọc file `.env` riêng. Nếu muốn dùng `.env`, bạn phải tự tích hợp thêm thư viện/loader.

---

## 5) Cách setup database

## Tạo database

Bạn có 2 lựa chọn:

### Lựa chọn A: để ứng dụng tự tạo DB (đang bật sẵn)
- Ứng dụng gọi `db.Database.EnsureCreated()` khi startup.
- Chỉ cần bảo đảm SQL Server chạy + connection string đúng.

### Lựa chọn B: tạo DB bằng EF Core migration

1. Cài tool EF (nếu máy chưa có):
   ```bash
   dotnet tool install --global dotnet-ef
   ```
2. Tạo/cập nhật schema:
   ```bash
   dotnet ef database update
   ```

## Import schema

- Không có file SQL dump riêng trong repo.
- Schema được quản lý bằng thư mục `Migrations/`.

## Migration

- Repo đã có migration sẵn.
- Cập nhật DB bằng:
  ```bash
  dotnet ef database update
  ```

## Seed dữ liệu mẫu

- Khi app chạy, `SeedData.InitializeAsync(db)` sẽ tự thêm:
  - Roles
  - Admin mặc định
  - Category, Product, Banner, SiteSettings, Article

---

## 6) Cách chạy frontend

Vì là Razor MVC, frontend chạy cùng backend.

Chạy app:

```bash
dotnet run
```

Mở trình duyệt theo URL được in ra terminal (thường là `http://localhost:xxxx` hoặc `https://localhost:xxxx`).

---

## 7) Cách chạy backend

Tương tự frontend (cùng process):

```bash
dotnet run
```

API/Controller sẽ phục vụ trực tiếp từ cùng host.

---

## 8) Port nào dùng cho frontend/backend/database

- **Frontend + Backend**: cùng 1 app ASP.NET Core, port do Kestrel/profile launch quyết định khi chạy `dotnet run`.
  - Nếu muốn cố định port:
    ```bash
    dotnet run --urls "http://localhost:5000"
    ```
- **Database (SQL Server)**:
  - SQL Server mặc định thường dùng `1433` (instance default),
  - SQLExpress có thể dùng dynamic port theo instance.

---

## 9) Cách đăng nhập tài khoản test

Tài khoản admin được seed sẵn:

- **Email:** `admin@pcstore.local`
- **Password:** `123456`

Các bước:
1. Chạy app.
2. Truy cập `/Account/Login`.
3. Đăng nhập bằng tài khoản trên.

---

## 10) Nếu project dùng JWT/token/auth thì giải thích cách hoạt động

Project **không dùng JWT**. Cơ chế thực tế:

1. User login tại `AccountController.Login`.
2. Hệ thống verify password hash SHA256 + salt tĩnh trong `AuthService`.
3. Tạo `ClaimsIdentity` + `ClaimsPrincipal`.
4. Gọi `HttpContext.SignInAsync(...)` với scheme cookie `PcStoreCookie`.
5. Cookie auth được browser lưu và gửi lại mỗi request.
6. Middleware `UseAuthentication()` + `UseAuthorization()` xử lý xác thực/phân quyền.

---

## 11) Lỗi thường gặp lần đầu và cách fix

1. **Không kết nối được SQL Server**
   - Kiểm tra service SQL Server đã chạy.
   - Kiểm tra đúng server/instance trong `DefaultConnection` (mặc định dùng `(localdb)\MSSQLLocalDB`).
   - Nếu dùng LocalDB trên Windows, kiểm tra và khởi động:
     ```powershell
     sqllocaldb info
     sqllocaldb start MSSQLLocalDB
     ```
   - Nếu dùng cert local, giữ `TrustServerCertificate=True`.

2. **Login fail dù đúng mật khẩu seed**
   - Có thể DB cũ chứa dữ liệu khác.
   - Xoá DB `DATN_PCStore` rồi chạy lại để seed mới.

3. **`dotnet` command not found**
   - Chưa cài .NET SDK 8 hoặc PATH chưa nhận.
   - Mở terminal mới sau khi cài.

4. **Lỗi EF tool `dotnet ef` không tồn tại**
   - Cài: `dotnet tool install --global dotnet-ef`

5. **Port bị chiếm**
   - Chạy với port khác:
     ```bash
     dotnet run --urls "http://localhost:5001"
     ```

---

## 12) Thứ tự chuẩn để chạy toàn bộ hệ thống trên máy mới

1. Cài Git, .NET 8 SDK, SQL Server.
2. Clone repo.
3. Sửa connection string đúng máy mới.
4. `dotnet restore`.
5. (Tuỳ chọn) `dotnet ef database update`.
6. `dotnet run`.
7. Mở URL app và login tài khoản admin seed sẵn.

---

## 13) Checklist nhanh setup trong 5 phút

- [ ] Cài Git
- [ ] Cài .NET SDK 8
- [ ] Cài và bật SQL Server
- [ ] Clone repo
- [ ] Cập nhật `ConnectionStrings:DefaultConnection` nếu không dùng LocalDB mặc định
- [ ] Chạy `dotnet restore`
- [ ] Chạy `dotnet run`
- [ ] Đăng nhập `admin@pcstore.local / 123456`

---

## Full commands (copy chạy luôn)

> Ví dụ cho máy Windows + SQLExpress local.

```bash
git clone <URL_REPO_GITHUB>
cd DATN

# kiểm tra dotnet
dotnet --info

# restore package
dotnet restore

# (tuỳ chọn) cài EF tool
dotnet tool install --global dotnet-ef

# (tuỳ chọn) apply migration
dotnet ef database update

# chạy app
dotnet run
```

Nếu cần set nhanh connection string bằng biến môi trường trước khi chạy:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\\MSSQLLocalDB;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run
```
