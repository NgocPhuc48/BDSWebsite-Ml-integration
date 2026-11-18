# Nhà Tốt — Website bất động sản tích hợp ML

Ứng dụng ASP.NET Core MVC phục vụ đăng tin, tìm bất động sản, theo dõi Bẫy giá, thu thập dữ liệu Facebook và ước tính giá rao. Nhánh phát triển website: `web`.

## Công nghệ

- .NET 8, ASP.NET Core MVC/Razor, Entity Framework Core và SQL Server.
- Bootstrap 5, Chart.js, Leaflet.
- Apify cho thu thập bài Facebook công khai.
- Pipeline hồi quy Ridge chạy bằng C#; không cần server Python riêng.
- `tool-cao-data`: công cụ React/Vite độc lập ban đầu, không bắt buộc để chạy website.

## Chức năng

| Khu vực | Chức năng |
| --- | --- |
| Người dùng | Đăng ký/đăng nhập, đăng và quản lý tin, lưu tin, ví điểm, lịch hẹn |
| Bẫy giá | Tạo yêu cầu theo địa phương, loại BĐS, ngân sách và tiêu chí; mỗi yêu cầu một dòng, xem số tin khớp/gợi ý và mở chi tiết |
| Định giá | Ước tính giá rao từ mô hình đạt chất lượng hoặc tin so sánh; báo thiếu dữ liệu khi chưa đủ cơ sở |
| Admin | Quản lý danh mục, tài khoản, ví, tin đăng, báo cáo, tư vấn và mục tiêu Bẫy giá |
| SocialBot | Kết nối Apify, quản lý nguồn, cào thủ công/theo lịch, lưu/chống trùng/bóc tách, chỉnh sửa và kiểm duyệt |
| MLOps | Huấn luyện, lưu phiên bản, đánh giá MAE/RMSE/R²/MAPE và kích hoạt mô hình đạt điều kiện |

## Chạy ứng dụng

### 1. Chuẩn bị

- Cài .NET 8 SDK và SQL Server.
- Tạo/khôi phục database NhaTot theo schema gốc của dự án. Ứng dụng **không tự tạo toàn bộ database gốc**.
- Mở terminal tại thư mục chứa `NhaTot.sln`.

### 2. Cấu hình database

Sao chép file mẫu và sửa chuỗi kết nối cho máy của bạn:

```powershell
Copy-Item NhaTot.Web/appsettings.example.json NhaTot.Web/appsettings.json
```

File mẫu dùng Windows Authentication trên SQL Server cục bộ. Không commit mật khẩu SQL hoặc file cấu hình riêng. Có thể ghi đè chuỗi kết nối bằng biến môi trường `ConnectionStrings__DefaultConnection`.

### 3. Build và chạy

```powershell
dotnet restore NhaTot.sln
dotnet build NhaTot.sln --no-restore
dotnet run --project NhaTot.Web --launch-profile https
```

Địa chỉ phát triển: `https://localhost:7153` hoặc `http://localhost:5000`.

Khi khởi động, ứng dụng tự cập nhật schema SocialBot theo các SQL nhúng 003/005/006/007, có khóa và bảng ghi phiên bản. Không cần chạy từng file SQL thủ công. Tài khoản SQL cần quyền thêm bảng/cột và cập nhật dữ liệu cần thiết. Lịch cào mới mặc định tắt.

Không có mật khẩu đăng nhập mặc định được công bố trong repo. Tạo tài khoản và quản lý vai trò bằng quy trình quản trị của môi trường triển khai.

## Thu thập dữ liệu và Bẫy giá

1. Vào **Admin → SocialBot → Kết nối Apify**, nhập token rồi kiểm tra/lưu.
2. Thêm URL nhóm/page Facebook công khai chuyên BĐS, bật nguồn.
3. Cào thử, xem kết quả và chọn **Lưu vào database**; có thể xuất CSV.
4. Mở **Dữ liệu đã thu thập**, kiểm tra giá tổng, diện tích, loại BĐS, địa phương và loại giao dịch trước khi duyệt.
5. Bật **Lịch cào tự động** nếu cần. Ứng dụng phải tiếp tục chạy để lịch hoạt động.

