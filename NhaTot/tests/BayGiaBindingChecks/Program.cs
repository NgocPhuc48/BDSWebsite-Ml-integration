using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NhaTot.Web.Controllers;
using NhaTot.Web.Models;

var builder = WebApplication.CreateBuilder();
builder.Services.AddControllersWithViews();
using var provider = builder.Services.BuildServiceProvider();
var parameter = typeof(BayGiaController).GetMethod("TaoMucTieu")!.GetParameters()[0];
var binding = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes(false))!;
if (binding.BinderModelName != "FormTao") throw new Exception("Action prefix is incorrect");
// vi-VN chạy trước để phát hiện lỗi parse giới hạn ngay lần validation đầu tiên.
foreach (var culture in new[] { "vi-VN", "en-US" })
{
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
foreach (var price in new[] { "1000000000", "0", "abc" })
{
    using var scope = provider.CreateScope();
    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    var values = new Dictionary<string, StringValues> {
        ["FormTao.MaLoaiBds"] = "1", ["FormTao.MaTinh"] = "1", ["FormTao.MaQuanHuyen"] = "2",
        ["FormTao.NganSachMaxVnd"] = price, ["FormTao.DienTichMin"] = "80",
        ["FormTao.SoPhongNgu"] = "3", ["FormTao.PhapLy"] = "Sổ đỏ", ["FormTao.LoaiGiaoDich"] = "BAN"
    };
    var metadata = scope.ServiceProvider.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(TaoMucTieuBayGiaViewModel));
    var binder = scope.ServiceProvider.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, BindingInfo = binding, CacheToken = parameter });
    var context = DefaultModelBindingContext.CreateBindingContext(action,
        new FormValueProvider(BindingSource.Form, new FormCollection(values), CultureInfo.InvariantCulture), metadata, binding, "FormTao");
    await binder.BindModelAsync(context);
    var model = (TaoMucTieuBayGiaViewModel)context.Result.Model!;
    scope.ServiceProvider.GetRequiredService<IObjectModelValidator>().Validate(action, null, "FormTao", model);
    if (price == "1000000000")
    {
        if (!action.ModelState.IsValid || model.NganSachMaxVnd != 1000000000m || model.DienTichMin != 80 || model.SoPhongNgu != 3 || model.PhapLy != "Sổ đỏ")
            throw new Exception("Valid prefixed form lost values or failed validation");
    }
    else if (action.ModelState.IsValid) throw new Exception("Invalid price accepted");
}
foreach (var type in new[] { typeof(TaoMucTieuBayGiaViewModel), typeof(NhaTot.Web.Areas.Admin.Models.CrawlReviewModel) })
foreach (var property in type.GetProperties())
foreach (var range in property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), false)
    .Cast<System.ComponentModel.DataAnnotations.RangeAttribute>().Where(x => x.OperandType == typeof(decimal)))
{
    var min = decimal.Parse((string)range.Minimum, CultureInfo.InvariantCulture);
    var max = decimal.Parse((string)range.Maximum, CultureInfo.InvariantCulture);
    if (!range.IsValid(min) || !range.IsValid(max) || range.IsValid(min - .01m) || range.IsValid(max + .01m))
        throw new Exception($"Decimal validation bounds failed for {type.Name}.{property.Name} under {culture}");
}
}
Console.WriteLine("Passed vi-VN/en-US: MVC binding of 1 billion and full criteria; invalid prices rejected; decimal boundaries verified for BayGia and Admin review. No wallet changes.");
