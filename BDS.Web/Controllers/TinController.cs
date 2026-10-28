using System.Security.Claims;
using Core.DTO;
using Core.RequestModel;
using Core.Service.TinBanService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDS.Web.Controllers
{
    public class TinController : Controller
    {
        [HttpGet("/bds/{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var tin = await new TinBanService().LayTinById(id);
            if (tin == null) return NotFound();
            return View(tin);
        }

        [HttpGet]
        public async Task<IActionResult> Dang()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.KhuVuc = await new TinBanService().LayKhuVucFilterModel();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dang(string tenChuNha, string diaChi, string tieuDe, string moTa, long giaBan, string soDienThoai, double dienTich, int soPhongNgu, int soPhongVeSinh, string? provinceCode, List<IFormFile> anh)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tinMoi = new TaoTinKhachNgoai
            {
                BatDongSan = new BatDongSanDTO
                {
                    TenChuNha = tenChuNha,
                    DiaChi = diaChi,
                    DiaChiGoogleMap = diaChi,
                    SoDienThoaiChuNha = soDienThoai,
                    SoDienThoaiLienHe = soDienThoai,
                    GiaBan = giaBan,
                    DienTich = dienTich,
                    SoPhongNgu = soPhongNgu,
                    SoPhongVeSinh = soPhongVeSinh,
                    ProvinceCode = provinceCode,
                    MoTaChiTiet = moTa,
                    PhapLy = Core.Enums.PhapLy.DACOSO,
                    DauChuTaoId = userId
                },
                TinBan = new TinBanDTO
                {
                    TieuDe = tieuDe,
                    MoTa = moTa,
                    GiaBan = giaBan,
                    SoDienThoai = soDienThoai,
                    NguoiDangId = userId
                }
            };
            await new TinBanService().DangTinBanKhachNgoai(tinMoi, anh ?? new List<IFormFile>());
            TempData["ThongBao"] = "Đã gửi tin. Tin đang chờ duyệt.";
            return RedirectToAction("Index", "Profile");
        }
    }
}
