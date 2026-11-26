# Nhà Tốt — Website Bất Động Sản Tích Hợp ML & SocialBot

Ứng dụng ASP.NET Core MVC (.NET 8) phục vụ đăng tin, tìm kiếm bất động sản tại **Nghệ An & Hà Tĩnh**, theo dõi **Bẫy giá**, tích hợp trực tiếp **Tool Cào Dữ Liệu Facebook (Apify)** và định giá BĐS bằng Machine Learning. Mã nguồn chính nằm trong thư mục `NhaTot/`.

## Công nghệ sử dụng

- **.NET 8**, ASP.NET Core MVC / Razor Views, Entity Framework Core (MySQL `Pomelo.EntityFrameworkCore.MySql` & SQL Server).
- **Bootstrap 5**, Chart.js, Leaflet, Google Maps Geocoding API (kèm fallback OpenStreetMap Nominatim).
- **Apify Actors** tích hợp trực tiếp trong Admin SocialBot:
  - `calm_builder/facebook-groups-scraper`: tự tìm Group công khai theo chủ đề và lấy bài viết + bình luận (không cần tài khoản Facebook).
  - `alien_force/facebook-scraper-pro-rental`: tìm kiếm bài viết Facebook theo từ khóa và thời gian gần nhất.
- **ML Pipeline**: hồi quy Ridge (`RidgeLogPrice-v1`) viết thuần C#, huấn luyện và đánh giá trực tiếp trong ứng dụng.
- **`NhaTot/ToolCaoData`**: bản Blazor Server độc lập của công cụ cào dữ liệu (tùy chọn).

## Chức năng chính

| Phân hệ | Mô tả chức năng |
| --- | --- |
| **Người dùng** | Đăng ký/đăng nhập, đăng và quản lý tin BĐS, lưu tin yêu thích, ví điểm, đặt lịch hẹn tư vấn |
| **Bẫy giá** | Đặt mục tiêu săn BĐS theo khu vực (Nghệ An / Hà Tĩnh), loại BĐS (`Đất nền`, `Nhà`, `Căn hộ`, `Chung cư`, `Biệt thự`), ngân sách và diện tích; gợi ý tin khớp từ cả tin nội bộ lẫn bài Facebook đã duyệt |
| **Định giá AI** | Ước tính giá rao từ mô hình ML đạt chuẩn hoặc từ tập tin so sánh cùng khu vực |
| **Tool Cào Dữ Liệu Facebook** | Tích hợp ngay tại nút **Mở Tool Cào Dữ Liệu** (`/Admin/SocialBot/ToolCaoDuLieu`) với 2 chế độ **Tìm Group & Bài viết** và **Tìm theo từ khóa** cho cả nhóm **Mua bán** và **Cho thuê** tại Nghệ An / Hà Tĩnh |
| **Tiền xử lý & Kiểm duyệt** | Tự động lọc bỏ bài tin tức/hình sự/lừa đảo/hỏi đáp, xóa icon/emoji rác, bóc tách địa chỉ, SĐT, diện tích, quy đổi khoảng giá viết tắt (`1 tỷ 7xx` → `GiaMinVND = 1,701,000,000`, `GiaMaxVND = 1,799,000,000`) tách biệt với giá cụ thể (`GiaRaoVND`), lưu nóng vào CSDL và kiểm duyệt |
| **MLOps** | Huấn luyện, quản lý phiên bản mô hình, đánh giá MAE / RMSE / R² / MAPE và tự động kích hoạt khi đạt ngưỡng |

## Hướng dẫn cài đặt & chạy ứng dụng

### 1. Chuẩn bị cơ sở dữ liệu

- Cài đặt **.NET 8 SDK** và **MySQL 8.0+** (hoặc SQL Server; xem lược đồ đầy đủ tại `CSDL.txt` ở thư mục gốc repo).
- Cấu hình chuỗi kết nối trong `NhaTot/NhaTot.Web/appsettings.json` (hoặc biến môi trường `ConnectionStrings__DefaultConnection`).
- *(Tùy chọn)* Điền khóa Google Maps vào `"GoogleMaps": { "ApiKey": "..." }` trong `appsettings.json` để tra cứu tỉnh từ địa chỉ (nếu để trống hệ thống tự dùng OpenStreetMap Nominatim).

