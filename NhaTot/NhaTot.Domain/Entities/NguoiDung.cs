using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class NguoiDung
{
    public long MaNguoiDung { get; set; }

    public string? HoTen { get; set; }

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public string? HashMatKhau { get; set; }

    public int? VaiTro { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<BaoCaoTinDang> BaoCaoTinDangMaNguoiBaoCaoNavigations { get; set; } = new List<BaoCaoTinDang>();

    public virtual ICollection<BaoCaoTinDang> BaoCaoTinDangMaNguoiXuLyNavigations { get; set; } = new List<BaoCaoTinDang>();

    public virtual ICollection<LichHen> LichHens { get; set; } = new List<LichHen>();

    public virtual ICollection<LichSuDinhGia> LichSuDinhGia { get; set; } = new List<LichSuDinhGia>();

    public virtual ICollection<LichSuGiaTin> LichSuGiaTins { get; set; } = new List<LichSuGiaTin>();

    public virtual ICollection<MucTieuTheoDoiGia> MucTieuTheoDoiGia { get; set; } = new List<MucTieuTheoDoiGia>();

    public virtual ICollection<NhatKyHeThong> NhatKyHeThongs { get; set; } = new List<NhatKyHeThong>();

    public virtual ICollection<NhuCauTimKiem> NhuCauTimKiems { get; set; } = new List<NhuCauTimKiem>();

    public virtual ICollection<PhanCongTuVan> PhanCongTuVanMaNguoiPhanCongNavigations { get; set; } = new List<PhanCongTuVan>();

    public virtual ICollection<PhanCongTuVan> PhanCongTuVanMaTuVanVienNavigations { get; set; } = new List<PhanCongTuVan>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<TinDang> TinDangMaNguoiDangNavigations { get; set; } = new List<TinDang>();

    public virtual ICollection<TinDang> TinDangMaNguoiDuyetNavigations { get; set; } = new List<TinDang>();

    public virtual ICollection<ViTaiKhoan> ViTaiKhoans { get; set; } = new List<ViTaiKhoan>();

    public virtual ICollection<YeuCauTuVan> YeuCauTuVans { get; set; } = new List<YeuCauTuVan>();

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
