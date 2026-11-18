using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;
namespace NhaTot.Web.Services;

public sealed class AiDinhGiaService(NhaTotDbContext db, ValuationTrainingService training) : IAiDinhGiaService
{
    public async Task<decimal> TinhGiaGoiYAsync(int bdsId)
    {
        var bds = await db.BatDongSans.FindAsync((long)bdsId);
        if (bds?.MaPhuongXa == null || bds.MaLoaiBds == null) return 0;
        var r = (KetQuaDinhGiaViewModel)await DinhGiaBatDongSanAsync(new DinhGiaInputModel {
            MaPhuongXa = bds.MaPhuongXa.Value, MaLoaiBds = bds.MaLoaiBds.Value, DienTich = bds.DienTichDat ?? bds.DienTichSuDung ?? 0 });
        return r.Success ? r.GiaDuDoanVnd : 0;
    }
    public async Task<object> DinhGiaBatDongSanAsync(DinhGiaInputModel input)
    {
        var ward = await db.PhuongXas.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhuongXa == input.MaPhuongXa && x.TrangThai == true);
        if (ward?.MaQuanHuyen == null || input.DienTich is < 1 or > 100000
            || !await db.LoaiBds.AnyAsync(x => x.MaLoaiBds == input.MaLoaiBds && x.TrangThai == true))
            return new KetQuaDinhGiaViewModel { Message = "Loại BĐS, phường/xã hoặc diện tích không hợp lệ." };
        var now = DateTime.UtcNow;
        var query = new ValuationSample("input", input.MaLoaiBds, ward.MaQuanHuyen.Value, input.MaPhuongXa, (double)input.DienTich, 0, now);
        var active = await db.PhienBanMoHinhs.AsNoTracking().Where(x => x.TenMoHinh == ValuationTrainingService.ModelName && x.TrangThai == "ACTIVE")
            .OrderByDescending(x => x.NgayHuanLuyen).FirstOrDefaultAsync();
        var model = active == null ? null : await training.LoadAsync(active);
        var result = new KetQuaDinhGiaViewModel { ThoiGianThucHien = DateTime.Now };
        double price, low, high;
        if (model != null && model.Supports(query) && now - model.LastDate < TimeSpan.FromDays(180))
        {
            price = model.Predict(query); low = price * Math.Exp(-model.LogError90); high = price * Math.Exp(model.LogError90);
            result.TenMoHinh = ValuationTrainingService.ModelName; result.MaPhienBan = active!.MaPhienBan;
            result.Message = $"Ước tính giá rao từ mô hình huấn luyện; MAPE tập kiểm tra {model.Metrics.Mape:P1}. Khoảng giá từ sai số hiệu chỉnh, không phải cam kết xác suất cho từng tài sản.";
            if ((model.Support[^1].Date - model.Origin).TotalDays >= 365 && Math.Abs(model.Weights[2] / 12) <= .03)
                for (var i = 1; i <= 6; i++)
                {
                    var p = model.Predict(query with { Date = now.AddMonths(i) });
                    if (!double.IsFinite(p) || !double.IsFinite(p*Math.Exp(model.LogError90)) || p*Math.Exp(model.LogError90) > 1e15) break;
                    result.DanhSachDuBao6Thang.Add(new DuBaoThangItemDto { Thang = i, NhanThang = $"T+{i} ({now.AddMonths(i):MM/yyyy})",
                        GiaDuBaoVnd = (decimal)Math.Round(p), GiaMinVnd = (decimal)Math.Round(p*Math.Exp(-model.LogError90)),
                        GiaMaxVnd = (decimal)Math.Round(p*Math.Exp(model.LogError90)), PhanTramTangTruong = (p/price-1)*100 });
                }
        }
        else
        {
            var comparables = (await training.DataAsync()).Where(x => x.Type == query.Type && x.Ward == query.Ward
                && x.Date >= now.AddMonths(-12) && x.Area >= query.Area*.5 && x.Area <= query.Area*2).ToArray();
            if (comparables.Length < 3) return new KetQuaDinhGiaViewModel { Message = "Chưa có mô hình phù hợp và chưa đủ 3 tin bán đã duyệt tương đồng trong phường/xã. Cần thêm dữ liệu thực để định giá." };
            var units = comparables.Select(x => x.Price/x.Area).Order().ToArray();
            price = ValuationRegression.Median(units)*query.Area; low = units[0]*query.Area; high = units[^1]*query.Area;
            result.TenMoHinh = "So sánh tin bán đã duyệt";
            result.Message = $"Tham khảo {comparables.Length} tin cùng loại/phường trong 12 tháng; chưa dùng ML. Khoảng giá là dải giá rao quan sát.";
        }
        if (!double.IsFinite(price) || !double.IsFinite(high) || price <= 0 || high > 1e15)
            return new KetQuaDinhGiaViewModel { Message = "Kết quả ngoài phạm vi dữ liệu hỗ trợ." };
        result.Success = true; result.GiaDuDoanVnd = (decimal)Math.Round(price); result.GiaThapNhatVnd = (decimal)Math.Round(low);
        result.GiaCaoNhatVnd = (decimal)Math.Round(high); result.DonGiaTrenM2Vnd = Math.Round(result.GiaDuDoanVnd/input.DienTich);
        result.Message += " Đường, mặt tiền, pháp lý và nội thất chưa được dùng trong phiên bản đầu. "
            + (result.DanhSachDuBao6Thang.Count == 0 ? "Chưa đủ lịch sử để dự báo 6 tháng." : "Dự báo là kịch bản ngoại suy thời gian, chưa kiểm định riêng chân trời 6 tháng.");
        return result;
    }
}
