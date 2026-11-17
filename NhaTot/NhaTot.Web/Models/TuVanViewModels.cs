using System;
using System.Collections.Generic;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Models
{
    public class TuVanIndexViewModel
    {
        public string ActiveTab { get; set; } = "my_tasks"; // "my_tasks", "available", "schedule"
        public List<YeuCauTuVanItemDto> DanhSachYeuCauCuaToi { get; set; } = new();
        public List<YeuCauTuVanItemDto> DanhSachYeuCauChuaNhan { get; set; } = new();
        public List<LichHenItemDto> DanhSachLichHen { get; set; } = new();

        public int SoLuongCuaToi => DanhSachYeuCauCuaToi.Count;
        public int SoLuongChuaNhan => DanhSachYeuCauChuaNhan.Count;
        public int SoLuongLichHenSapToi => DanhSachLichHen.Count(l => l.TrangThai == "SCHEDULED" || l.TrangThai == "CONFIRMED");
    }

    public class YeuCauTuVanItemDto
    {
        public long MaYeuCau { get; set; }
        public string? TieuDe { get; set; }
        public string? NoiDung { get; set; }
        public string? TrangThai { get; set; }
        public int? MucDoUuTien { get; set; }
        public DateTime? NgayTao { get; set; }

        public string? TenKhachHang { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }

        public string? TenLoaiBds { get; set; }
        public string? KhuVuc { get; set; }
        public decimal? NganSachMinVnd { get; set; }
        public decimal? NganSachMaxVnd { get; set; }

        public string? TenTuVanVien { get; set; }
        public DateTime? NgayPhanCong { get; set; }
        public bool DaCoNguoiNhan => !string.IsNullOrEmpty(TenTuVanVien);
    }

    public class ChiTietYeuCauTuVanViewModel
    {
        public YeuCauTuVan YeuCau { get; set; } = null!;
        public NguoiDung? KhachHang { get; set; }
        public NhuCauTimKiem? NhuCau { get; set; }
        public PhanCongTuVan? PhanCongHienTai { get; set; }
        public List<LichHen> DanhSachLichHen { get; set; } = new();
        public List<TinDangGoiYDto> DanhSachTinDangGoiY { get; set; } = new();

        public bool LaTuVanVienPhuTrach { get; set; }
    }

    public class TinDangGoiYDto
    {
        public long MaTin { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public decimal? GiaRaoVnd { get; set; }
        public decimal? DienTichDat { get; set; }
        public string? DiaChi { get; set; }
        public string? TenLoaiBds { get; set; }
        public string? AnhDaiDien { get; set; }
        public string? Huong { get; set; }
    }

    public class LichHenIndexViewModel
    {
        public string? StatusFilter { get; set; } // "ALL", "CHO_XAC_NHAN", "SAP_DIEN_RA", "DA_XEM_NHA", "DA_HUY"
        public List<LichHenItemDto> DanhSachLichHen { get; set; } = new();
        public int TongLichHen => DanhSachLichHen.Count;
        public int SoLuongChoXacNhan { get; set; }
        public int SoLuongSapDienRa { get; set; }
        public int SoLuongDaXem { get; set; }
        public int SoLuongDaHuy { get; set; }
    }

    public class LichHenItemDto
    {
        public long MaLichHen { get; set; }
        public long? MaYeuCau { get; set; }
        public string? TieuDeYeuCau { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SoDienThoaiKhach { get; set; }
        public long? MaTin { get; set; }
        public string? TieuDeTinDang { get; set; }
        public DateTime? ThoiGianHen { get; set; }
        public string? DiaDiem { get; set; }
        public string? TrangThai { get; set; } // SCHEDULED, COMPLETED, CANCELLED
        public string? GhiChu { get; set; }
    }

    public class TaoLichHenInputModel
    {
        public long MaYeuCau { get; set; }
        public long? MaTin { get; set; }
        public DateTime ThoiGianHen { get; set; }
        public string DiaDiem { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }
}
