using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LichSuDinhGia
{
    public long MaDinhGia { get; set; }

    public long? MaNguoiDung { get; set; }

    public long? MaBds { get; set; }

    public int? MaPhienBan { get; set; }

    public int? MaPhuongXa { get; set; }

    public int? MaLoaiBds { get; set; }

    public decimal? DienTich { get; set; }

    public decimal? ChieuRongMatTien { get; set; }

    public decimal? DuongVaoRong { get; set; }

    public string? Huong { get; set; }

    public string? PhapLy { get; set; }

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public decimal? GiaDuDoanVnd { get; set; }

    public decimal? GiaThapNhatVnd { get; set; }

    public decimal? GiaCaoNhatVnd { get; set; }

    public decimal? DoTinCay { get; set; }

    public string? CanhBao { get; set; }

    public DateTime? ThoiGianDinhGia { get; set; }

    public virtual BatDongSan? MaBdsNavigation { get; set; }

    public virtual LoaiBd? MaLoaiBdsNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual PhienBanMoHinh? MaPhienBanNavigation { get; set; }

    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }
}
