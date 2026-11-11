namespace NhaTot.Web.Models;

public class HomeViewModel
{
    public SearchFilterModel Filter { get; set; } = new();
    public List<NhaTot.Domain.Entities.LoaiBd> DanhSachLoaiBds { get; set; } = new();
    public List<NhaTot.Domain.Entities.TinhThanh> DanhSachTinhThanh { get; set; } = new();
    public List<TinDangItemDto> TinNoiBat { get; set; } = new();
    public List<TinDangItemDto> TinMoiNhat { get; set; } = new();
}

public class SearchFilterModel
{
    public string? LoaiGiaoDich { get; set; } // "BAN" hoặc "CHO_THUE"
    public int? MaLoaiBds { get; set; }
    public int? MaTinh { get; set; }
    public int? MaQuanHuyen { get; set; }
    public int? MaPhuongXa { get; set; }
    public decimal? GiaTu { get; set; }
    public decimal? GiaDen { get; set; }
    public decimal? DienTichTu { get; set; } // Sửa double? -> decimal?
    public decimal? DienTichDen { get; set; } // Sửa double? -> decimal?
    public string? Huong { get; set; }
    public string? TuKhoa { get; set; }
}

public class TinDangItemDto
    {
        public long MaTin { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public decimal? GiaRaoVnd { get; set; }
        public string? LoaiGiaoDich { get; set; }
        public string? TenLoaiBds { get; set; }
        public decimal? DienTichDat { get; set; }
        public string? DiaChiHienThi { get; set; }
        public decimal? ViDo { get; set; }
        public decimal? KinhDo { get; set; }
        public string? HinhAnh { get; set; }
        public string? AnhDaiDien { get; set; }
        public DateTime? NgayDang { get; set; }
    }

public class MapLocationDto
{
    public long MaTin { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public decimal? GiaRaoVnd { get; set; }
    public decimal? DienTichDat { get; set; } // Sửa double? -> decimal?
    public decimal ViDo { get; set; } // Sửa double -> decimal
    public decimal KinhDo { get; set; } // Sửa double -> decimal
    public string DiaChi { get; set; } = string.Empty;
}