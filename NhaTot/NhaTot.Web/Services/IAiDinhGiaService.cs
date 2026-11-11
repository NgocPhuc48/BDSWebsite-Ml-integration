using NhaTot.Web.Models;

namespace NhaTot.Web.Services
{
    public interface IAiDinhGiaService
    {
        Task<decimal> TinhGiaGoiYAsync(int bdsId);
        Task<object> DinhGiaBatDongSanAsync(DinhGiaInputModel input);
    }
}