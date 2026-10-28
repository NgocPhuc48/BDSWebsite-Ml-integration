using System.Globalization;
using Core.Enums;

namespace BDS.Web.Infrastructure
{
    public static class HienThi
    {
        public static string Tien(long? so)
        {
            if (so == null) return "0 ₫";
            return so.Value.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
        }

        public static string Cat(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length > 40 ? text.Substring(0, 40) + "..." : text;
        }

        public static string ThoiGian(DateTime thoiDiem)
        {
            var miliGiay = Math.Abs((thoiDiem - DateTime.Now).TotalMilliseconds);
            var ngay = (int)(miliGiay / (24 * 60 * 60 * 1000));
            if (ngay > 0) return ngay + " ngày trước";
            var gio = (int)(miliGiay / (60 * 60 * 1000));
            if (gio > 0) return gio + " giờ trước";
            var phut = (int)(miliGiay / (60 * 1000));
            if (phut > 0) return phut + " phút trước";
            return "Vừa xong";
        }

        public static string LoaiNha(LoaiBatDongSan loai) => loai switch
        {
            LoaiBatDongSan.NHAPHO => "Nhà phố",
            LoaiBatDongSan.DATTHOCU => "Đất thổ cư",
            LoaiBatDongSan.DATNEN => "Đất nền",
            LoaiBatDongSan.CHUNGCU => "Chung cư",
            LoaiBatDongSan.KHACHDANG => "Khách đăng",
            _ => loai.ToString()
        };

        public static string TrangThaiTin(TrangThaiTinBan trangThai) => trangThai switch
        {
            TrangThaiTinBan.DANGCHO => "Đang chờ",
            TrangThaiTinBan.DANGHIENTHI => "Đang hiển thị",
            TrangThaiTinBan.DAKHOA => "Đã khóa",
            TrangThaiTinBan.DATRATIENCHOXETDUYET => "Đã trả tiền, chờ duyệt",
            _ => trangThai.ToString()
        };

        public static string? AnhDau(Core.Entities.BatDongSan? bds)
        {
            return bds?.HinhAnhBatDongSan?.FirstOrDefault()?.LinkHinhAnh;
        }
    }
}
