using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class ChiTietNha
{
    public long MaChiTietNha { get; set; }

    public long? MaBds { get; set; }

    public int? SoTang { get; set; }

    public int? SoPhongNgu { get; set; }

    public int? SoWc { get; set; }

    public int? NamXayDung { get; set; }

    public string? TinhTrangNha { get; set; }

    public string? NoiThat { get; set; }

    public virtual BatDongSan? MaBdsNavigation { get; set; }
}
