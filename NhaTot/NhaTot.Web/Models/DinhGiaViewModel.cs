using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NhaTot.Web.Models
{
    public class DinhGiaInputModel
    {
        public long? MaBds { get; set; }

        // --- CÁC TRƯỜNG CHUNG (LUÔN HIỆN) ---
        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản")]
        public int MaLoaiBds { get; set; }

        public int? MaTinh { get; set; }
        public int? MaQuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public int MaPhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập diện tích đất")]
        [Range(1, 100000, ErrorMessage = "Diện tích phải lớn hơn 0")]
        public decimal DienTich { get; set; }

        public decimal DienTichDat
        {
            get => DienTich;
            set => DienTich = value;
        }

        public decimal? ChieuRongMatTien { get; set; }
        public decimal? DuongVaoRong { get; set; }

        public double? RongDuongMeters
        {
            get => DuongVaoRong.HasValue ? (double)DuongVaoRong.Value : null;
            set => DuongVaoRong = value.HasValue ? (decimal)value.Value : null;
        }
        public string? Huong { get; set; }
        public string? PhapLy { get; set; }

        public decimal? ViDo { get; set; }
        public decimal? KinhDo { get; set; }

        // --- CÁC TRƯỜNG RIÊNG CỦA NHÀ Ở (ẨN KHI LÀ ĐẤT) ---
        public int? SoTang { get; set; }
        public int? SoPhongNgu { get; set; }
        public int? SoWc { get; set; }
        public string? TinhTrangNha { get; set; }
        public string? NoiThat { get; set; }
    }

    public class DuBaoThangItemDto
    {
        public int Thang { get; set; } // 1..6
        public string NhanThang { get; set; } = string.Empty; // "Tháng 12/2026"
        public decimal GiaDuBaoVnd { get; set; }
        public decimal GiaMinVnd { get; set; }
        public decimal GiaMaxVnd { get; set; }
        public double PhanTramTangTruong { get; set; }
    }

    public class KetQuaDinhGiaViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // Định giá thời điểm hiện tại
        public decimal GiaDuDoanVnd { get; set; }
        public decimal GiaThapNhatVnd { get; set; }
        public decimal GiaCaoNhatVnd { get; set; }
        public decimal DonGiaTrenM2Vnd { get; set; }
        public double? DoTinCayPhanTram { get; set; }
        public int? MaPhienBan { get; set; }

        public string TenMoHinh { get; set; } = "";
        public DateTime ThoiGianThucHien { get; set; } = DateTime.Now;

        // Danh sách dự báo giá 6 tháng tiếp theo
        public List<DuBaoThangItemDto> DanhSachDuBao6Thang { get; set; } = new List<DuBaoThangItemDto>();
    }
}
