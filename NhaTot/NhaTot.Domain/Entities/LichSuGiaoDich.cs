using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LichSuGiaoDich
{
    public long MaGiaoDich { get; set; }

    public long? MaVi { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public decimal? SoDiem { get; set; }

    public decimal? SoDuSauGiaoDich { get; set; }

    public string? MoTa { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ViTaiKhoan? MaViNavigation { get; set; }
}
