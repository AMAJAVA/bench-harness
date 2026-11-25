IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [LeaveTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(10) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [IsTracked] bit NOT NULL,
        [AllowsRetroactive] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Version] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [PublicHolidays] (
        [Date] date NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PublicHolidays] PRIMARY KEY ([Date])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [NormalizedEmail] nvarchar(254) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [ManagerId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [SecurityStamp] uniqueidentifier NOT NULL,
        [MustChangePassword] bit NOT NULL,
        [FailedLoginCount] int NOT NULL DEFAULT 0,
        [FirstFailedLoginAtUtc] datetime2(3) NULL,
        [LockoutEndUtc] datetime2(3) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [DeactivatedAtUtc] datetime2(3) NULL,
        [Version] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Users_Deactivation] CHECK ((IsActive = 1 AND DeactivatedAtUtc IS NULL) OR (IsActive = 0 AND DeactivatedAtUtc IS NOT NULL)),
        CONSTRAINT [CK_Users_FailedLoginCount] CHECK (FailedLoginCount >= 0),
        CONSTRAINT [CK_Users_NotOwnManager] CHECK (ManagerId IS NULL OR ManagerId <> Id),
        CONSTRAINT [FK_Users_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLog] (
        [Id] bigint NOT NULL IDENTITY,
        [AuthorId] uniqueidentifier NOT NULL,
        [Action] varchar(40) NOT NULL,
        [EntityType] varchar(40) NOT NULL,
        [EntityId] nvarchar(64) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [OccurredAtUtc] datetime2(3) NOT NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLog_Author] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [Balances] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [LeaveTypeId] uniqueidentifier NOT NULL,
        [AmountHundredths] int NOT NULL,
        [Version] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Balances] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Balances_Amount_NonNegative] CHECK (AmountHundredths >= 0),
        CONSTRAINT [FK_Balances_LeaveTypes] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]),
        CONSTRAINT [FK_Balances_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [LeaveRequests] (
        [Id] uniqueidentifier NOT NULL,
        [RequesterId] uniqueidentifier NOT NULL,
        [LeaveTypeId] uniqueidentifier NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [DaysCount] int NOT NULL,
        [RequesterComment] nvarchar(500) NULL,
        [Status] varchar(20) NOT NULL,
        [SubmittedAtUtc] datetime2(3) NOT NULL,
        [SubmissionOverlapAlert] bit NOT NULL,
        [SubmissionOverlapMaxAbsent] int NOT NULL,
        [SubmissionOverlapTeamSize] int NOT NULL,
        [DecidedById] uniqueidentifier NULL,
        [DecidedAtUtc] datetime2(3) NULL,
        [DecisionComment] nvarchar(1000) NULL,
        [OverlapAcknowledgedById] uniqueidentifier NULL,
        [OverlapAcknowledgedAtUtc] datetime2(3) NULL,
        [OverlapAcknowledgedAbsent] int NULL,
        [OverlapAcknowledgedTeamSize] int NULL,
        [CancelledById] uniqueidentifier NULL,
        [CancelledAtUtc] datetime2(3) NULL,
        [CancellationReason] nvarchar(200) NULL,
        [Version] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveRequests_Acknowledgement] CHECK ((OverlapAcknowledgedById IS NULL AND OverlapAcknowledgedAtUtc IS NULL AND OverlapAcknowledgedAbsent IS NULL AND OverlapAcknowledgedTeamSize IS NULL) OR (OverlapAcknowledgedById IS NOT NULL AND OverlapAcknowledgedAtUtc IS NOT NULL AND OverlapAcknowledgedTeamSize > 0 AND OverlapAcknowledgedAbsent >= 0 AND OverlapAcknowledgedAbsent <= OverlapAcknowledgedTeamSize)),
        CONSTRAINT [CK_LeaveRequests_Cancellation] CHECK ((Status = 'Cancelled' AND CancelledAtUtc IS NOT NULL) OR (Status <> 'Cancelled' AND CancelledAtUtc IS NULL AND CancelledById IS NULL)),
        CONSTRAINT [CK_LeaveRequests_Dates] CHECK (EndDate >= StartDate),
        CONSTRAINT [CK_LeaveRequests_DaysCount] CHECK (DaysCount >= 1),
        CONSTRAINT [CK_LeaveRequests_Decision] CHECK ((Status = 'Pending' AND DecidedById IS NULL AND DecidedAtUtc IS NULL) OR (Status IN ('Approved', 'Rejected') AND DecidedById IS NOT NULL AND DecidedAtUtc IS NOT NULL) OR (Status = 'Cancelled')),
        CONSTRAINT [CK_LeaveRequests_NoSelfDecision] CHECK (DecidedById IS NULL OR DecidedById <> RequesterId),
        CONSTRAINT [CK_LeaveRequests_RejectionComment] CHECK (Status <> 'Rejected' OR DecisionComment IS NOT NULL),
        CONSTRAINT [CK_LeaveRequests_Status] CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
        CONSTRAINT [CK_LeaveRequests_SubmissionOverlap] CHECK (SubmissionOverlapTeamSize >= 0 AND SubmissionOverlapMaxAbsent >= 0 AND SubmissionOverlapMaxAbsent <= SubmissionOverlapTeamSize),
        CONSTRAINT [FK_LeaveRequests_AcknowledgedBy] FOREIGN KEY ([OverlapAcknowledgedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_LeaveRequests_CancelledBy] FOREIGN KEY ([CancelledById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_LeaveRequests_DecidedBy] FOREIGN KEY ([DecidedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_LeaveRequests_LeaveTypes] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]),
        CONSTRAINT [FK_LeaveRequests_Requester] FOREIGN KEY ([RequesterId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [Role] varchar(20) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [Role]),
        CONSTRAINT [CK_UserRoles_Role] CHECK (Role IN ('Employee', 'Manager', 'HrAdmin')),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [BalanceMovements] (
        [Id] bigint NOT NULL IDENTITY,
        [BalanceId] uniqueidentifier NOT NULL,
        [Kind] varchar(20) NOT NULL,
        [QuantityHundredths] int NOT NULL,
        [BalanceAfterHundredths] int NOT NULL,
        [Reason] nvarchar(500) NULL,
        [LeaveRequestId] uniqueidentifier NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetime2(3) NOT NULL,
        CONSTRAINT [PK_BalanceMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BalanceMovements_BalanceAfter] CHECK (BalanceAfterHundredths >= 0),
        CONSTRAINT [CK_BalanceMovements_Kind] CHECK (Kind IN ('Allocation', 'Adjustment', 'Debit', 'Recredit')),
        CONSTRAINT [CK_BalanceMovements_Links] CHECK ((Kind IN ('Allocation', 'Adjustment') AND Reason IS NOT NULL AND LeaveRequestId IS NULL) OR (Kind IN ('Debit', 'Recredit') AND LeaveRequestId IS NOT NULL)),
        CONSTRAINT [CK_BalanceMovements_Quantity_NonZero] CHECK (QuantityHundredths <> 0),
        CONSTRAINT [CK_BalanceMovements_Sign] CHECK ((Kind = 'Allocation' AND QuantityHundredths > 0) OR (Kind = 'Adjustment') OR (Kind = 'Debit' AND QuantityHundredths < 0) OR (Kind = 'Recredit' AND QuantityHundredths > 0)),
        CONSTRAINT [FK_BalanceMovements_Author] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_BalanceMovements_Balances] FOREIGN KEY ([BalanceId]) REFERENCES [Balances] ([Id]),
        CONSTRAINT [FK_BalanceMovements_LeaveRequests] FOREIGN KEY ([LeaveRequestId]) REFERENCES [LeaveRequests] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [LeaveRequestHistory] (
        [Id] bigint NOT NULL IDENTITY,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [FromStatus] varchar(20) NULL,
        [ToStatus] varchar(20) NOT NULL,
        [AuthorId] uniqueidentifier NULL,
        [OccurredAtUtc] datetime2(3) NOT NULL,
        [Comment] nvarchar(1000) NULL,
        CONSTRAINT [PK_LeaveRequestHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveRequestHistory_FromStatus] CHECK (FromStatus IS NULL OR FromStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
        CONSTRAINT [CK_LeaveRequestHistory_ToStatus] CHECK (ToStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
        CONSTRAINT [FK_LeaveRequestHistory_Author] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_LeaveRequestHistory_LeaveRequests] FOREIGN KEY ([LeaveRequestId]) REFERENCES [LeaveRequests] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [EventType] varchar(30) NOT NULL,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [RecipientEmail] nvarchar(254) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Status] varchar(10) NOT NULL,
        [Attempts] int NOT NULL DEFAULT 0,
        [NextAttemptAtUtc] datetime2(3) NULL,
        [LockedUntilUtc] datetime2(3) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [LastAttemptAtUtc] datetime2(3) NULL,
        [SentAtUtc] datetime2(3) NULL,
        [LastError] nvarchar(500) NULL,
        [Version] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Notifications_Attempts] CHECK (Attempts >= 0 AND Attempts <= 4),
        CONSTRAINT [CK_Notifications_EventType] CHECK (EventType IN ('Submission', 'Approval', 'Rejection', 'ApprovedCancellation')),
        CONSTRAINT [CK_Notifications_Queued] CHECK (Status <> 'Queued' OR NextAttemptAtUtc IS NOT NULL),
        CONSTRAINT [CK_Notifications_Sent] CHECK ((Status = 'Sent' AND SentAtUtc IS NOT NULL) OR (Status <> 'Sent' AND SentAtUtc IS NULL)),
        CONSTRAINT [CK_Notifications_Status] CHECK (Status IN ('Queued', 'Sent', 'Failed')),
        CONSTRAINT [FK_Notifications_LeaveRequests] FOREIGN KEY ([LeaveRequestId]) REFERENCES [LeaveRequests] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLog_AuthorId] ON [AuditLog] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLog_Entity] ON [AuditLog] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLog_OccurredAt] ON [AuditLog] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BalanceMovements_AuthorId] ON [BalanceMovements] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BalanceMovements_Balance] ON [BalanceMovements] ([BalanceId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BalanceMovements_Request_Kind] ON [BalanceMovements] ([LeaveRequestId], [Kind]) WHERE LeaveRequestId IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Balances_LeaveTypeId] ON [Balances] ([LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Balances_User_LeaveType] ON [Balances] ([UserId], [LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestHistory_AuthorId] ON [LeaveRequestHistory] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestHistory_Request] ON [LeaveRequestHistory] ([LeaveRequestId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_LeaveRequests_Approved_Dates] ON [LeaveRequests] ([StartDate], [EndDate]) WHERE Status = ''Approved''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_CancelledById] ON [LeaveRequests] ([CancelledById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_DecidedById] ON [LeaveRequests] ([DecidedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [LeaveRequests] ([LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_OverlapAcknowledgedById] ON [LeaveRequests] ([OverlapAcknowledgedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_LeaveRequests_Pending_Submitted] ON [LeaveRequests] ([SubmittedAtUtc]) WHERE Status = ''Pending''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_Requester_Status] ON [LeaveRequests] ([RequesterId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_LeaveTypes_Code] ON [LeaveTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Notifications_Due] ON [Notifications] ([NextAttemptAtUtc]) WHERE Status = ''Queued''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_LeaveRequestId] ON [Notifications] ([LeaveRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_Status_Created] ON [Notifications] ([Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_Role] ON [UserRoles] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_ManagerId] ON [Users] ([ManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_NormalizedEmail] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261125201834_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261125201834_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

