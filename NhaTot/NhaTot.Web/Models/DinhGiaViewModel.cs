using System;
using System.ComponentModel.DataAnnotations;

namespace NhaTot.Web.Models
{
    public class DinhGiaInputModel
    {
        public long? MaBds { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản")]
        public int MaLoaiBds { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public int MaPhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập diện tích đất")]
        [Range(1, 100000, ErrorMessage = "Diện tích phải lớn hơn 0")]
        public decimal DienTichDat { get; set; }

        public double? DienTichSuDung { get; set; }
        public int? SoTang { get; set; }
        public int? SoPhongNgu { get; set; }
        public int? SoPhongTam { get; set; }
        public string? Huong { get; set; }
        public double? RongDuongMeters { get; set; }
    }

    public class KetQuaDinhGiaViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public decimal GiaDuBoanVnd { get; set; }
        public decimal GiaMinVnd { get; set; }
        public decimal GiaMaxVnd { get; set; }
        public decimal DonGiaTrenM2Vnd { get; set; }

        public double DoTinCaiPhanTram { get; set; }
        public string TenMoHinh { get; set; } = "Random Forest Regressor v2.1";
        public DateTime ThoiGianThucHien { get; set; } = DateTime.Now;

        public decimal? GiaDauGoiVnd { get; set; }
        public decimal? ChechLechVnd { get; set; }
        public double? ChechLechPhanTram { get; set; }
    }
}