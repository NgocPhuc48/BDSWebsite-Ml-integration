using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class PhuongXa
{
    public int MaPhuongXa { get; set; }

    public int? MaQuanHuyen { get; set; }

    public string? TenPhuongXa { get; set; }

    public string? MaHanhChinh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<BatDongSan> BatDongSans { get; set; } = new List<BatDongSan>();

    public virtual ICollection<LeadKhachHangNhuCau> LeadKhachHangNhuCaus { get; set; } = new List<LeadKhachHangNhuCau>();

    public virtual ICollection<LichSuDinhGia> LichSuDinhGia { get; set; } = new List<LichSuDinhGia>();

    public virtual QuanHuyen? MaQuanHuyenNavigation { get; set; }

    public virtual ICollection<MucTieuTheoDoiGia> MucTieuTheoDoiGia { get; set; } = new List<MucTieuTheoDoiGia>();

    public virtual ICollection<NhuCauTimKiem> NhuCauTimKiems { get; set; } = new List<NhuCauTimKiem>();
}
