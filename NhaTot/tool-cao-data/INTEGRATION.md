# SocialBot – hướng dẫn tích hợp

## Bắt đầu

1. Khởi động NhaTot.Web với kết nối database hiện có. Ứng dụng tự cập nhật schema SocialBot trước khi phục vụ request; không cần chạy SQL thủ công.
2. Vào **Admin → SocialBot → Kết nối Apify**, nhập API token rồi chọn **Kiểm tra và lưu kết nối**. Bước kiểm tra chỉ đọc thông tin tài khoản, không chạy cào. Không cần biến môi trường hay khởi động lại ứng dụng.
3. Thêm URL HTTPS nhóm/page Facebook công khai chuyên bất động sản, bật nguồn rồi cào thử.
4. Sau khi hoàn tất, chọn **Lưu vào database**, vào **Dữ liệu đã thu thập → Sửa / Kiểm duyệt** để kiểm tra và duyệt bài.
5. Nếu muốn tự động, vào **Lịch cào tự động**, chọn chu kỳ và bật lịch. Phiên tự động tự lưu/bóc tách nhưng vẫn chờ kiểm duyệt.

## Kết nối và khóa mã hóa

- Token được kiểm tra qua Apify `/v2/users/me`, mã hóa bằng ASP.NET Core Data Protection rồi lưu vào database. Không trả token về HTML, không ghi token vào source hoặc đưa vào URL request.
- Token mới chỉ thay thế token cũ sau khi kiểm tra thành công. Token đã cấu hình bằng `Apify__Token` vẫn là phương án dự phòng khi chưa lưu token qua trang Admin; token lưu trong Admin được ưu tiên.
- Khóa lưu ngoài wwwroot tại `NhaTot.Web/App_Data/DataProtectionKeys`, được loại khỏi Git và gói publish. Trên Windows, khóa còn được bảo vệ bằng DPAPI theo tài khoản chạy ứng dụng.
- Khi triển khai phải giữ thư mục khóa qua các lần cập nhật, cấp quyền chỉ cho tài khoản chạy app và dùng HTTPS. Mất khóa hoặc đổi máy/tài khoản Windows có thể cần nhập lại token. Nhiều instance phải dùng chung key ring và cơ chế bảo vệ khóa phù hợp.

## Tự cập nhật database

- SQL 003/005/006/007 được nhúng vào assembly, chạy theo thứ tự và ghi vào `dbo.SocialBotSchemaVersions`. Cơ chế khóa SQL Server ngăn nhiều instance cùng cập nhật. Phiên bản đã áp dụng không chạy lại.
- Chỉ bổ sung các cột/bảng SocialBot và bổ sung tỉnh từ địa phương đã biết cho mục tiêu cũ; không xóa dữ liệu. Tương thích cả database đã chạy các script cũ thủ công.
- Cần database NhaTot gốc và tài khoản SQL có quyền thêm bảng/cột, cập nhật dữ liệu và gọi `sp_getapplock`. Không tự tạo toàn bộ database NhaTot. Cập nhật thất bại sẽ dừng startup để tránh chạy app với schema thiếu.
- Lịch mới mặc định tắt; nâng cấp giữ nguyên cấu hình lịch đang có. Không tự khởi tạo phiên Apify chỉ vì cập nhật schema.

## Thu thập và bóc tách

- Actor: `apisamurai~facebook-scraper`, tài liệu https://apify.com/apisamurai/facebook-scraper . Thu thập theo nguồn, không cần từ khóa; không quét toàn bộ Facebook.
- Mỗi phiên lấy 1–10 nguồn ACTIVE, tối đa 1–100 bài mỗi nguồn và thời gian đăng 1/7/30 ngày. Bảng kết quả tối đa 1.000 bản ghi, có xuất CSV.
- Lưu ID, URL, tiêu đề, nội dung đầy đủ, JSON gốc, thời gian UTC, SHA-256; chống trùng theo ID/URL/hash. Bài thiếu nội dung, định danh hoặc nguồn bị bỏ qua. Cả bài và nhật ký được lưu trong transaction.
- Quy tắc bóc tách nhận giá rao tổng, diện tích, loại BĐS, tỉnh và phường/xã theo danh mục. Không đoán giá thuê/đơn giá m² thành tổng giá; trường mơ hồ để trống. Không phân loại ngữ nghĩa bài lạc chủ đề.
- `EXTRACTED` và `NEEDS_REVIEW` đều chờ Admin kiểm tra. Trang dữ liệu hiển thị 100 bài mới nhất và xử lý tối đa 100 bài NEW cũ mỗi lần. Không bóc tách bài cũ chỉ còn tiêu đề bị cắt ngắn.

## Kiểm duyệt và Bẫy giá

- Admin chỉnh tiêu đề, giá, diện tích, chọn loại BĐS/phường/xã, phân biệt chào bán và nhu cầu, xác định mua bán/cho thuê. Giữ nguyên nội dung/JSON gốc; ghi nhật ký trước/sau.
- Chỉ bài APPROVED + OFFER được gợi ý dưới mục tiêu của chính người đăng nhập; lưu nháp hoặc từ chối sẽ gỡ bài khi người dùng tải lại.
- Gợi ý gồm MXH đã duyệt và tin Nhà Tốt APPROVED còn hạn, cùng loại BĐS/giao dịch/khu vực; giá từ ngân sách tối thiểu đến 110% tối đa, diện tích đạt yêu cầu. Tin thiếu giá/diện tích không được coi là đạt.
- Tối đa 25 gợi ý mỗi nguồn, mới nhất trước. Tin hệ thống đã có trong kết quả khớp không lặp lại ở gợi ý. Hướng/pháp lý/phòng ngủ và tiêu chí chi tiết khác cần xác minh; gợi ý không tính vào khớp lệnh/hoàn cọc.
- Mục tiêu cũ thiếu mọi định danh địa phương không tự đoán tỉnh. Chưa có thông báo đẩy hoặc cập nhật thời gian thực.

## Lịch tự động

- Worker kiểm tra mỗi 30 giây khi app đang chạy; chu kỳ 15–1440 phút tính từ khi phiên trước hoàn tất. Không chạy bù hàng loạt.
- ID phiên và nguồn lưu database để tiếp tục theo dõi sau khởi động lại. Khóa database tránh các worker tạo phiên lịch đồng thời; cào thủ công là luồng độc lập.
- Phiên quá 20 phút được yêu cầu abort. Nếu không xác định được kết quả tạo phiên, lịch tự tắt START_UNKNOWN; kiểm tra Apify Console trước khi xác nhận bật lại.
- Tắt lịch ngăn tạo phiên mới; phiên hiện tại vẫn được theo dõi và lưu. App phải luôn chạy, cấu hình host tránh idle nếu cần.

## Kiểm tra

- `dotnet build NhaTot.Web/NhaTot.Web.csproj --no-restore`
- `dotnet run --project tests/ExtractionChecks/ExtractionChecks.csproj`
- `dotnet run --project tests/SetupChecks/SetupChecks.csproj` kiểm tra SQL nhúng và mã hóa. Thêm `-- --database` để thực thi cập nhật schema hai lần và kiểm tra khả năng chạy lại trên database cấu hình cục bộ, không chạy actor.
