using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duan_CNPM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationLinkNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Link",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 11, 13, 44, 646, DateTimeKind.Local).AddTicks(819));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 11, 13, 44, 646, DateTimeKind.Local).AddTicks(830));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 11, 13, 44, 646, DateTimeKind.Local).AddTicks(832));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 11, 11, 13, 44, 646, DateTimeKind.Local).AddTicks(83), "$2a$11$6FhnDwsAT6xifh2FgGQAXudYChHjNfnO09ejbNt9tU9NfphT8FuzW" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Link",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 9, 39, 59, 340, DateTimeKind.Local).AddTicks(5430));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 9, 39, 59, 340, DateTimeKind.Local).AddTicks(5443));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 11, 11, 9, 39, 59, 340, DateTimeKind.Local).AddTicks(5444));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 11, 9, 39, 59, 340, DateTimeKind.Local).AddTicks(4810), "$2a$11$opNGXGyaz6xIC/5HJRaGzOypz9Z1AyaPMK/hZCnUmjv4apSXBg/Lm" });
        }
    }
}
