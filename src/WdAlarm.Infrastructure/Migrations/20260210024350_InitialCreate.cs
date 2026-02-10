using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WdAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DelayType = table.Column<string>(type: "text", nullable: false),
                    TimeoutDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllowPingNoTimerReset = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationMethod = table.Column<string>(type: "text", nullable: true),
                    VerificationConfig = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlarmDelayActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    OffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActionConfig = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmDelayActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmDelayActions_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlarmPingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActionConfig = table.Column<string>(type: "jsonb", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmPingActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmPingActions_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmDelayActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlarmPingActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlarmCycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ScheduledTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExecutedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionExecutions_AlarmDelayActions_AlarmDelayActionId",
                        column: x => x.AlarmDelayActionId,
                        principalTable: "AlarmDelayActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionExecutions_AlarmPingActions_AlarmPingActionId",
                        column: x => x.AlarmPingActionId,
                        principalTable: "AlarmPingActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlarmCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmPointTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedByPingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmCycles_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmCycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VerificationProof = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VerificationResult = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResetTimerRequested = table.Column<bool>(type: "boolean", nullable: false),
                    TimerWasReset = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pings_AlarmCycles_AlarmCycleId",
                        column: x => x.AlarmCycleId,
                        principalTable: "AlarmCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pings_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_AlarmCycleId",
                table: "ActionExecutions",
                column: "AlarmCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_AlarmDelayActionId",
                table: "ActionExecutions",
                column: "AlarmDelayActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_AlarmPingActionId",
                table: "ActionExecutions",
                column: "AlarmPingActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_PingId",
                table: "ActionExecutions",
                column: "PingId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_ScheduledTime",
                table: "ActionExecutions",
                column: "ScheduledTime");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_Status",
                table: "ActionExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActionExecutions_Status_ScheduledTime",
                table: "ActionExecutions",
                columns: new[] { "Status", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmCycles_AlarmId",
                table: "AlarmCycles",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmCycles_AlarmPointTime",
                table: "AlarmCycles",
                column: "AlarmPointTime");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmCycles_CompletedByPingId",
                table: "AlarmCycles",
                column: "CompletedByPingId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmCycles_Status",
                table: "AlarmCycles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDelayActions_AlarmId",
                table: "AlarmDelayActions",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmPingActions_AlarmId",
                table: "AlarmPingActions",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsActive",
                table: "Alarms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_UserId",
                table: "Alarms",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Pings_AlarmCycleId",
                table: "Pings",
                column: "AlarmCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Pings_AlarmId",
                table: "Pings",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Pings_ReceivedAt",
                table: "Pings",
                column: "ReceivedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionExecutions_AlarmCycles_AlarmCycleId",
                table: "ActionExecutions",
                column: "AlarmCycleId",
                principalTable: "AlarmCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionExecutions_Pings_PingId",
                table: "ActionExecutions",
                column: "PingId",
                principalTable: "Pings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AlarmCycles_Pings_CompletedByPingId",
                table: "AlarmCycles",
                column: "CompletedByPingId",
                principalTable: "Pings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pings_AlarmCycles_AlarmCycleId",
                table: "Pings");

            migrationBuilder.DropTable(
                name: "ActionExecutions");

            migrationBuilder.DropTable(
                name: "AlarmDelayActions");

            migrationBuilder.DropTable(
                name: "AlarmPingActions");

            migrationBuilder.DropTable(
                name: "AlarmCycles");

            migrationBuilder.DropTable(
                name: "Pings");

            migrationBuilder.DropTable(
                name: "Alarms");
        }
    }
}
