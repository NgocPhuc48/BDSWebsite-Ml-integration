using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Models
{
    public class ChiTietTinDangViewModel
    {
        public TinDang TinDang { get; set; } = default!;
        public BatDongSan? BatDongSan { get; set; }
        public ChiTietNha? ChiTietNha { get; set; }
        public ChiTietDat? ChiTietDat { get; set; }
        public NguoiDung? NguoiDang { get; set; }

        public decimal? GiaDinhGiaAi { get; set; }
        public decimal? MucChechLechGia { get; set; }
        public double? PhamViChechLechPhanTram { get; set; }

        public IEnumerable<TinDangItemDto> DanhSachBdsLienQuan { get; set; } = new List<TinDangItemDto>();
    }

    public class DatLichHenInputModel
    {
        [Required]
        public long MaTin { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn")]
        public DateTime NgayHen { get; set; }

        public string? GioHen { get; set; }
        public string? GhiChu { get; set; }
    }

    public class GuiYeuCauTuVanInputModel
    {
        [Required]
        public long MaTin { get; set; }

        public string? TieuDe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        public string? NoiDung { get; set; }
    }

    public class BaoCaoViPhamInputModel
    {
        [Required]
        public long MaTin { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do vi phạm")]
        public string LyDo { get; set; } = string.Empty;

        public string? MoTa { get; set; }
    }
}