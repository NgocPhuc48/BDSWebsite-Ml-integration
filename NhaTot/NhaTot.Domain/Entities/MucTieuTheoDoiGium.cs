using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class MucTieuTheoDoiGia
{
    public long MaMucTieu { get; set; }

    public long? MaNguoiDung { get; set; }

    public int? MaQuanHuyen { get; set; }

    public int? MaPhuongXa { get; set; }

    public int? MaLoaiBds { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public decimal? GiaMongMuonVnd { get; set; }

    public decimal? TienCocDiem { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayHetHan { get; set; }

    public virtual LoaiBd? MaLoaiBdsNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }

    public virtual QuanHuyen? MaQuanHuyenNavigation { get; set; }
}
