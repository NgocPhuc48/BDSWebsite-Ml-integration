using System.Security.Claims;
using Core.DTO;
using Core.RequestModel;
using Core.Service.NguoiDungService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BDS.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string taiKhoan, string matKhau, string? returnUrl)
        {
            var user = await new NguoiDungService().DangNhap(new DangNhapRequest
            {
                TaiKhoan = taiKhoan,
                MatKhau = matKhau
            });
            if (user == null)
            {
                ViewBag.Loi = "Tài khoản hoặc mật khẩu không đúng.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            await DangNhapCookie(user);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string hoTenNguoiDung, string tenTaiKhoan, string matKhau, string xacNhanMatKhau)
        {
            if (matKhau != xacNhanMatKhau)
            {
                ViewBag.Loi = "Mật khẩu xác nhận không khớp.";
                return View();
            }
            var ketQua = await new NguoiDungService().DangKy(new NguoiDungDTO
            {
                HoTenNguoiDung = hoTenNguoiDung,
                TenTaiKhoan = tenTaiKhoan,
                MatKhau = matKhau,
                NgayThangNamSinh = DateTime.Today.AddYears(-20)
            });
            if (ketQua == null)
            {
                ViewBag.Loi = "Tài khoản đã tồn tại.";
                return View();
            }
            TempData["ThongBao"] = "Đăng ký thành công. Hãy đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Forgot() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(string email)
        {
            var user = await new NguoiDungService().QuenMatKhau(email);
            ViewBag.ThongBao = user == null
                ? "Không tìm thấy tài khoản."
                : "Nếu email hợp lệ, hệ thống đã gửi mã đổi mật khẩu. Mở liên kết trong email hoặc nhập mã bên dưới.";
            return View();
        }

        public IActionResult Reset(string? code)
        {
            ViewBag.Code = code;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(string code, string matKhauMoi)
        {
            var user = await new NguoiDungService().XacNhanMatKhau(code, matKhauMoi);
            if (user == null)
            {
                ViewBag.Loi = "Mã không hợp lệ hoặc đã hết hạn.";
                ViewBag.Code = code;
                return View();
            }
            TempData["ThongBao"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task DangNhapCookie(NguoiDungDTO user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTenNguoiDung ?? user.TenTaiKhoan),
                new Claim("TenTaiKhoan", user.TenTaiKhoan ?? ""),
                new Claim("HinhDaiDien", user.HinhDaiDien ?? "")
            };
            if (user.NguoiDungRole != null)
            {
                foreach (var item in user.NguoiDungRole)
                {
                    if (!string.IsNullOrEmpty(item.Role?.RoleCode))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, item.Role.RoleCode));
                    }
                }
            }
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
    }
}
