using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NhaTot.Web.Models
{
    public class DangTinViewModel
    {
        // --- Thông tin Tin Đăng ---
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin đăng.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết.")]
        public string MoTa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá rao bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá rao bán phải lớn hơn 0.")]
        public decimal GiaRaoVnd { get; set; }

        // --- Thông tin Bất Động Sản ---
        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản.")]
        public int MaLoaiBds { get; set; }

        // --- Thông tin Vị trí Cascade ---
        public int? MaTinh { get; set; }
        public int? MaQuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        public int MaPhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập diện tích.")]
        [Range(0.1, 100000, ErrorMessage = "Diện tích phải lớn hơn 0.")]
        public double DienTichDatM2 { get; set; }

        public string? DiaChiMoTa { get; set; }

        // --- Chi tiết Nhà (Tùy chọn nếu là Nhà ở) ---
        public int? SoTang { get; set; }
        public int? SoPhongNgu { get; set; }
        public int? SoWc { get; set; }
        public int? NamXayDung { get; set; }
        public string? TinhTrangNha { get; set; }
        public string? NoiThat { get; set; }

        // --- Chi tiết Đất (Tùy chọn) ---
        public string? LoaiDat { get; set; }
        public double? MatTienM { get; set; }
        public double? ChieuSauM { get; set; }
        public string? LoaiDuong { get; set; }
        public double? DoRongDuongM { get; set; }
        public string? HinhDang { get; set; }

        // --- Danh sách ảnh upload ---
        public List<IFormFile>? HinhAnhFiles { get; set; }
    }
}