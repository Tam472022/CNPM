using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duan_CNPM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 9, 36, 52, 480, DateTimeKind.Local).AddTicks(3913));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 9, 36, 52, 480, DateTimeKind.Local).AddTicks(3925));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 9, 36, 52, 480, DateTimeKind.Local).AddTicks(3926));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "EmailConfirmed", "EmailVerificationExpiry", "EmailVerificationToken", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 18, 9, 36, 52, 480, DateTimeKind.Local).AddTicks(3118), false, null, null, "$2a$11$XoIGJzUBIEhiJsu1wNvVv.Nz7WhrA.JUxTR95uNDeaSJsL76.T3um" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 13, 16, 25, 19, 676, DateTimeKind.Local).AddTicks(825));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 13, 16, 25, 19, 676, DateTimeKind.Local).AddTicks(1043));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 13, 16, 25, 19, 676, DateTimeKind.Local).AddTicks(1045));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 13, 16, 25, 19, 675, DateTimeKind.Local).AddTicks(8591), "$2a$11$jK/xUi9LFbnsjzZ0Jsfftu6xZhQGE9iO6YGS578T5Nhf9kgOsP1y2" });
        }
    }
}
