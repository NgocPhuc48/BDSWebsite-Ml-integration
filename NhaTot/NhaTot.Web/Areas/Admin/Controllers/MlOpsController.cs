using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MlOpsController : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Train([FromServices] NhaTot.Web.Services.ValuationTrainingService training)
    {
        try { TempData["TrainingMessage"] = await training.TrainAsync(); }
        catch (InvalidOperationException ex) { TempData["TrainingMessage"] = ex.Message; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
        { TempData["TrainingMessage"] = "Không lưu được mô hình. Kiểm tra quyền ghi artifact và database; mô hình đang dùng được giữ nguyên."; }
        return RedirectToAction(nameof(Index));
    }
    private readonly NhaTotDbContext _context;

    public MlOpsController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/MlOps (Danh sÃ¡ch cÃ¡c phiÃªn báº£n Model AI + ÄÃ¡nh giÃ¡)
    public async Task<IActionResult> Index()
    {
        var modelList = await _context.PhienBanMoHinhs
            .Include(m => m.KetQuaDanhGiaMoHinhs)
            .OrderByDescending(m => m.NgayHuanLuyen)
            .ToListAsync();

        ViewBag.ActiveModel = modelList.FirstOrDefault(m => m.TrangThai == "ACTIVE" && m.TenMoHinh == NhaTot.Web.Services.ValuationTrainingService.ModelName);
        ViewBag.TongSoModel = modelList.Count;

        return View(modelList);
    }

    // GET: Admin/MlOps/GetEvaluationMetrics/5 (Láº¥y cÃ¡c chá»‰ sá»‘ MAE, RMSE, R2 qua AJAX)
    [HttpGet]
    public async Task<IActionResult> GetEvaluationMetrics(int id)
    {
        var modelInfo = await _context.PhienBanMoHinhs
            .Include(m => m.KetQuaDanhGiaMoHinhs)
            .FirstOrDefaultAsync(m => m.MaPhienBan == id);

        if (modelInfo == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin mÃ´ hÃ¬nh!" });

        var metrics = modelInfo.KetQuaDanhGiaMoHinhs.Select(k => new
        {
            k.MaKetQua,
            k.TapDuLieu,
            k.Mae,
            k.Rmse,
            k.R2,
            k.SoMau,
            NgayDanhGia = k.NgayDanhGia?.ToString("dd/MM/yyyy HH:mm")
        });

        return Json(new { 
            success = true, 
            modelName = modelInfo.TenMoHinh, 
            version = modelInfo.PhienBan, 
            metrics = metrics 
        });
    }

    // POST: Admin/MlOps/SetActiveModel/5 (KÃ­ch hoáº¡t phiÃªn báº£n lÃ m Production Model)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActiveModel(int id, [FromServices] NhaTot.Web.Services.ValuationTrainingService training)
    {
        var candidate = await _context.PhienBanMoHinhs.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhienBan == id);
        if (candidate == null || await training.LoadAsync(candidate) == null)
            return Json(new { success = false, message = "Artifact chưa hợp lệ hoặc mô hình chưa đạt ngưỡng đánh giá." });
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var selectedModel = await _context.PhienBanMoHinhs.FindAsync(id);
            if (selectedModel == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y phiÃªn báº£n mÃ´ hÃ¬nh!" });

            // 1. Chuyá»ƒn táº¥t cáº£ cÃ¡c model cÅ© vá» tráº¡ng thÃ¡i DEPRECATED
            var activeModels = await _context.PhienBanMoHinhs
                .Where(m => m.TrangThai == "ACTIVE")
                .ToListAsync();

            foreach (var m in activeModels)
            {
                m.TrangThai = "DEPRECATED";
            }

            // 2. KÃ­ch hoáº¡t model má»›i Ä‘Æ°á»£c chá»n
            selectedModel.TrangThai = "ACTIVE";

            _context.PhienBanMoHinhs.UpdateRange(activeModels);
            _context.PhienBanMoHinhs.Update(selectedModel);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { 
                success = true, 
                message = $"ÄÃ£ kÃ­ch hoáº¡t phiÃªn báº£n v{selectedModel.PhienBan} lÃ m Production Model chÃ­nh!" 
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = "Lá»—i khi kÃ­ch hoáº¡t Model: " + ex.Message });
        }
    }
}
