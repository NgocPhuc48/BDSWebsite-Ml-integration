using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class QuanHuyen
{
    public int MaQuanHuyen { get; set; }

    public int? MaTinh { get; set; }

    public string? TenQuanHuyen { get; set; }

    public string? MaHanhChinh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual TinhThanh? MaTinhNavigation { get; set; }

    public virtual ICollection<MucTieuTheoDoiGia> MucTieuTheoDoiGia { get; set; } = new List<MucTieuTheoDoiGia>();

    public virtual ICollection<PhuongXa> PhuongXas { get; set; } = new List<PhuongXa>();
}
