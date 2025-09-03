BEGIN TRANSACTION;
CREATE TABLE [POItemRegistries] (
    [ItemId] int NOT NULL IDENTITY,
    [POId] int NOT NULL,
    [ItemType] nvarchar(450) NOT NULL,
    [BarcodeValue] nvarchar(450) NOT NULL,
    [ItemSequence] int NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [ScannedAt] datetime2 NULL,
    [ScannedBy] nvarchar(max) NULL,
    [ValidatedAt] datetime2 NULL,
    [ValidatedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ItemDescription] nvarchar(max) NULL,
    [ExpectedQuantity] int NULL,
    [Container] nvarchar(max) NULL,
    CONSTRAINT [PK_POItemRegistries] PRIMARY KEY ([ItemId]),
    CONSTRAINT [FK_POItemRegistries_POMasters_POId] FOREIGN KEY ([POId]) REFERENCES [POMasters] ([POId]) ON DELETE CASCADE
);

CREATE TABLE [UserSessionLocks] (
    [LockId] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [SessionId] int NOT NULL,
    [MasterQRCode] nvarchar(max) NOT NULL,
    [LockedAt] datetime2 NOT NULL,
    [UnlockedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserSessionLocks] PRIMARY KEY ([LockId]),
    CONSTRAINT [FK_UserSessionLocks_UploadSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [UploadSessions] ([SessionId]) ON DELETE CASCADE
);

CREATE TABLE [UserPOLocks] (
    [POLockId] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [SessionLockId] int NOT NULL,
    [POId] int NOT NULL,
    [NoPO] nvarchar(max) NOT NULL,
    [LockedAt] datetime2 NOT NULL,
    [UnlockedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserPOLocks] PRIMARY KEY ([POLockId]),
    CONSTRAINT [FK_UserPOLocks_POMasters_POId] FOREIGN KEY ([POId]) REFERENCES [POMasters] ([POId]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPOLocks_UserSessionLocks_SessionLockId] FOREIGN KEY ([SessionLockId]) REFERENCES [UserSessionLocks] ([LockId]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_POItemRegistries_BarcodeValue] ON [POItemRegistries] ([BarcodeValue]);

CREATE INDEX [IX_POItemRegistries_POId_ItemType_Status] ON [POItemRegistries] ([POId], [ItemType], [Status]);

CREATE INDEX [IX_UserPOLocks_POId] ON [UserPOLocks] ([POId]);

CREATE INDEX [IX_UserPOLocks_SessionLockId] ON [UserPOLocks] ([SessionLockId]);

CREATE INDEX [IX_UserPOLocks_UserId_IsActive] ON [UserPOLocks] ([UserId], [IsActive]);

CREATE INDEX [IX_UserSessionLocks_SessionId] ON [UserSessionLocks] ([SessionId]);

CREATE INDEX [IX_UserSessionLocks_UserId_IsActive] ON [UserSessionLocks] ([UserId], [IsActive]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250903063827_AddHierarchicalLockSystem', N'9.0.8');

COMMIT;
GO

