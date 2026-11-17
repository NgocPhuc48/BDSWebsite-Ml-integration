using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NhaTot.Web.Models
{
    public class DangTinViewModel
    {
        // --- 1. THÔNG TIN TIN ĐĂNG (TinDang) ---
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin đăng.")]
        [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết.")]
        public string MoTa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá rao bán/cho thuê.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá rao phải lớn hơn hoặc bằng 0.")]
        public decimal GiaRaoVnd { get; set; }

        public string LoaiGiaoDich { get; set; } = "BAN"; // BAN hoặc CHO_THUE
        public decimal? TienCocVnd { get; set; }
        public int? KyHanThanhToanThang { get; set; }
        public int? ThoiHanHopDongMinThang { get; set; }

        // --- 2. THÔNG TIN BẤT ĐỘNG SẢN CHUNG (BatDongSan) ---
        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản.")]
        public int MaLoaiBds { get; set; }

        public int? MaTinh { get; set; }
        public int? MaQuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        public int MaPhuongXa { get; set; }

        public string? DiaChiMoTa { get; set; }
        public string? TenDuAn { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập diện tích đất.")]
        [Range(0.1, 100000, ErrorMessage = "Diện tích đất phải lớn hơn 0.")]
        public decimal DienTichDatM2 { get; set; }

        public decimal? DienTichSuDungM2 { get; set; }
        public string? Huong { get; set; }
        public string? PhapLy { get; set; }
        public string? MoTaDacDiem { get; set; }

        // Tọa độ GIS (BatDongSan)
        public decimal? ViDo { get; set; }
        public decimal? KinhDo { get; set; }

        // --- 3. CHI TIẾT RIÊNG CHO ĐẤT (ChiTietDat) ---
        public string? LoaiDat { get; set; }
        public decimal? MatTienM { get; set; }
        public decimal? ChieuSauM { get; set; }
        public string? LoaiDuong { get; set; }
        public decimal? DoRongDuongM { get; set; }
        public string? HinhDang { get; set; }

        // --- 4. CHI TIẾT RIÊNG CHO NHÀ Ở (ChiTietNha) ---
        public int? SoTang { get; set; }
        public int? SoPhongNgu { get; set; }
        public int? SoWc { get; set; }
        public int? NamXayDung { get; set; }
        public string? TinhTrangNha { get; set; }
        public string? NoiThat { get; set; }

        // --- 5. HÌNH ẢNH UPLOAD ---
        public List<IFormFile>? HinhAnhFiles { get; set; }
        public List<IFormFile>? AnhSoDoFiles { get; set; }
    }
}
