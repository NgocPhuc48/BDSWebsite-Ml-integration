using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using NhaTot.Web.Areas.Admin.Models;
using NhaTot.Web.Services;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    [HttpGet]
    public async Task<IActionResult> KetNoi([FromServices] ApifyTokenStore tokens)
    {
        var model = new ApifyConnectionModel();
        try { model.HasToken = !string.IsNullOrWhiteSpace(await tokens.GetAsync()); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KetNoi(ApifyConnectionModel model, [FromServices] ApifyTokenStore tokens,
        [FromServices] IHttpClientFactory clients)
    {
        // Không trả lại token qua HTML hoặc ModelState, kể cả khi kết nối thất bại.
        var token = (model.Token ?? "").Trim();
        model.Token = "";
        ModelState.Clear();
        if (token.Length is < 10 or > 512 || token.Any(char.IsWhiteSpace) || token.Any(char.IsControl))
            ModelState.AddModelError("", "Vui lòng nhập token Apify hợp lệ.");
        else
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.apify.com/v2/users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await clients.CreateClient("ApifyConnectionCheck").SendAsync(request, HttpContext.RequestAborted);
                if (!response.IsSuccessStatusCode)
                    ModelState.AddModelError("", "Apify chưa chấp nhận kết nối. Kiểm tra token và quyền đọc thông tin tài khoản.");
                else
                {
                    await tokens.SaveAsync(token, HttpContext.RequestAborted);
                    TempData["ConnectionMessage"] = "Đã kết nối và lưu token. Bạn có thể thêm nguồn rồi bắt đầu cào, không cần khởi động lại ứng dụng.";
                    return RedirectToAction(nameof(KetNoi));
                }
            }
            catch (HttpRequestException) { ModelState.AddModelError("", "Không kết nối được Apify. Kiểm tra mạng rồi thử lại."); }
            catch (TaskCanceledException) { ModelState.AddModelError("", "Kết nối Apify quá thời gian. Vui lòng thử lại."); }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or Microsoft.EntityFrameworkCore.DbUpdateException
                or UnauthorizedAccessException or IOException)
            { ModelState.AddModelError("", "Không lưu được token. Kiểm tra quyền ghi dữ liệu và khóa mã hóa trên server."); }
        }
        try { model.HasToken = !string.IsNullOrWhiteSpace(await tokens.GetAsync()); }
        catch (InvalidOperationException) { model.HasToken = false; }
        return View(model);
    }
}
