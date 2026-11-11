using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LichSuGiaTin
{
    public long MaLichSu { get; set; }

    public long? MaTin { get; set; }

    public decimal? GiaCuVnd { get; set; }

    public decimal? GiaMoiVnd { get; set; }

    public DateTime? ThoiDiem { get; set; }

    public long? NguoiThayDoi { get; set; }

    public virtual TinDang? MaTinNavigation { get; set; }

    public virtual NguoiDung? NguoiThayDoiNavigation { get; set; }
}