### 2. Build và chạy Website

Mở terminal tại thư mục `NhaTot/`:

```powershell
dotnet restore NhaTot.sln
dotnet build NhaTot.sln --no-restore
dotnet run --project NhaTot.Web/NhaTot.Web.csproj
```

- Địa chỉ mặc định: `http://localhost:5000` (hoặc `https://localhost:7153`).
- Khi khởi động, `SocialBotSchemaInitializer` tự động đồng bộ các bảng/cột mới (`GiaRaoVND`, `GiaMinVND`, `GiaMaxVND`, `LichCaoDuLieu`, chuẩn hóa 5 danh mục `LoaiBDS`: `Đất nền`, `Nhà`, `Căn hộ`, `Chung cư`, `Biệt thự`).

## Quy trình sử dụng Tool Cào Dữ Liệu Facebook

1. Vào **Admin → SocialBot → Kết nối Apify**, nhập Apify API Token (được mã hóa an toàn bằng ASP.NET Core Data Protection trong `App_Data`).
2. Bấm **Mở Tool Cào Dữ Liệu** (`/Admin/SocialBot/ToolCaoDuLieu`):
   - **Tab Tìm Group & Bài viết**: chọn khu vực (`Nghệ An` / `Hà Tĩnh`), số Group, số thành viên tối thiểu, thời gian bài viết, số bài và bấm 1 trong 6 chủ đề (`mua bán nhà`, `mua bán đất`, `mua bất động sản`, `thuê nhà`, `thuê đất`, `thuê bất động sản`).
   - **Tab Tìm theo từ khóa**: quét trực tiếp bài đăng công khai theo từ khóa và khoảng thời gian gần nhất.
3. Các bài hợp lệ (đúng nhu cầu mua bán/cho thuê BĐS, có giá và địa chỉ tại Nghệ An / Hà Tĩnh) sẽ được **tự động lưu nóng** vào bảng `DuLieuThuThap` (hoặc xuất file CSV).
4. Vào **Dữ liệu đã thu thập** (`/Admin/SocialBot/DuLieu`) để xem bảng đã tiền xử lý (Loại BĐS, Khoảng giá / Giá, Diện tích, Địa chỉ sạch không icon, Tỉnh, Số điện thoại, Link bài viết gốc), xóa từng bài/xóa toàn bộ (tự reset bộ đếm về `#1`) hoặc bấm **Sửa / Kiểm duyệt** để duyệt đưa vào gợi ý **Bẫy giá**.

Chi tiết dự án công cụ độc lập: [Hướng dẫn ToolCaoData](NhaTot/ToolCaoData/README.md) · [Kế hoạch mô hình Định giá](NhaTot/NhaTot.Web/VALUATION_PLAN.md).

## Chạy bộ kiểm thử

Từ thư mục `NhaTot/`:

```powershell
dotnet run --project tests/ExtractionChecks
dotnet run --project tests/ValuationChecks
dotnet run --project tests/BayGiaBindingChecks
dotnet run --project tests/SocialBotViewChecks
dotnet run --project tests/SetupChecks
```

## Cấu trúc thư mục

```text
CSDL.txt                Lược đồ cơ sở dữ liệu & dữ liệu khởi tạo chuẩn (Nghệ An & Hà Tĩnh)
NhaTot/
├── NhaTot.Domain/      Entities (DuLieuThuThap, LoaiBd, TinDang...) và NhaTotDbContext
├── NhaTot.Web/         Ứng dụng MVC chính, Areas/Admin/SocialBot, Services ETL/Apify/ML
├── ToolCaoData/        Ứng dụng Blazor Server độc lập (tùy chọn)
├── tests/              Các project kiểm tra bóc tách, định giá, binding và schema
└── NhaTot.sln          Solution .NET 8
```
