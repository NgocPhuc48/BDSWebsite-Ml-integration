using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class YeuThich
{
    public long MaYeuThich { get; set; }

    public long? MaNguoiDung { get; set; }

    public long? MaTin { get; set; }

    public DateTime? NgayLuu { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual TinDang? MaTinNavigation { get; set; }
}
