using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;
using NhaTot.Web.Services;

namespace NhaTot.Web.Controllers
{
    public class DinhGiaController : Controller
    {
        private readonly NhaTotDbContext _context;
        private readonly IAiDinhGiaService _dinhGiaService;

        public DinhGiaController(NhaTotDbContext context, IAiDinhGiaService dinhGiaService)
        {
            _context = context;
            _dinhGiaService = dinhGiaService;
        }

        // GET: /DinhGia
        public async Task<IActionResult> Index()
        {
            ViewBag.DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();
            ViewBag.DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
            return View(new DinhGiaInputModel());
        }

        // POST: /DinhGia/TinhGia
        [HttpPost]
        public async Task<IActionResult> TinhGia([FromBody] DinhGiaInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var result = await _dinhGiaService.DinhGiaBatDongSanAsync(input);
            return Json(result);
        }
    }
}