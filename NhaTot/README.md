# Nhà Tốt — Website Bất Động Sản Tích Hợp ML & SocialBot

Ứng dụng ASP.NET Core MVC (.NET 8) phục vụ đăng tin, tìm kiếm bất động sản tại **Nghệ An & Hà Tĩnh**, theo dõi **Bẫy giá**, tích hợp trực tiếp **Tool Cào Dữ Liệu Facebook (Apify)** và định giá BĐS bằng Machine Learning.

## Công nghệ sử dụng

- **.NET 8**, ASP.NET Core MVC / Razor Views, Entity Framework Core (MySQL `Pomelo.EntityFrameworkCore.MySql` & SQL Server).
- **Bootstrap 5**, Chart.js, Leaflet, Google Maps Geocoding API (kèm fallback OpenStreetMap Nominatim).
- **Apify Actors** tích hợp trực tiếp trong Admin SocialBot:
  - `calm_builder/facebook-groups-scraper`: tự tìm Group công khai theo chủ đề và lấy bài viết + bình luận.
  - `alien_force/facebook-scraper-pro-rental`: tìm kiếm bài viết Facebook theo từ khóa và thời gian gần nhất.
- **ML Pipeline**: hồi quy Ridge (`RidgeLogPrice-v1`) viết thuần C#.
- **`ToolCaoData/`**: bản Blazor Server độc lập (tùy chọn, xem [ToolCaoData/README.md](ToolCaoData/README.md)).

## Chạy nhanh ứng dụng

```powershell
dotnet restore NhaTot.sln
dotnet build NhaTot.sln --no-restore
dotnet run --project NhaTot.Web/NhaTot.Web.csproj
```

- Truy cập `http://localhost:5000` → đăng nhập Admin → **SocialBot** → **Mở Tool Cào Dữ Liệu** (`/Admin/SocialBot/ToolCaoDuLieu`).
- Khi khởi động, `SocialBotSchemaInitializer` tự động đồng bộ các cột `GiaRaoVND`, `GiaMinVND`, `GiaMaxVND` trong bảng `DuLieuThuThap` và 5 danh mục chuẩn `LoaiBDS` (`Đất nền`, `Nhà`, `Căn hộ`, `Chung cư`, `Biệt thự`).

## Kiểm thử

```powershell
dotnet run --project tests/ExtractionChecks
dotnet run --project tests/ValuationChecks
dotnet run --project tests/BayGiaBindingChecks
dotnet run --project tests/SocialBotViewChecks
dotnet run --project tests/SetupChecks
```
