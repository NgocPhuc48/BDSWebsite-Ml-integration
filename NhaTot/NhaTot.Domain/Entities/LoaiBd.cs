using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LoaiBd
{
    public int MaLoaiBds { get; set; }

    public string? TenLoaiBds { get; set; }

    public string? MoTa { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<BatDongSan> BatDongSans { get; set; } = new List<BatDongSan>();

    public virtual ICollection<LichSuDinhGia> LichSuDinhGia { get; set; } = new List<LichSuDinhGia>();

    public virtual ICollection<MucTieuTheoDoiGia> MucTieuTheoDoiGia { get; set; } = new List<MucTieuTheoDoiGia>();

    public virtual ICollection<NhuCauTimKiem> NhuCauTimKiems { get; set; } = new List<NhuCauTimKiem>();
}
