using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class ViTaiKhoan
{
    public long MaVi { get; set; }

    public long? MaNguoiDung { get; set; }

    public decimal? SoDuDiem { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<LichSuGiaoDich> LichSuGiaoDiches { get; set; } = new List<LichSuGiaoDich>();

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }
}
