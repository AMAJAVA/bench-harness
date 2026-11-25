using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamLeave.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsTracked = table.Column<bool>(type: "bit", nullable: false),
                    AllowsRetroactive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SecurityStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FirstFailedLoginAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    LockoutEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Deactivation", "(IsActive = 1 AND DeactivatedAtUtc IS NULL) OR (IsActive = 0 AND DeactivatedAtUtc IS NOT NULL)");
                    table.CheckConstraint("CK_Users_FailedLoginCount", "FailedLoginCount >= 0");
                    table.CheckConstraint("CK_Users_NotOwnManager", "ManagerId IS NULL OR ManagerId <> Id");
                    table.ForeignKey(
                        name: "FK_Users_Manager",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_Author",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountHundredths = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Balances", x => x.Id);
                    table.CheckConstraint("CK_Balances_Amount_NonNegative", "AmountHundredths >= 0");
                    table.ForeignKey(
                        name: "FK_Balances_LeaveTypes",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Balances_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysCount = table.Column<int>(type: "int", nullable: false),
                    RequesterComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    SubmissionOverlapAlert = table.Column<bool>(type: "bit", nullable: false),
                    SubmissionOverlapMaxAbsent = table.Column<int>(type: "int", nullable: false),
                    SubmissionOverlapTeamSize = table.Column<int>(type: "int", nullable: false),
                    DecidedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OverlapAcknowledgedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverlapAcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    OverlapAcknowledgedAbsent = table.Column<int>(type: "int", nullable: true),
                    OverlapAcknowledgedTeamSize = table.Column<int>(type: "int", nullable: true),
                    CancelledById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequests_Acknowledgement", "(OverlapAcknowledgedById IS NULL AND OverlapAcknowledgedAtUtc IS NULL AND OverlapAcknowledgedAbsent IS NULL AND OverlapAcknowledgedTeamSize IS NULL) OR (OverlapAcknowledgedById IS NOT NULL AND OverlapAcknowledgedAtUtc IS NOT NULL AND OverlapAcknowledgedTeamSize > 0 AND OverlapAcknowledgedAbsent >= 0 AND OverlapAcknowledgedAbsent <= OverlapAcknowledgedTeamSize)");
                    table.CheckConstraint("CK_LeaveRequests_Cancellation", "(Status = 'Cancelled' AND CancelledAtUtc IS NOT NULL) OR (Status <> 'Cancelled' AND CancelledAtUtc IS NULL AND CancelledById IS NULL)");
                    table.CheckConstraint("CK_LeaveRequests_Dates", "EndDate >= StartDate");
                    table.CheckConstraint("CK_LeaveRequests_DaysCount", "DaysCount >= 1");
                    table.CheckConstraint("CK_LeaveRequests_Decision", "(Status = 'Pending' AND DecidedById IS NULL AND DecidedAtUtc IS NULL) OR (Status IN ('Approved', 'Rejected') AND DecidedById IS NOT NULL AND DecidedAtUtc IS NOT NULL) OR (Status = 'Cancelled')");
                    table.CheckConstraint("CK_LeaveRequests_NoSelfDecision", "DecidedById IS NULL OR DecidedById <> RequesterId");
                    table.CheckConstraint("CK_LeaveRequests_RejectionComment", "Status <> 'Rejected' OR DecisionComment IS NOT NULL");
                    table.CheckConstraint("CK_LeaveRequests_Status", "Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
                    table.CheckConstraint("CK_LeaveRequests_SubmissionOverlap", "SubmissionOverlapTeamSize >= 0 AND SubmissionOverlapMaxAbsent >= 0 AND SubmissionOverlapMaxAbsent <= SubmissionOverlapTeamSize");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_AcknowledgedBy",
                        column: x => x.OverlapAcknowledgedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_CancelledBy",
                        column: x => x.CancelledById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_DecidedBy",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Requester",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.Role });
                    table.CheckConstraint("CK_UserRoles_Role", "Role IN ('Employee', 'Manager', 'HrAdmin')");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BalanceMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    QuantityHundredths = table.Column<int>(type: "int", nullable: false),
                    BalanceAfterHundredths = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceMovements", x => x.Id);
                    table.CheckConstraint("CK_BalanceMovements_BalanceAfter", "BalanceAfterHundredths >= 0");
                    table.CheckConstraint("CK_BalanceMovements_Kind", "Kind IN ('Allocation', 'Adjustment', 'Debit', 'Recredit')");
                    table.CheckConstraint("CK_BalanceMovements_Links", "(Kind IN ('Allocation', 'Adjustment') AND Reason IS NOT NULL AND LeaveRequestId IS NULL) OR (Kind IN ('Debit', 'Recredit') AND LeaveRequestId IS NOT NULL)");
                    table.CheckConstraint("CK_BalanceMovements_Quantity_NonZero", "QuantityHundredths <> 0");
                    table.CheckConstraint("CK_BalanceMovements_Sign", "(Kind = 'Allocation' AND QuantityHundredths > 0) OR (Kind = 'Adjustment') OR (Kind = 'Debit' AND QuantityHundredths < 0) OR (Kind = 'Recredit' AND QuantityHundredths > 0)");
                    table.ForeignKey(
                        name: "FK_BalanceMovements_Author",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BalanceMovements_Balances",
                        column: x => x.BalanceId,
                        principalTable: "Balances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BalanceMovements_LeaveRequests",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestHistory", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequestHistory_FromStatus", "FromStatus IS NULL OR FromStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
                    table.CheckConstraint("CK_LeaveRequestHistory_ToStatus", "ToStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_LeaveRequestHistory_Author",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequestHistory_LeaveRequests",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_Attempts", "Attempts >= 0 AND Attempts <= 4");
                    table.CheckConstraint("CK_Notifications_EventType", "EventType IN ('Submission', 'Approval', 'Rejection', 'ApprovedCancellation')");
                    table.CheckConstraint("CK_Notifications_Queued", "Status <> 'Queued' OR NextAttemptAtUtc IS NOT NULL");
                    table.CheckConstraint("CK_Notifications_Sent", "(Status = 'Sent' AND SentAtUtc IS NOT NULL) OR (Status <> 'Sent' AND SentAtUtc IS NULL)");
                    table.CheckConstraint("CK_Notifications_Status", "Status IN ('Queued', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_Notifications_LeaveRequests",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_AuthorId",
                table: "AuditLog",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Entity",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_OccurredAt",
                table: "AuditLog",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceMovements_AuthorId",
                table: "BalanceMovements",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceMovements_Balance",
                table: "BalanceMovements",
                columns: new[] { "BalanceId", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_BalanceMovements_Request_Kind",
                table: "BalanceMovements",
                columns: new[] { "LeaveRequestId", "Kind" },
                unique: true,
                filter: "LeaveRequestId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Balances_LeaveTypeId",
                table: "Balances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Balances_User_LeaveType",
                table: "Balances",
                columns: new[] { "UserId", "LeaveTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestHistory_AuthorId",
                table: "LeaveRequestHistory",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestHistory_Request",
                table: "LeaveRequestHistory",
                columns: new[] { "LeaveRequestId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Approved_Dates",
                table: "LeaveRequests",
                columns: new[] { "StartDate", "EndDate" },
                filter: "Status = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CancelledById",
                table: "LeaveRequests",
                column: "CancelledById");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_DecidedById",
                table: "LeaveRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_OverlapAcknowledgedById",
                table: "LeaveRequests",
                column: "OverlapAcknowledgedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Pending_Submitted",
                table: "LeaveRequests",
                column: "SubmittedAtUtc",
                filter: "Status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Requester_Status",
                table: "LeaveRequests",
                columns: new[] { "RequesterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Due",
                table: "Notifications",
                column: "NextAttemptAtUtc",
                filter: "Status = 'Queued'");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LeaveRequestId",
                table: "Notifications",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_Created",
                table: "Notifications",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_Role",
                table: "UserRoles",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BalanceMovements");

            migrationBuilder.DropTable(
                name: "LeaveRequestHistory");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Balances");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LeaveTypes");
        }
    }
}
