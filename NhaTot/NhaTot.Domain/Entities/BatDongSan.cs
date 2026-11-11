using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class BatDongSan
{
    public long MaBds { get; set; }

    public int? MaLoaiBds { get; set; }

    public int? MaPhuongXa { get; set; }

    public string? DiaChiMoTa { get; set; }

    public string? TenDuAn { get; set; }

    public decimal? DienTichDat { get; set; }

    public decimal? DienTichSuDung { get; set; }

    public string? Huong { get; set; }

    public string? PhapLy { get; set; }

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string? MoTaDacDiem { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<ChiTietDat> ChiTietDats { get; set; } = new List<ChiTietDat>();

    public virtual ICollection<ChiTietNha> ChiTietNhas { get; set; } = new List<ChiTietNha>();

    public virtual ICollection<LichSuDinhGia> LichSuDinhGia { get; set; } = new List<LichSuDinhGia>();

    public virtual LoaiBd? MaLoaiBdsNavigation { get; set; }

    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }

    public virtual ICollection<TinDang> TinDangs { get; set; } = new List<TinDang>();
}
