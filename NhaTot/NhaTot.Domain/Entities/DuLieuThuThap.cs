using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class DuLieuThuThap
{
    public long MaDuLieu { get; set; }

    public int? MaNguon { get; set; }

    public string? MaTinNguon { get; set; }

    public string? Urltin { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDungGoc { get; set; }

    public string? DuLieuGocJson { get; set; }

    public int? MaLoaiBdsDaDuyet { get; set; }
    public int? MaPhuongXaDaDuyet { get; set; }
    public string? LoaiGiaoDichDaDuyet { get; set; }
    public string? LoaiNoiDungDaDuyet { get; set; }

    public string? LoaiBdsnguon { get; set; }

    public string? TinhNguon { get; set; }

    public string? PhuongXaNguon { get; set; }

    public decimal? GiaRaoVnd { get; set; }

    public decimal? DienTich { get; set; }

    public string? HashNoiDung { get; set; }

    public DateTime? NgayDangNguon { get; set; }

    public DateTime? NgayThuThap { get; set; }

    public string? TrangThaiXuLy { get; set; }

    public virtual ICollection<LeadKhachHangNhuCau> LeadKhachHangNhuCaus { get; set; } = new List<LeadKhachHangNhuCau>();

    public virtual ICollection<LichSuXuLyDuLieu> LichSuXuLyDuLieus { get; set; } = new List<LichSuXuLyDuLieu>();

    public virtual NguonDuLieu? MaNguonNavigation { get; set; }
}
