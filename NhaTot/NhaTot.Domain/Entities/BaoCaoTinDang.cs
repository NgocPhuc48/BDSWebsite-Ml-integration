using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class BaoCaoTinDang
{
    public long MaBaoCao { get; set; }

    public long? MaTin { get; set; }

    public long? MaNguoiBaoCao { get; set; }

    public string? LyDo { get; set; }

    public string? MoTa { get; set; }

    public string? TrangThai { get; set; }

    public long? MaNguoiXuLy { get; set; }

    public DateTime? NgayBaoCao { get; set; }

    public DateTime? NgayXuLy { get; set; }

    public virtual NguoiDung? MaNguoiBaoCaoNavigation { get; set; }

    public virtual NguoiDung? MaNguoiXuLyNavigation { get; set; }

    public virtual TinDang? MaTinNavigation { get; set; }
}
