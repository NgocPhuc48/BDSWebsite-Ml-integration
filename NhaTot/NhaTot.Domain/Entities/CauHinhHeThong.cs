using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class CauHinhHeThong
{
    public int MaCauHinh { get; set; }

    public string? Khoa { get; set; }

    public string? GiaTri { get; set; }

    public string? MoTa { get; set; }

    public DateTime? NgayCapNhat { get; set; }
}
