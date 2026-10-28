using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    public partial class ThemCotThieu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHetHan",
                table: "TinBan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TinCuaCongTy",
                table: "TinBan",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhongBan",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "PhieuXemNhaBatDongSan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "PhieuXemNha",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "PhieuXemNha",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SoDu",
                table: "NguoiDung",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayChot",
                table: "HopDongMuaBatDongSan",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "HoaDonNapTien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoTien = table.Column<long>(type: "bigint", nullable: false),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaThanhToan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDonNapTien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoaDonNapTien_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuenMatKhau",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<int>(type: "int", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuenMatKhau", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoaDonNapTien_NguoiDungId",
                table: "HoaDonNapTien",
                column: "NguoiDungId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoaDonNapTien");

            migrationBuilder.DropTable(
                name: "QuenMatKhau");

            migrationBuilder.DropColumn(
                name: "NgayHetHan",
                table: "TinBan");

            migrationBuilder.DropColumn(
                name: "TinCuaCongTy",
                table: "TinBan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhongBan");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "PhieuXemNhaBatDongSan");

            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "PhieuXemNha");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "PhieuXemNha");

            migrationBuilder.DropColumn(
                name: "SoDu",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayChot",
                table: "HopDongMuaBatDongSan");
        }
    }
}
