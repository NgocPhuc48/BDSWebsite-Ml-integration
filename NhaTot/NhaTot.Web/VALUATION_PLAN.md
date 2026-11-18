# Kế hoạch và triển khai Định giá v1

## Mục tiêu
Thay công thức giá, độ tin cậy và tăng trưởng gán sẵn bằng một pipeline có thể huấn luyện, đánh giá, lưu phiên bản và chạy dự đoán trong ASP.NET Core. Nhãn là **giá rao bán**, không phải giá giao dịch hay thẩm định pháp lý.

## Kế hoạch đã triển khai
1. Dữ liệu: TinDang APPROVED + BAN và DuLieuThuThap APPROVED + OFFER + BAN, có giá/diện tích/loại BĐS/phường/huyện/thời gian. Không dùng lịch sử dự đoán làm nhãn. Loại ngày tương lai, giá/diện tích không hợp lệ và trùng tài sản/nội dung/giá-diện tích-địa phương.
2. Mô hình: Ridge hồi quy log giá, log diện tích và bình phương log diện tích, thời gian, one-hot loại BĐS/huyện/phường. L2=1 cố định, không điều chỉnh siêu tham số bằng test. Cài bằng C#, không cần Python/server ML riêng.
3. Đánh giá: ít nhất 60 mẫu; chia theo ngày khoảng 60% train, 20% calibration, 20% test (tối thiểu 30/10/10). Cùng ngày không nằm ở nhiều tập. Từ điển category và hệ số chỉ học trên train. So MAE với baseline trung vị đơn giá/m² train; báo MAE, RMSE, R² và MAPE trên test.
4. Kích hoạt: chỉ chấp nhận MAE tốt hơn baseline và MAPE ≤35%. Ngưỡng này là kiểm soát kỹ thuật ban đầu, không đảm bảo chất lượng thị trường; cần thêm dữ liệu ngoài mẫu theo khu vực. Ghi phiên bản/metrics vào PhienBanMoHinh và KetQuaDanhGiaMoHinh. Artifact JSON lưu ngoài wwwroot trong App_Data/ValuationModels.
5. Tích hợp: DinhGiaController gọi đúng AiDinhGiaService, không còn hai công thức tách rời. Lưu ID model và kết quả vào lịch sử, bỏ phần độ tin cậy gán sẵn. Không hỗ trợ loại/khu vực hoặc diện tích quá xa tập train; dữ liệu gần nhất quá 180 ngày không dùng model.
6. Dự phòng: nếu chưa có model đủ chất lượng, cần ít nhất 3 tin cùng loại/phường, diện tích từ 0,5–2 lần diện tích yêu cầu trong 12 tháng. Dùng trung vị đơn giá và hiển thị dải giá quan sát; thiếu mẫu thì trả thông báo chưa đủ dữ liệu, không giả lập giá.
7. Dự báo: chỉ hiện khi phần train có ít nhất 12 tháng và hệ số thời gian không quá 3%/tháng. Đây là ngoại suy theo đặc trưng thời gian của mô hình giá rao, chưa phải một mô hình chuỗi thời gian kiểm định riêng cho chân trời 6 tháng. Nếu không đủ thì không vẽ đường tăng trưởng. Khoảng giá từ phân vị sai số calibration là tham khảo, không cam kết xác suất theo từng tài sản.
8. Vận hành: Admin/MlOps có nút huấn luyện và kích hoạt nếu đạt. Worker kiểm tra khi app chạy và mỗi 24 giờ, huấn luyện lại khi fingerprint dữ liệu thay đổi và đủ mẫu; không tự cào hay dùng dữ liệu tổng hợp để kích hoạt.

## Giới hạn cần giữ rõ
- v1 chưa học tác động đường, mặt tiền, hướng, pháp lý, số phòng, nội thất; trang kết quả nêu rõ. Cần đủ dữ liệu đặc điểm trước khi mở rộng feature.
- Giá rao có thiên lệch nguồn và chưa phản ánh giao dịch thực. Dữ liệu nhiều tin nhưng ít tài sản độc lập không thay thế độ phủ thị trường.
- Các tập test/calibration hiện còn nhỏ; không coi tỷ lệ coverage calibration là xác suất đảm bảo. Mô hình dự báo 6 tháng chuyên biệt cần nhiều tháng dữ liệu và backtest riêng từng horizon.
- Artifact và khóa cấu hình nằm trong App_Data đã bị loại khỏi Git/publish. Khi triển khai cần giữ/copy model cùng database registry. Thiếu artifact sẽ chuyển về so sánh dữ liệu, không đọc đường dẫn tùy ý.
- Mô hình đã huấn luyện không tự xóa ảnh hưởng của dữ liệu bị thu hồi duyệt; cần huấn luyện lại sau khi sửa dữ liệu. Dữ liệu thật ít thì kết quả là chưa đủ mẫu, không hạ điều kiện kích hoạt.

## Kiểm tra và chạy
- Thuật toán: dotnet run --project tests/ValuationChecks/ValuationChecks.csproj
- Database thật/huấn luyện: dotnet run --project tests/SetupChecks/SetupChecks.csproj -- --train
- UI: Admin → MlOps → Huấn luyện và áp dụng nếu đạt chất lượng; sau đó mở /DinhGia.
- Kiểm tra synthetic chỉ kiểm tra hồi quy, baseline, rò rỉ theo thời gian, loại trùng, dữ liệu không hợp lệ, ngoài miền và JSON roundtrip; không ghi dữ liệu giả vào database.

## Kết quả trên database cục bộ ngày 18/11/2026
Có 3 mẫu hợp lệ sau làm sạch. Chưa đủ 60 mẫu để huấn luyện/đánh giá độc lập; không kích hoạt mô hình giả. Pipeline và luồng suy luận đã tích hợp. Cần tiếp tục thu thập/kiểm duyệt dữ liệu thực trải theo thời gian; worker/nút huấn luyện dùng lại pipeline này khi đủ dữ liệu.

Tham khảo phương pháp: https://scikit-learn.org/stable/modules/linear_model.html#ridge-regression-and-classification ; https://scikit-learn.org/stable/modules/cross_validation.html#time-series-split
