using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly NhaTotDbContext _context;
        private readonly PasswordHasher<string> _passwordHasher;

        public TaiKhoanController(NhaTotDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<string>();
        }

        // GET: /TaiKhoan/DangKy
        public IActionResult DangKy()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new DangKyViewModel());
        }

        // POST: /TaiKhoan/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DangKyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra trùng Email hoặc Số điện thoại
            bool isEmailExist = await _context.NguoiDungs.AnyAsync(u => u.Email == model.Email);
            if (isEmailExist)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            bool isSdtExist = await _context.NguoiDungs.AnyAsync(u => u.SoDienThoai == model.SoDienThoai);
            if (isSdtExist)
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã được sử dụng.");
                return View(model);
            }

            // Mã hóa mật khẩu
            string hashedPassword = _passwordHasher.HashPassword(model.Email, model.MatKhau);

            var nguoiDung = new NguoiDung
            {
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                HashMatKhau = hashedPassword,
                VaiTro = model.VaiTro,
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };

            _context.NguoiDungs.Add(nguoiDung);
            await _context.SaveChangesAsync();

            // Tự động tạo Ví tài khoản cho người dùng mới
            var viTaiKhoan = new ViTaiKhoan
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                SoDuDiem = 0,
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };
            _context.ViTaiKhoans.Add(viTaiKhoan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // GET: /TaiKhoan/DangNhap
        public IActionResult DangNhap(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new DangNhapViewModel { ReturnUrl = returnUrl });
        }

        // POST: /TaiKhoan/DangNhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Tìm người dùng theo Email hoặc Số điện thoại
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u =>
                u.Email == model.EmailHoacSdt || u.SoDienThoai == model.EmailHoacSdt);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (user.TrangThai == "LOCKED")
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Xác thực mật khẩu
            var result = _passwordHasher.VerifyHashedPassword(user.Email ?? "", user.HashMatKhau ?? "", model.MatKhau);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Tạo danh sách Claims lưu danh tính đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen ?? "Người dùng"),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.VaiTro == 4 ? "Admin" : (user.VaiTro == 2 ? "Broker" : "User"))
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.GhiNhoDangNhap,
                ExpiresUtc = model.GhiNhoDangNhap ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // POST: /TaiKhoan/DangXuat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}