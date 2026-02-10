using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WdAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOffsetToMilliseconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffsetMinutes",
                table: "AlarmDelayActions");

            migrationBuilder.AddColumn<long>(
                name: "OffsetMilliseconds",
                table: "AlarmDelayActions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffsetMilliseconds",
                table: "AlarmDelayActions");

            migrationBuilder.AddColumn<int>(
                name: "OffsetMinutes",
                table: "AlarmDelayActions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
