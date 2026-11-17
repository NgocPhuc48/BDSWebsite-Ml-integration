using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NhaTot.Web.Models
{
    public class TaoMucTieuBayGiaViewModel
    {
        public int? MaTinh { get; set; }

        public int? MaQuanHuyen { get; set; }

        public int? MaPhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản.")]
        public int MaLoaiBds { get; set; }

        public string LoaiGiaoDich { get; set; } = "BAN"; // BAN hoặc CHO_THUE

        // --- Thuộc tính chung cho cả Đất và Nhà ---
        public decimal? DienTichMin { get; set; } // Diện tích tối thiểu (m²)
        public string? Huong { get; set; } // Đông, Tây, Nam, Bắc, Đông Nam...
        public string? PhapLy { get; set; } // Sổ đỏ/Sổ hồng, Hợp đồng mua bán...

        // --- Thuộc tính riêng của ĐẤT (ChiTietDat) ---
        public string? LoaiDat { get; set; } // Đất thổ cư, Đất nền dự án...
        public decimal? MatTienMinM { get; set; } // Mặt tiền tối thiểu (m)
        public decimal? ChieuSauMinM { get; set; } // Chiều sâu tối thiểu (m)
        public string? LoaiDuong { get; set; } // Nhựa, Bê tông, Đất...
        public decimal? DoRongDuongMinM { get; set; } // Độ rộng đường tối thiểu (m)
        public string? HinhDangDat { get; set; } // Vuông vắn, Nở hậu...

        // --- Thuộc tính riêng của NHÀ / CĂN HỘ (ChiTietNha) ---
        public int? SoTangMin { get; set; } // Số tầng tối thiểu
        public int? SoPhongNgu { get; set; } // Số phòng ngủ tối thiểu
        public int? SoWcMin { get; set; } // Số WC tối thiểu
        public int? NamXayDungMin { get; set; } // Năm xây dựng (từ năm)
        public string? TinhTrangNha { get; set; } // Nhà mới, Đã qua sử dụng...
        public string? NoiThat { get; set; } // Đầy đủ, Cơ bản, Nhà trống

        // --- Khoảng giá ---
        public decimal? NganSachMinVnd { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá tối đa mong muốn.")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá mong muốn phải lớn hơn 0.")]
        public decimal NganSachMaxVnd { get; set; }

        public string? GhiChuTieuChi { get; set; }
    }

    public class TieuChiBayGiaDetailDto
    {
        public decimal? DienTichMin { get; set; }
        public string? Huong { get; set; }
        public string? PhapLy { get; set; }

        // Đất
        public string? LoaiDat { get; set; }
        public decimal? MatTienMinM { get; set; }
        public decimal? ChieuSauMinM { get; set; }
        public string? LoaiDuong { get; set; }
        public decimal? DoRongDuongMinM { get; set; }
        public string? HinhDangDat { get; set; }

        // Nhà
        public int? SoTangMin { get; set; }
        public int? SoPhongNgu { get; set; }
        public int? SoWcMin { get; set; }
        public int? NamXayDungMin { get; set; }
        public string? TinhTrangNha { get; set; }
        public string? NoiThat { get; set; }

        public string? GhiChu { get; set; }
    }

    public class KetQuaKhopItemDto
    {
        public string NguonTin { get; set; } = string.Empty; // "TinDang" hoặc "LeadMXH"
        public long Id { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public decimal? GiaRaoVnd { get; set; }
        public string GiaHienThi { get; set; } = string.Empty;
        public string? TenKhuVuc { get; set; }
        public string? TenLoaiBds { get; set; }
        public decimal? DienTich { get; set; }
        public string? Huong { get; set; }
        public int? SoPhongNgu { get; set; }
        public decimal? MatTienM { get; set; }
        public string? HinhAnh { get; set; }
        public string? SoDienThoai { get; set; }
        public string? TenLienHe { get; set; }
        public string? UrlTinGoc { get; set; } // Link chi tiết bài đăng hoặc Link Profile/Group FB
        public bool CoLinkMxh { get; set; }
        public DateTime? NgayDang { get; set; }
    }

    public class MucTieuTheoDoiChiTietDto
    {
        public long MaMucTieu { get; set; }
        public string? TenQuanHuyen { get; set; }
        public string? TenPhuongXa { get; set; }
        public string? TenLoaiBds { get; set; }
        public string? LoaiGiaoDich { get; set; }
        public decimal? NganSachMinVnd { get; set; }
        public decimal? NganSachMaxVnd { get; set; }
        public int? SoPhongNgu { get; set; }
        public decimal? PhiDaTruDiem { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public bool DaHetHan { get; set; }
        public string? ThongBaoHoanTien { get; set; }

        // Tiêu chí chi tiết kèm theo
        public TieuChiBayGiaDetailDto? TieuChiChiTiet { get; set; }
        public List<string> DanhSachMoTaTieuChi { get; set; } = new List<string>();

        public List<KetQuaKhopItemDto> DanhSachKhop { get; set; } = new List<KetQuaKhopItemDto>();
    }

    public class BayGiaIndexViewModel
    {
        public decimal SoDuViHienTai { get; set; }
        public TaoMucTieuBayGiaViewModel FormTao { get; set; } = new TaoMucTieuBayGiaViewModel();
        public List<MucTieuTheoDoiChiTietDto> DanhSachMucTieu { get; set; } = new List<MucTieuTheoDoiChiTietDto>();
        public long? MucTieuDangXemId { get; set; }
    }
}
