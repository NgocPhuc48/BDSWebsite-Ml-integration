using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LeadKhachHangNhuCau
{
    public long MaLead { get; set; }

    public long? MaDuLieu { get; set; }

    public string? TenKhachHang { get; set; }

    public string? SoDienThoai { get; set; }

    public string? LinkProfileMxh { get; set; }

    public string? NoiDungNhuCau { get; set; }

    public string? NguonMxh { get; set; }

    public int? MaPhuongXa { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public DateTime? NgayQuet { get; set; }

    public virtual DuLieuThuThap? MaDuLieuNavigation { get; set; }

    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }
}
