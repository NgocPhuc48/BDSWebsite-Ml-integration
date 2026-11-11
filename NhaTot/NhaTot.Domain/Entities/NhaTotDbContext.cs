using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NhaTot.Domain.Entities;

public partial class NhaTotDbContext : DbContext
{
    public NhaTotDbContext()
    {
    }

    public NhaTotDbContext(DbContextOptions<NhaTotDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BaoCaoTinDang> BaoCaoTinDangs { get; set; }

    public virtual DbSet<BatDongSan> BatDongSans { get; set; }

    public virtual DbSet<CauHinhHeThong> CauHinhHeThongs { get; set; }

    public virtual DbSet<ChiTietDat> ChiTietDats { get; set; }

    public virtual DbSet<ChiTietNha> ChiTietNhas { get; set; }

    public virtual DbSet<DuLieuThuThap> DuLieuThuThaps { get; set; }

    public virtual DbSet<KetQuaDanhGiaMoHinh> KetQuaDanhGiaMoHinhs { get; set; }

    public virtual DbSet<LeadKhachHangNhuCau> LeadKhachHangNhuCaus { get; set; }

    public virtual DbSet<LichHen> LichHens { get; set; }

    public virtual DbSet<LichSuDinhGia> LichSuDinhGias { get; set; }

    public virtual DbSet<LichSuGiaTin> LichSuGiaTins { get; set; }

    public virtual DbSet<LichSuGiaoDich> LichSuGiaoDiches { get; set; }

    public virtual DbSet<LichSuXuLyDuLieu> LichSuXuLyDuLieus { get; set; }

    public virtual DbSet<LoaiBd> LoaiBds { get; set; }

    public virtual DbSet<MucTieuTheoDoiGia> MucTieuTheoDoiGias { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NguonDuLieu> NguonDuLieus { get; set; }

    public virtual DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }

    public virtual DbSet<NhuCauTimKiem> NhuCauTimKiems { get; set; }

    public virtual DbSet<PhanCongTuVan> PhanCongTuVans { get; set; }

    public virtual DbSet<PhienBanMoHinh> PhienBanMoHinhs { get; set; }

    public virtual DbSet<PhuongXa> PhuongXas { get; set; }

    public virtual DbSet<QuanHuyen> QuanHuyens { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TinDang> TinDangs { get; set; }

    public virtual DbSet<TinhThanh> TinhThanhs { get; set; }

    public virtual DbSet<ViTaiKhoan> ViTaiKhoans { get; set; }

    public virtual DbSet<YeuCauTuVan> YeuCauTuVans { get; set; }

    public virtual DbSet<YeuThich> YeuThiches { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=NhaTot;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaoCaoTinDang>(entity =>
        {
            entity.HasKey(e => e.MaBaoCao).HasName("PK__BaoCaoTi__25A9188C0613F671");

            entity.ToTable("BaoCaoTinDang");

            entity.Property(e => e.LyDo).HasMaxLength(300);
            entity.Property(e => e.MoTa).HasMaxLength(1000);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiBaoCaoNavigation).WithMany(p => p.BaoCaoTinDangMaNguoiBaoCaoNavigations)
                .HasForeignKey(d => d.MaNguoiBaoCao)
                .HasConstraintName("FK_BaoCaoTinDang_NguoiBaoCao");

            entity.HasOne(d => d.MaNguoiXuLyNavigation).WithMany(p => p.BaoCaoTinDangMaNguoiXuLyNavigations)
                .HasForeignKey(d => d.MaNguoiXuLy)
                .HasConstraintName("FK_BaoCaoTinDang_NguoiXuLy");

            entity.HasOne(d => d.MaTinNavigation).WithMany(p => p.BaoCaoTinDangs)
                .HasForeignKey(d => d.MaTin)
                .HasConstraintName("FK_BaoCaoTinDang_TinDang");
        });

        modelBuilder.Entity<BatDongSan>(entity =>
        {
            entity.HasKey(e => e.MaBds).HasName("PK__BatDongS__352177DB6C6B5CA4");

            entity.ToTable("BatDongSan");

            entity.Property(e => e.MaBds).HasColumnName("MaBDS");
            entity.Property(e => e.DiaChiMoTa).HasMaxLength(300);
            entity.Property(e => e.DienTichDat).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.DienTichSuDung).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Huong)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.KinhDo).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.MaLoaiBds).HasColumnName("MaLoaiBDS");
            entity.Property(e => e.PhapLy).HasMaxLength(100);
            entity.Property(e => e.TenDuAn).HasMaxLength(200);
            entity.Property(e => e.ViDo).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.MaLoaiBdsNavigation).WithMany(p => p.BatDongSans)
                .HasForeignKey(d => d.MaLoaiBds)
                .HasConstraintName("FK_BatDongSan_LoaiBDS");

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.BatDongSans)
                .HasForeignKey(d => d.MaPhuongXa)
                .HasConstraintName("FK_BatDongSan_PhuongXa");
        });

        modelBuilder.Entity<CauHinhHeThong>(entity =>
        {
            entity.HasKey(e => e.MaCauHinh).HasName("PK__CauHinhH__F0685B7DD040D289");

            entity.ToTable("CauHinhHeThong");

            entity.Property(e => e.Khoa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(500);
        });

        modelBuilder.Entity<ChiTietDat>(entity =>
        {
            entity.HasKey(e => e.MaChiTietDat).HasName("PK__ChiTietD__D599ABCA8EEF3BF2");

            entity.ToTable("ChiTietDat");

            entity.Property(e => e.ChieuSauM).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.DoRongDuongM).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.HinhDang).HasMaxLength(80);
            entity.Property(e => e.LoaiDat).HasMaxLength(100);
            entity.Property(e => e.LoaiDuong).HasMaxLength(100);
            entity.Property(e => e.MaBds).HasColumnName("MaBDS");
            entity.Property(e => e.MatTienM).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.MaBdsNavigation).WithMany(p => p.ChiTietDats)
                .HasForeignKey(d => d.MaBds)
                .HasConstraintName("FK_ChiTietDat_BatDongSan");
        });

        modelBuilder.Entity<ChiTietNha>(entity =>
        {
            entity.HasKey(e => e.MaChiTietNha).HasName("PK__ChiTietN__CEC279B0EB11E480");

            entity.ToTable("ChiTietNha");

            entity.Property(e => e.MaBds).HasColumnName("MaBDS");
            entity.Property(e => e.NoiThat).HasMaxLength(100);
            entity.Property(e => e.SoWc).HasColumnName("SoWC");
            entity.Property(e => e.TinhTrangNha).HasMaxLength(100);

            entity.HasOne(d => d.MaBdsNavigation).WithMany(p => p.ChiTietNhas)
                .HasForeignKey(d => d.MaBds)
                .HasConstraintName("FK_ChiTietNha_BatDongSan");
        });

        modelBuilder.Entity<DuLieuThuThap>(entity =>
        {
            entity.HasKey(e => e.MaDuLieu).HasName("PK__DuLieuTh__1859F88D4882FAFE");

            entity.ToTable("DuLieuThuThap");

            entity.Property(e => e.DienTich).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.GiaRaoVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaRaoVND");
            entity.Property(e => e.HashNoiDung)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.LoaiBdsnguon)
                .HasMaxLength(150)
                .HasColumnName("LoaiBDSNguon");
            entity.Property(e => e.MaTinNguon).HasMaxLength(200);
            entity.Property(e => e.PhuongXaNguon).HasMaxLength(150);
            entity.Property(e => e.TieuDe).HasMaxLength(500);
            entity.Property(e => e.TinhNguon).HasMaxLength(150);
            entity.Property(e => e.TrangThaiXuLy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Urltin)
                .HasMaxLength(1500)
                .HasColumnName("URLTin");

            entity.HasOne(d => d.MaNguonNavigation).WithMany(p => p.DuLieuThuThaps)
                .HasForeignKey(d => d.MaNguon)
                .HasConstraintName("FK_DuLieuThuThap_NguonDuLieu");
        });

        modelBuilder.Entity<KetQuaDanhGiaMoHinh>(entity =>
        {
            entity.HasKey(e => e.MaKetQua).HasName("PK__KetQuaDa__D5B3102A571CF98E");

            entity.ToTable("KetQuaDanhGiaMoHinh");

            entity.Property(e => e.GhiChu).HasMaxLength(1000);
            entity.Property(e => e.Mae)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("MAE");
            entity.Property(e => e.R2).HasColumnType("decimal(10, 6)");
            entity.Property(e => e.Rmse)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("RMSE");
            entity.Property(e => e.TapDuLieu)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaPhienBanNavigation).WithMany(p => p.KetQuaDanhGiaMoHinhs)
                .HasForeignKey(d => d.MaPhienBan)
                .HasConstraintName("FK_KetQuaDanhGiaMoHinh_PhienBan");
        });

        modelBuilder.Entity<LeadKhachHangNhuCau>(entity =>
        {
            entity.HasKey(e => e.MaLead).HasName("PK__LeadKhac__7D921B211D0A977E");

            entity.ToTable("LeadKhachHangNhuCau");

            entity.Property(e => e.LinkProfileMxh)
                .HasMaxLength(1000)
                .HasColumnName("LinkProfileMXH");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NguonMxh)
                .HasMaxLength(100)
                .HasColumnName("NguonMXH");
            entity.Property(e => e.NoiDungNhuCau).HasMaxLength(2000);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TenKhachHang).HasMaxLength(150);

            entity.HasOne(d => d.MaDuLieuNavigation).WithMany(p => p.LeadKhachHangNhuCaus)
                .HasForeignKey(d => d.MaDuLieu)
                .HasConstraintName("FK_LeadKhachHangNhuCau_DuLieuThuThap");

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.LeadKhachHangNhuCaus)
                .HasForeignKey(d => d.MaPhuongXa)
                .HasConstraintName("FK_LeadKhachHangNhuCau_PhuongXa");
        });

        modelBuilder.Entity<LichHen>(entity =>
        {
            entity.HasKey(e => e.MaLichHen).HasName("PK__LichHen__150F264FEDE103EC");

            entity.ToTable("LichHen");

            entity.Property(e => e.DiaDiem).HasMaxLength(500);
            entity.Property(e => e.GhiChu).HasMaxLength(1000);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiTaoNavigation).WithMany(p => p.LichHens)
                .HasForeignKey(d => d.MaNguoiTao)
                .HasConstraintName("FK_LichHen_NguoiTao");

            entity.HasOne(d => d.MaTinNavigation).WithMany(p => p.LichHens)
                .HasForeignKey(d => d.MaTin)
                .HasConstraintName("FK_LichHen_TinDang");

            entity.HasOne(d => d.MaYeuCauNavigation).WithMany(p => p.LichHens)
                .HasForeignKey(d => d.MaYeuCau)
                .HasConstraintName("FK_LichHen_YeuCau");
        });

        modelBuilder.Entity<LichSuDinhGia>(entity =>
        {
            entity.ToTable("LichSuDinhGia");
            entity.HasKey(e => e.MaDinhGia).HasName("PK__LichSuDi__D687F58DE10FC057");

            entity.Property(e => e.CanhBao).HasMaxLength(1000);
            entity.Property(e => e.ChieuRongMatTien).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.DienTich).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.DoTinCay).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DuongVaoRong).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.GiaCaoNhatVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaCaoNhatVND");
            entity.Property(e => e.GiaDuDoanVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaDuDoanVND");
            entity.Property(e => e.GiaThapNhatVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaThapNhatVND");
            entity.Property(e => e.Huong)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.KinhDo).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.MaBds).HasColumnName("MaBDS");
            entity.Property(e => e.MaLoaiBds).HasColumnName("MaLoaiBDS");
            entity.Property(e => e.PhapLy).HasMaxLength(100);
            entity.Property(e => e.ViDo).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.MaBdsNavigation).WithMany(p => p.LichSuDinhGia)
                .HasForeignKey(d => d.MaBds)
                .HasConstraintName("FK_LichSuDinhGia_BatDongSan");

            entity.HasOne(d => d.MaLoaiBdsNavigation).WithMany(p => p.LichSuDinhGia)
                .HasForeignKey(d => d.MaLoaiBds)
                .HasConstraintName("FK_LichSuDinhGia_LoaiBDS");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.LichSuDinhGia)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_LichSuDinhGia_NguoiDung");

            entity.HasOne(d => d.MaPhienBanNavigation).WithMany(p => p.LichSuDinhGia)
                .HasForeignKey(d => d.MaPhienBan)
                .HasConstraintName("FK_LichSuDinhGia_PhienBan");

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.LichSuDinhGia)
                .HasForeignKey(d => d.MaPhuongXa)
                .HasConstraintName("FK_LichSuDinhGia_PhuongXa");
        });

        modelBuilder.Entity<LichSuGiaTin>(entity =>
        {
            entity.HasKey(e => e.MaLichSu).HasName("PK__LichSuGi__C443222AB6043311");

            entity.ToTable("LichSuGiaTin");

            entity.Property(e => e.GiaCuVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaCuVND");
            entity.Property(e => e.GiaMoiVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaMoiVND");

            entity.HasOne(d => d.MaTinNavigation).WithMany(p => p.LichSuGiaTins)
                .HasForeignKey(d => d.MaTin)
                .HasConstraintName("FK_LichSuGiaTin_TinDang");

            entity.HasOne(d => d.NguoiThayDoiNavigation).WithMany(p => p.LichSuGiaTins)
                .HasForeignKey(d => d.NguoiThayDoi)
                .HasConstraintName("FK_LichSuGiaTin_NguoiDung");
        });

        modelBuilder.Entity<LichSuGiaoDich>(entity =>
        {
            entity.HasKey(e => e.MaGiaoDich).HasName("PK__LichSuGi__0A2A24EB8C075EEE");

            entity.ToTable("LichSuGiaoDich");

            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.SoDiem).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoDuSauGiaoDich).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaViNavigation).WithMany(p => p.LichSuGiaoDiches)
                .HasForeignKey(d => d.MaVi)
                .HasConstraintName("FK_LichSuGiaoDich_ViTaiKhoan");
        });

        modelBuilder.Entity<LichSuXuLyDuLieu>(entity =>
        {
            entity.HasKey(e => e.MaXuLy).HasName("PK__LichSuXu__9136894E85652364");

            entity.ToTable("LichSuXuLyDuLieu");

            entity.Property(e => e.BuocXuLy).HasMaxLength(100);
            entity.Property(e => e.ChiTiet).HasMaxLength(1000);
            entity.Property(e => e.KetQua)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaDuLieuNavigation).WithMany(p => p.LichSuXuLyDuLieus)
                .HasForeignKey(d => d.MaDuLieu)
                .HasConstraintName("FK_LichSuXuLyDuLieu_DuLieuThuThap");
        });

        modelBuilder.Entity<LoaiBd>(entity =>
        {
            entity.HasKey(e => e.MaLoaiBds).HasName("PK__LoaiBDS__9676B22DC1D57352");

            entity.ToTable("LoaiBDS");

            entity.Property(e => e.MaLoaiBds).HasColumnName("MaLoaiBDS");
            entity.Property(e => e.MoTa).HasMaxLength(300);
            entity.Property(e => e.TenLoaiBds)
                .HasMaxLength(80)
                .HasColumnName("TenLoaiBDS");
        });

        modelBuilder.Entity<MucTieuTheoDoiGia>(entity =>
        {
            entity.ToTable("MucTieuTheoDoiGia");
            entity.HasKey(e => e.MaMucTieu).HasName("PK__MucTieuT__E587A3292743F788");

            entity.Property(e => e.GiaMongMuonVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaMongMuonVND");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiBds).HasColumnName("MaLoaiBDS");
            entity.Property(e => e.TienCocDiem).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaLoaiBdsNavigation).WithMany(p => p.MucTieuTheoDoiGia)
                .HasForeignKey(d => d.MaLoaiBds)
                .HasConstraintName("FK_MucTieuTheoDoiGia_LoaiBDS");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.MucTieuTheoDoiGia)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_MucTieuTheoDoiGia_NguoiDung");

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.MucTieuTheoDoiGia)
                .HasForeignKey(d => d.MaPhuongXa)
                .HasConstraintName("FK_MucTieuTheoDoiGia_PhuongXa");

            entity.HasOne(d => d.MaQuanHuyenNavigation).WithMany(p => p.MucTieuTheoDoiGia)
                .HasForeignKey(d => d.MaQuanHuyen)
                .HasConstraintName("FK_MucTieuTheoDoiGia_QuanHuyen");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D7623E4ADB93");

            entity.ToTable("NguoiDung");

            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.HashMatKhau).HasMaxLength(255);
            entity.Property(e => e.HoTen).HasMaxLength(150);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NguonDuLieu>(entity =>
        {
            entity.HasKey(e => e.MaNguon).HasName("PK__NguonDuL__ABEE19F856A8065D");

            entity.ToTable("NguonDuLieu");

            entity.Property(e => e.DiaChiWebsite).HasMaxLength(1000);
            entity.Property(e => e.GhiChuGiayPhep).HasMaxLength(1000);
            entity.Property(e => e.PhamViSuDung).HasMaxLength(500);
            entity.Property(e => e.TenNguon).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NhatKyHeThong>(entity =>
        {
            entity.HasKey(e => e.MaNhatKy).HasName("PK__NhatKyHe__E42EF42E5A08D972");

            entity.ToTable("NhatKyHeThong");

            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("DiaChiIP");
            entity.Property(e => e.DoiTuong).HasMaxLength(100);
            entity.Property(e => e.HanhDong).HasMaxLength(150);
            entity.Property(e => e.MaDoiTuong).HasMaxLength(100);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.NhatKyHeThongs)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_NhatKyHeThong_NguoiDung");
        });

        modelBuilder.Entity<NhuCauTimKiem>(entity =>
        {
            entity.HasKey(e => e.MaNhuCau).HasName("PK__NhuCauTi__A137B963CA35F44B");

            entity.ToTable("NhuCauTimKiem");

            entity.Property(e => e.DienTichMax).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.DienTichMin).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiBds).HasColumnName("MaLoaiBDS");
            entity.Property(e => e.MucDich).HasMaxLength(100);
            entity.Property(e => e.NganSachMaxVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("NganSachMaxVND");
            entity.Property(e => e.NganSachMinVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("NganSachMinVND");
            entity.Property(e => e.TieuChiThem).HasMaxLength(1000);

            entity.HasOne(d => d.MaLoaiBdsNavigation).WithMany(p => p.NhuCauTimKiems)
                .HasForeignKey(d => d.MaLoaiBds)
                .HasConstraintName("FK_NhuCauTimKiem_LoaiBDS");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.NhuCauTimKiems)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_NhuCauTimKiem_NguoiDung");

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.NhuCauTimKiems)
                .HasForeignKey(d => d.MaPhuongXa)
                .HasConstraintName("FK_NhuCauTimKiem_PhuongXa");
        });

        modelBuilder.Entity<PhanCongTuVan>(entity =>
        {
            entity.HasKey(e => e.MaPhanCong).HasName("PK__PhanCong__C279D916B0ED7B37");

            entity.ToTable("PhanCongTuVan");

            entity.Property(e => e.GhiChu).HasMaxLength(500);

            entity.HasOne(d => d.MaNguoiPhanCongNavigation).WithMany(p => p.PhanCongTuVanMaNguoiPhanCongNavigations)
                .HasForeignKey(d => d.MaNguoiPhanCong)
                .HasConstraintName("FK_PhanCongTuVan_NguoiPhanCong");

            entity.HasOne(d => d.MaTuVanVienNavigation).WithMany(p => p.PhanCongTuVanMaTuVanVienNavigations)
                .HasForeignKey(d => d.MaTuVanVien)
                .HasConstraintName("FK_PhanCongTuVan_TuVanVien");

            entity.HasOne(d => d.MaYeuCauNavigation).WithMany(p => p.PhanCongTuVans)
                .HasForeignKey(d => d.MaYeuCau)
                .HasConstraintName("FK_PhanCongTuVan_YeuCau");
        });

        modelBuilder.Entity<PhienBanMoHinh>(entity =>
        {
            entity.HasKey(e => e.MaPhienBan).HasName("PK__PhienBan__DB433AD444191B9A");

            entity.ToTable("PhienBanMoHinh");

            entity.Property(e => e.DuongDanArtifact).HasMaxLength(1000);
            entity.Property(e => e.GhiChu).HasMaxLength(1000);
            entity.Property(e => e.LoaiBdsapDung).HasColumnName("LoaiBDSApDung");
            entity.Property(e => e.PhienBan)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TenMoHinh).HasMaxLength(100);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PhuongXa>(entity =>
        {
            entity.HasKey(e => e.MaPhuongXa).HasName("PK__PhuongXa__55F1EFA98043B425");

            entity.ToTable("PhuongXa");

            entity.Property(e => e.MaHanhChinh)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TenPhuongXa).HasMaxLength(120);
            entity.Property(e => e.ViDo).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.KinhDo).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.MaQuanHuyenNavigation).WithMany(p => p.PhuongXas)
                .HasForeignKey(d => d.MaQuanHuyen)
                .HasConstraintName("FK_PhuongXa_QuanHuyen");
        });

        modelBuilder.Entity<QuanHuyen>(entity =>
        {
            entity.HasKey(e => e.MaQuanHuyen).HasName("PK__QuanHuye__B86B827AEAA34824");

            entity.ToTable("QuanHuyen");

            entity.Property(e => e.MaHanhChinh)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TenQuanHuyen).HasMaxLength(120);

            entity.HasOne(d => d.MaTinhNavigation).WithMany(p => p.QuanHuyens)
                .HasForeignKey(d => d.MaTinh)
                .HasConstraintName("FK_QuanHuyen_TinhThanh");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54EEEC7059D");

            entity.ToTable("ThongBao");

            entity.Property(e => e.LoaiThongBao)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NoiDung).HasMaxLength(1000);
            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_ThongBao_NguoiDung");
        });

        modelBuilder.Entity<TinDang>(entity =>
        {
            entity.HasKey(e => e.MaTin).HasName("PK__TinDang__3149033500787549");

            entity.ToTable("TinDang");

            entity.Property(e => e.GiaRaoVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaRaoVND");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LyDoTuChoi).HasMaxLength(500);
            entity.Property(e => e.MaBds).HasColumnName("MaBDS");
            entity.Property(e => e.TienCocVnd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TienCocVND");
            entity.Property(e => e.TieuDe).HasMaxLength(250);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaBdsNavigation).WithMany(p => p.TinDangs)
                .HasForeignKey(d => d.MaBds)
                .HasConstraintName("FK_TinDang_BatDongSan");

            entity.HasOne(d => d.MaNguoiDangNavigation).WithMany(p => p.TinDangMaNguoiDangNavigations)
                .HasForeignKey(d => d.MaNguoiDang)
                .HasConstraintName("FK_TinDang_NguoiDang");

            entity.HasOne(d => d.MaNguoiDuyetNavigation).WithMany(p => p.TinDangMaNguoiDuyetNavigations)
                .HasForeignKey(d => d.MaNguoiDuyet)
                .HasConstraintName("FK_TinDang_NguoiDuyet");
        });

        modelBuilder.Entity<TinhThanh>(entity =>
        {
            entity.HasKey(e => e.MaTinh).HasName("PK__TinhThan__4CC54480D0704594");

            entity.ToTable("TinhThanh");

            entity.Property(e => e.MaHanhChinh)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TenTinh).HasMaxLength(100);
        });

        modelBuilder.Entity<ViTaiKhoan>(entity =>
        {
            entity.HasKey(e => e.MaVi).HasName("PK__ViTaiKho__272510137A8B4FB1");

            entity.ToTable("ViTaiKhoan");

            entity.Property(e => e.SoDuDiem).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ViTaiKhoans)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_ViTaiKhoan_NguoiDung");
        });

        modelBuilder.Entity<YeuCauTuVan>(entity =>
        {
            entity.HasKey(e => e.MaYeuCau).HasName("PK__YeuCauTu__CFA5DF4E6F5B95DC");

            entity.ToTable("YeuCauTuVan");

            entity.Property(e => e.NoiDung).HasMaxLength(2000);
            entity.Property(e => e.TieuDe).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNguoiGuiNavigation).WithMany(p => p.YeuCauTuVans)
                .HasForeignKey(d => d.MaNguoiGui)
                .HasConstraintName("FK_YeuCauTuVan_NguoiGui");

            entity.HasOne(d => d.MaNhuCauNavigation).WithMany(p => p.YeuCauTuVans)
                .HasForeignKey(d => d.MaNhuCau)
                .HasConstraintName("FK_YeuCauTuVan_NhuCau");
        });

        modelBuilder.Entity<YeuThich>(entity =>
        {
            entity.HasKey(e => e.MaYeuThich).HasName("PK__YeuThich__B9007E4C569F0EE4");

            entity.ToTable("YeuThich");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.YeuThiches)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_YeuThich_NguoiDung");

            entity.HasOne(d => d.MaTinNavigation).WithMany(p => p.YeuThiches)
                .HasForeignKey(d => d.MaTin)
                .HasConstraintName("FK_YeuThich_TinDang");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