Chỉ bài chào bán/cho thuê đã duyệt được gợi ý cho yêu cầu Bẫy giá tương đồng. Tin đăng Nhà Tốt đã duyệt, còn hạn cũng được gợi ý. Gợi ý tương đồng được phân biệt với kết quả khớp dùng cho đối soát cọc.

Token lưu bằng ASP.NET Core Data Protection, không hiển thị lại trên trang. Giữ thư mục `NhaTot.Web/App_Data` qua các lần triển khai để bảo toàn khóa và artifact; không đưa thư mục này lên Git. Trên Windows, khóa được bảo vệ bằng DPAPI theo tài khoản chạy ứng dụng.

Chi tiết: [Hướng dẫn SocialBot](tool-cao-data/INTEGRATION.md).

## Huấn luyện và định giá

- Dữ liệu: tin bán đã duyệt và bài cào đã kiểm duyệt; loại bản trùng và dữ liệu thiếu/không hợp lệ.
- Tối thiểu 60 mẫu độc lập, đủ ngày để tách train/calibration/test theo thời gian; không dùng dữ liệu tổng hợp để triển khai model thật.
- Đặc trưng v1: diện tích, loại BĐS, huyện/phường và thời gian. Chưa học ảnh hưởng pháp lý, đường, mặt tiền, số phòng hay nội thất.
- Chỉ tự kích hoạt khi MAE tốt hơn baseline so sánh và MAPE ≤35%. Đây là ngưỡng kỹ thuật ban đầu, không bảo đảm chất lượng thị trường.
- Huấn luyện tại **Admin → MlOps**; worker kiểm tra dữ liệu mới khi app chạy và mỗi 24 giờ.
- Thiếu mô hình phù hợp: dùng ít nhất 3 tin so sánh trong phường/xã; thiếu cả dữ liệu so sánh thì báo chưa đủ dữ liệu, không tạo giá giả.
- Dự báo 6 tháng chỉ hiện khi đủ lịch sử và điều kiện mô hình; là kịch bản ngoại suy giá rao, chưa phải dự báo giao dịch được kiểm định riêng theo từng chân trời.

Kiểm tra database cục bộ ngày 18/11/2026 mới có 3 mẫu hợp lệ, nên chưa kích hoạt mô hình. Số liệu này thuộc môi trường phát triển, không phải dữ liệu phân phối kèm repo.

Chi tiết: [Kế hoạch mô hình Định giá](NhaTot.Web/VALUATION_PLAN.md).

## Kiểm tra

Chạy từ thư mục chứa solution:

```powershell
dotnet run --project tests/ExtractionChecks
dotnet run --project tests/ValuationChecks
dotnet run --project tests/BayGiaBindingChecks
dotnet run --project tests/SocialBotViewChecks
dotnet run --project tests/SetupChecks
```

`SetupChecks -- --database` kiểm tra cập nhật schema trên database cấu hình cục bộ. `SetupChecks -- --train` đọc dữ liệu thật và thử huấn luyện; có thể ghi artifact/registry và kích hoạt nếu đạt. Chỉ dùng trên môi trường bạn muốn kiểm tra.

Nếu app đang chạy làm khóa file `.exe`, dừng app trước khi build lại. Sau khi cập nhật JavaScript, tải lại trang bằng Ctrl+F5.

## Cấu trúc

```text
NhaTot.Domain/          Entity và DbContext
NhaTot.Web/             MVC, Admin/Consultant, service, giao diện và static assets
tests/                 Kiểm tra dữ liệu, ML, binding và Razor
tool-cao-data/          Tool React cũ, SQL nâng cấp và tài liệu tích hợp
NhaTot.sln             Solution .NET
```

## Lưu ý triển khai

- Dùng HTTPS, bảo vệ kết nối SQL và quyền Admin; không dùng thông tin đăng nhập thử nghiệm trong môi trường công khai.
- Ví điểm hiện có luồng nạp nội bộ phục vụ ứng dụng; không coi là tích hợp cổng thanh toán thật.
- Chưa có thông báo đẩy hay cập nhật Bẫy giá thời gian thực; kết quả cập nhật khi tải trang.
- Chỉ thu thập nguồn có quyền truy cập/sử dụng phù hợp. Apify có thể tính phí theo tài khoản đã cấu hình.
