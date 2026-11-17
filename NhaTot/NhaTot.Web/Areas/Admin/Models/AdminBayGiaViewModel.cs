using System.Collections.Generic;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Models
{
    public class AdminBayGiaViewModel
    {
        // Thống kê nhanh
        public int TongMucTieuDangChay { get; set; }
        public decimal TongDiemTamGiuCoc { get; set; }
        public int TongMucTieuDaKhop { get; set; }
        public int TongMucTieuDaHoanTien { get; set; }

        public string? FilterTrangThai { get; set; }
        public List<AdminMucTieuItemDto> DanhSachMucTieu { get; set; } = new List<AdminMucTieuItemDto>();
    }

    public class AdminMucTieuItemDto
    {
        public long MaMucTieu { get; set; }
        public long? MaNguoiDung { get; set; }
        public string TenNguoiDung { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal? SoDuViHienTai { get; set; }

        public string TenKhuVuc { get; set; } = string.Empty;
        public string TenLoaiBds { get; set; } = string.Empty;
        public string LoaiGiaoDich { get; set; } = "BAN";
        public decimal? GiaMongMuonVnd { get; set; }
        public decimal? TienCocDiem { get; set; }
        public string TrangThai { get; set; } = "DANG_THEO_DOI";
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public bool DaQuaHan { get; set; }
        public int SoTinKhop { get; set; }
    }
}
