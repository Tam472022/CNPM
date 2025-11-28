using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duan_CNPM.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessorComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProfessorComment",
                table: "Projects",
                type: "ntext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "ntext");

            migrationBuilder.CreateTable(
                name: "ProfessorComments",
                columns: table => new
                {
                    CommentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    ProfessorID = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "ntext", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessorComments", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_ProfessorComments_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfessorComments_Users_ProfessorID",
                        column: x => x.ProfessorID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorComments_ProfessorID",
                table: "ProfessorComments",
                column: "ProfessorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorComments_ProjectID",
                table: "ProfessorComments",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessorComments");

            migrationBuilder.AlterColumn<string>(
                name: "ProfessorComment",
                table: "Projects",
                type: "ntext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "ntext",
                oldNullable: true);

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
    }
}
