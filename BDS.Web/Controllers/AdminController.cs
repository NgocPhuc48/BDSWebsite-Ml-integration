using Core.Data;
using Core.DTO;
using Core.Entities;
using Core.Service.NguoiDungService;
using Core.Service.PhongBanService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDS.Web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        public IActionResult NguoiDung()
        {
            ViewBag.Users = new NguoiDungService().LayHetNguoiDung().ToList();
            ViewBag.PhongBan = new PhongBanService().LayHetPhongBan().ToList();
            ViewBag.Roles = new BDSContext().Role.AsNoTracking().ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoNguoiDung(string hoTenNguoiDung, string tenTaiKhoan, string matKhau, string? soDienThoai, int? phongBanId)
        {
            var ketQua = await new NguoiDungService().TaoNguoiDung(new NguoiDungDTO
            {
                HoTenNguoiDung = hoTenNguoiDung,
                TenTaiKhoan = tenTaiKhoan,
                MatKhau = matKhau,
                SoDienThoai = soDienThoai,
                PhongBanId = phongBanId,
                NgayThangNamSinh = DateTime.Today.AddYears(-20)
            });
            TempData["ThongBao"] = ketQua == null ? "Tài khoản đã tồn tại." : "Đã tạo người dùng.";
            return RedirectToAction(nameof(NguoiDung));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaNguoiDung(int id, string hoTenNguoiDung, string matKhau, string? soDienThoai, string? soCanCuoc, string? diaChi, int? phongBanId, int[]? roleIds)
        {
            var roles = (roleIds ?? Array.Empty<int>()).Select(roleId => new NguoiDungRole
            {
                NguoiDungId = id,
                RoleId = roleId,
                NgayTao = DateTime.Now
            }).ToList();
            await new NguoiDungService().SuaNguoiDung(new NguoiDungDTO
            {
                Id = id,
                HoTenNguoiDung = hoTenNguoiDung,
                MatKhau = matKhau,
                SoDienThoai = soDienThoai,
                SoCanCuoc = soCanCuoc,
                DiaChi = diaChi,
                PhongBanId = phongBanId,
                NguoiDungRole = roles
            });
            TempData["ThongBao"] = "Đã cập nhật người dùng.";
            return RedirectToAction(nameof(NguoiDung));
        }

        public IActionResult PhongBan()
        {
            ViewBag.PhongBan = new PhongBanService().LayHetPhongBan().ToList();
            ViewBag.NhanVien = new PhongBanService().LayHetNhanVienKoPhaiTruongPhong().ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhongBan(string tenPhongBan, string khauHieu, int truongPhongId)
        {
            await new PhongBanService().TaoPhongBan(new PhongBanDTO
            {
                TenPhongBan = tenPhongBan,
                KhauHieu = khauHieu,
                TruongPhongId = truongPhongId,
                NgayTao = DateTime.Now
            });
            TempData["ThongBao"] = "Đã tạo phòng ban.";
            return RedirectToAction(nameof(PhongBan));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaPhongBan(int id, string tenPhongBan, string khauHieu, int truongPhongId, Core.Enums.TrangThaiPhongBan trangThai)
        {
            await new PhongBanService().CapNhatPhongBan(new PhongBanDTO
            {
                Id = id,
                TenPhongBan = tenPhongBan,
                KhauHieu = khauHieu,
                TruongPhongId = truongPhongId,
                TrangThai = trangThai
            });
            TempData["ThongBao"] = "Đã cập nhật phòng ban.";
            return RedirectToAction(nameof(PhongBan));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaPhongBan(int id)
        {
            var ketQua = await new PhongBanService().XoaPhongBan(id);
            TempData["ThongBao"] = ketQua == null
                ? "Không xóa được vì phòng ban còn thành viên."
                : "Đã xóa phòng ban.";
            return RedirectToAction(nameof(PhongBan));
        }
    }
}
