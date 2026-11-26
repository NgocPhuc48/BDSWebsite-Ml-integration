using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NhaTot.Web.Areas.Admin.Models;

public sealed class CrawlReviewModel
{
    public long Id { get; set; }
    [Required, StringLength(500)] public string Title { get; set; } = "";
    [StringLength(200)] public string? ExactPrice { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? PriceMin { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? PriceMax { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? Area { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? Province { get; set; }
    public int? WardId { get; set; }
    [Required] public string TransactionType { get; set; } = "BAN";
    [Required] public string ContentType { get; set; } = "OFFER";
    public string? RawContent { get; set; }
    public string? OriginalUrl { get; set; }
    public string? ExtractedLocation { get; set; }
    public string? ExtractedType { get; set; }
    public string? Status { get; set; }
    public List<SelectListItem> PropertyTypes { get; set; } = [];
    public List<SelectListItem> Wards { get; set; } = [];
}
