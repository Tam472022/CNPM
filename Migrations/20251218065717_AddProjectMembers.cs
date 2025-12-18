using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duan_CNPM.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Member2Name",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Member2StudentCode",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Member3Name",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Member3StudentCode",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 13, 57, 13, 209, DateTimeKind.Local).AddTicks(6958));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 13, 57, 13, 209, DateTimeKind.Local).AddTicks(6973));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigID",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 12, 18, 13, 57, 13, 209, DateTimeKind.Local).AddTicks(6974));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 18, 13, 57, 13, 209, DateTimeKind.Local).AddTicks(6210), "$2a$11$5.s2HVlCLe2yxFZwLYS78eFYtst8QcBxPoAy7ND4qE5HIr5ScC.7q" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Member2Name",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Member2StudentCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Member3Name",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Member3StudentCode",
                table: "Projects");

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
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 18, 9, 36, 52, 480, DateTimeKind.Local).AddTicks(3118), "$2a$11$XoIGJzUBIEhiJsu1wNvVv.Nz7WhrA.JUxTR95uNDeaSJsL76.T3um" });
        }
    }
}
