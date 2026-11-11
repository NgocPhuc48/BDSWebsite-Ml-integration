using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class NhuCauTimKiem
{
    public long MaNhuCau { get; set; }

    public long? MaNguoiDung { get; set; }

    public int? MaLoaiBds { get; set; }

    public int? MaPhuongXa { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public decimal? NganSachMinVnd { get; set; }

    public decimal? NganSachMaxVnd { get; set; }

    public decimal? DienTichMin { get; set; }

    public decimal? DienTichMax { get; set; }

    public string? MucDich { get; set; }

    public string? TieuChiThem { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual LoaiBd? MaLoaiBdsNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }

    public virtual ICollection<YeuCauTuVan> YeuCauTuVans { get; set; } = new List<YeuCauTuVan>();
}
