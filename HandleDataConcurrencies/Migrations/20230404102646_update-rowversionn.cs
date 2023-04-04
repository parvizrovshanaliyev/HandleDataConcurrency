using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleDataConcurrencies.Migrations
{
    /// <inheritdoc />
    public partial class updaterowversionn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Version",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Version",
                table: "Payments",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
