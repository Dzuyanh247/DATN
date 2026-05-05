using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

// Migration mẫu cho phase 1. Khi có .NET SDK, chạy lệnh add/update migration để EF quản lý tự động.
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Đã dùng EnsureCreated cho bản demo chạy nhanh.
        // TODO: thay bằng các lệnh CreateTable khi phát triển chính thức.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
