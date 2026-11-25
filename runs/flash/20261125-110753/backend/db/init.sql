CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE "Teams" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "ManagerId" UUID NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(256) NOT NULL UNIQUE,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "TeamId" UUID NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_Users_Teams" FOREIGN KEY ("TeamId") REFERENCES "Teams"("Id") ON DELETE SET NULL
);

ALTER TABLE "Teams" ADD CONSTRAINT "FK_Teams_Users" FOREIGN KEY ("ManagerId") REFERENCES "Users"("Id") ON DELETE RESTRICT;

CREATE TABLE "UserRoles" (
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    PRIMARY KEY ("UserId", "Role"),
    CONSTRAINT "FK_UserRoles_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE TABLE "LeaveTypes" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(20) NOT NULL UNIQUE,
    "Label" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "TrackBalance" BOOLEAN NOT NULL DEFAULT TRUE,
    "DisplayOrder" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE TABLE "LeaveBalances" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "LeaveTypeId" UUID NOT NULL,
    "Year" INT NOT NULL,
    "AcquiredDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "AdjustmentDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "UsedDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "Version" INT NOT NULL DEFAULT 0,
    CONSTRAINT "UQ_LeaveBalances_User_Type_Year" UNIQUE ("UserId", "LeaveTypeId", "Year"),
    CONSTRAINT "FK_LeaveBalances_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveBalances_LeaveTypes" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes"("Id") ON DELETE RESTRICT
);

CREATE TABLE "LeaveRequests" (
    "Id" UUID PRIMARY KEY,
    "EmployeeId" UUID NOT NULL,
    "TeamId" UUID NOT NULL,
    "LeaveTypeId" UUID NOT NULL,
    "StartDate" DATE NOT NULL,
    "EndDate" DATE NOT NULL,
    "WorkingDays" INT NOT NULL,
    "Status" VARCHAR(20) NOT NULL CHECK ("Status" IN ('Pending','Approved','Rejected')),
    "EmployeeComment" TEXT NULL,
    "SubmittedAt" TIMESTAMPTZ NOT NULL,
    "DeciderId" UUID NULL,
    "DecidedAt" TIMESTAMPTZ NULL,
    "ManagerComment" TEXT NULL,
    "HasOverlapWarning" BOOLEAN NOT NULL DEFAULT FALSE,
    "OverlapRatio" NUMERIC(5,4) NULL,
    "AcknowledgedOverlapBy" UUID NULL,
    "AcknowledgedOverlapAt" TIMESTAMPTZ NULL,
    "Version" INT NOT NULL DEFAULT 0,
    CONSTRAINT "FK_LeaveRequests_Users_Employee" FOREIGN KEY ("EmployeeId") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Teams" FOREIGN KEY ("TeamId") REFERENCES "Teams"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_LeaveTypes" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Users_Decider" FOREIGN KEY ("DeciderId") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Users_Ack" FOREIGN KEY ("AcknowledgedOverlapBy") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_LeaveRequests_Dates" CHECK ("StartDate" <= "EndDate")
);

CREATE INDEX "IX_LeaveRequests_Team_Status" ON "LeaveRequests" ("TeamId", "Status");
CREATE INDEX "IX_LeaveRequests_Employee_Status" ON "LeaveRequests" ("EmployeeId", "Status");
CREATE INDEX "IX_LeaveRequests_Dates" ON "LeaveRequests" ("StartDate", "EndDate");

CREATE TABLE "Holidays" (
    "Id" UUID PRIMARY KEY,
    "Date" DATE NOT NULL UNIQUE,
    "Label" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE TABLE "AuditLogs" (
    "Id" BIGSERIAL PRIMARY KEY,
    "ActorId" UUID NULL,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100) NOT NULL,
    "EntityId" UUID NULL,
    "OccurredAt" TIMESTAMPTZ NOT NULL,
    "Details" TEXT NULL,
    "Reason" TEXT NULL,
    CONSTRAINT "FK_AuditLogs_Users" FOREIGN KEY ("ActorId") REFERENCES "Users"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");
CREATE INDEX "IX_AuditLogs_OccurredAt" ON "AuditLogs" ("OccurredAt");

CREATE TABLE "EmailOutbox" (
    "Id" UUID PRIMARY KEY,
    "Recipient" VARCHAR(256) NOT NULL,
    "Template" VARCHAR(100) NOT NULL,
    "Payload" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL CHECK ("Status" IN ('Pending','Sent','Failed')),
    "Attempts" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "LastAttemptAt" TIMESTAMPTZ NULL,
    "ErrorMessage" TEXT NULL
);

CREATE INDEX "IX_EmailOutbox_Status_CreatedAt" ON "EmailOutbox" ("Status", "CreatedAt");
