using System.Security.Claims;
using Core.DTO;
using Core.RequestModel;
using Core.Service.NguoiDungService;
using Core.Service.TinBanService;
using Core.plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDS.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var id = CurrentUserId();
            var service = new NguoiDungService();
            ViewBag.Tin = new TinBanService().LayBaiDangById(id).ToList();
            var user = await service.LayNguoiDung(id);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(string hoTenNguoiDung, string? diaChi, string? soDienThoai, string? soCanCuoc, IFormFile? anh)
        {
            var id = CurrentUserId();
            var dto = new NguoiDungDTO
            {
                Id = id,
                HoTenNguoiDung = hoTenNguoiDung,
                DiaChi = diaChi,
                SoDienThoai = soDienThoai,
                SoCanCuoc = soCanCuoc
            };
            await new NguoiDungService().CapNhatThongTin(dto, anh!);
            TempData["ThongBao"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi)
        {
            var ketQua = await new NguoiDungService().DoiMatKhau(new DoiMatKhau
            {
                NguoiDungId = CurrentUserId(),
                MatKhauCu = matKhauCu,
                MatKhauMoi = matKhauMoi
            });
            TempData["ThongBao"] = ketQua == null ? "Mật khẩu cũ không đúng." : "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaHan(int tinBanId, DateTime ngayHet)
        {
            var ma = await new TinBanService().ThueDangBai(tinBanId, CurrentUserId(), ngayHet);
            TempData["ThongBao"] = ma == 1 ? "Gia hạn tin thành công." : "Số dư không đủ hoặc chưa nạp tiền.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NapTien(long soTien)
        {
            if (soTien <= 0)
            {
                TempData["ThongBao"] = "Số tiền không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }
            var userId = CurrentUserId();
            var hoaDon = await new NguoiDungService().NapTien(soTien, userId);
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", "NX0E4Z3Y");
            vnpay.AddRequestData("vnp_Amount", (soTien * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip) || ip == "::1") ip = "127.0.0.1";
            vnpay.AddRequestData("vnp_IpAddr", ip);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + hoaDon.Id);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", Url.Action(nameof(KetQuaNap), "Profile", null, Request.Scheme)!);
            vnpay.AddRequestData("vnp_TxnRef", hoaDon.Id.ToString());
            var url = vnpay.CreateRequestUrl("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", "BUEOSSZUUHLMPYVUCKRIUFEJRRWZREHH");
            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> KetQuaNap(int vnp_TxnRef, string? vnp_ResponseCode)
        {
            var status = vnp_ResponseCode == "00" ? 1 : 0;
            await new NguoiDungService().CheckThanhToan(vnp_TxnRef, status);
            TempData["ThongBao"] = status == 1 ? "Nạp tiền thành công." : "Nạp tiền chưa hoàn tất.";
            return RedirectToAction(nameof(Index));
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
