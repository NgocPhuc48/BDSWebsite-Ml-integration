using BDS.Web.Models;
using Core.Enums;
using Core.RequestModel;
using Core.Service.TinBanService;
using Microsoft.AspNetCore.Mvc;

namespace BDS.Web.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(TimKiem tim)
        {
            var filter = new Filter
            {
                tinhCode = tim.tinhCode,
                dienTich = tim.dienTich,
                keyword = tim.keyword,
                min = tim.min,
                max = tim.max
            };
            if (tim.min.HasValue && !tim.max.HasValue) filter.max = long.MaxValue;
            if (tim.max.HasValue && !tim.min.HasValue) filter.min = 0;
            if (tim.loai.HasValue && Enum.IsDefined(typeof(LoaiBatDongSan), tim.loai.Value))
            {
                filter.loaiBatDongSan = new List<LoaiBatDongSan> { (LoaiBatDongSan)tim.loai.Value };
            }

            var tinBan = new TinBanService();
            ViewBag.Tim = tim;
            ViewBag.KhuVuc = await tinBan.LayKhuVucFilterModel();
            ViewBag.TinCongTy = tinBan.LayHetTinBanWeb().ToList();
            var tin = tinBan.LayHetTinBan(filter).ToList();
            return View(tin);
        }
    }
}
