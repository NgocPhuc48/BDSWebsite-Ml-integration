using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class TinDang
{
    public long MaTin { get; set; }

    public long? MaBds { get; set; }

    public long? MaNguoiDang { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public string? TieuDe { get; set; }

    public string? MoTa { get; set; }

    public decimal? GiaRaoVnd { get; set; }

    public decimal? TienCocVnd { get; set; }

    public int? KyHanThanhToanThang { get; set; }

    public int? ThoiHanHopDongMinThang { get; set; }

    public string? DanhSachAnhDatDai { get; set; }

    public string? DanhSachAnhSoDoSoHong { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayDang { get; set; }

    public DateTime? NgayHetHan { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public long? MaNguoiDuyet { get; set; }

    public string? LyDoTuChoi { get; set; }

    public virtual ICollection<BaoCaoTinDang> BaoCaoTinDangs { get; set; } = new List<BaoCaoTinDang>();

    public virtual ICollection<LichHen> LichHens { get; set; } = new List<LichHen>();

    public virtual ICollection<LichSuGiaTin> LichSuGiaTins { get; set; } = new List<LichSuGiaTin>();

    public virtual BatDongSan? MaBdsNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDangNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDuyetNavigation { get; set; }

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
