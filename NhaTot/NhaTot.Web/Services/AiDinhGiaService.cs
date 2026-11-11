using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Services
{
    public class AiDinhGiaService : IAiDinhGiaService
    {
        private readonly NhaTotDbContext _context;

        public AiDinhGiaService(NhaTotDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> TinhGiaGoiYAsync(int bdsId)
        {
            var bds = await _context.BatDongSans.FindAsync(bdsId);
            if (bds == null) return 0;
            return 1000000000;
        }

        public async Task<object> DinhGiaBatDongSanAsync(DinhGiaInputModel input)
        {
            // Logic tính toán AI dựa trên input (Diện tích, số tầng, độ rộng đường,...)
            // Đây là ví dụ mẫu tính sơ bộ theo diện tích và đơn giá giả định
            decimal donGiaM2 = 25000000; // 25 triệu/m² cơ bản
            if (input.RongDuongMeters.HasValue && input.RongDuongMeters > 5)
            {
                donGiaM2 += 5000000; // Đường lớn cộng thêm giá
            }

            decimal tongGia = input.DienTichDat * donGiaM2;

            // Trả về object chứa các trường khớp với JavaScript phía Client mong đợi
            return await Task.FromResult(new
            {
                success = true,
                giaDuBoanVnd = tongGia,
                donGiaTrenM2Vnd = donGiaM2,
                doTinCaiPhanTram = 92.5
            });
        }
    }
}