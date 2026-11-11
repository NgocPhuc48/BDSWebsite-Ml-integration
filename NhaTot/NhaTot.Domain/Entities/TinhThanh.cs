using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class TinhThanh
{
    public int MaTinh { get; set; }

    public string? TenTinh { get; set; }

    public string? MaHanhChinh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<QuanHuyen> QuanHuyens { get; set; } = new List<QuanHuyen>();
}
