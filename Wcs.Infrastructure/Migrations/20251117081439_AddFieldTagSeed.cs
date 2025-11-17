using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wcs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldTagSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeviceCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Args = table.Column<string>(type: "TEXT", nullable: true),
                    RequestId = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldTags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    DataType = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<ushort>(type: "INTEGER", nullable: false),
                    BitIndex = table.Column<ushort>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<string>(type: "TEXT", nullable: true),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldTags", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FieldTags",
                columns: new[] { "Id", "Address", "BitIndex", "DataType", "Description", "DeviceId", "Direction", "EquipmentId", "PropertyName" },
                values: new object[,]
                {
                    { "CV01_FAULT", (ushort)0, null, 1, "컨베이어1 Fault 상태", "PLC01", 0, "CV01", "HasFault" },
                    { "CV01_PE_IN", (ushort)1, null, 1, "컨베이어1 입구 포토센서", "PLC01", 0, "CV01", "IsBlocked" },
                    { "CV01_RUN_FB", (ushort)0, null, 0, "컨베이어1 구동 피드백", "PLC01", 0, "CV01", "IsRunning" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "FieldTags");
        }
    }
}
