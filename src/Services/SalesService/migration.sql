BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE TABLE [ParkingSlots] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [StationId] uniqueidentifier NOT NULL,
        [SlotType] nvarchar(20) NOT NULL,
        [SlotNumber] nvarchar(10) NOT NULL,
        [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ParkingSlots] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE TABLE [WalletPaymentRequests] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [SaleTransactionId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [DealerUserId] uniqueidentifier NOT NULL,
        [StationId] uniqueidentifier NOT NULL,
        [Amount] DECIMAL(12,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
        [Description] nvarchar(500) NOT NULL,
        [VehicleNumber] nvarchar(15) NULL,
        [FuelTypeName] nvarchar(50) NULL,
        [QuantityLitres] DECIMAL(10,3) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_WalletPaymentRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE TABLE [ParkingBookings] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [ParkingSlotId] uniqueidentifier NOT NULL,
        [StationId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [SlotType] nvarchar(20) NOT NULL,
        [DurationHours] int NOT NULL,
        [Amount] DECIMAL(10,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Initiated',
        [RazorpayOrderId] nvarchar(100) NULL,
        [RazorpayPaymentId] nvarchar(100) NULL,
        [BookedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ParkingBookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ParkingBookings_ParkingSlots_ParkingSlotId] FOREIGN KEY ([ParkingSlotId]) REFERENCES [ParkingSlots] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_ParkingBookings_CustomerId] ON [ParkingBookings] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_ParkingBookings_ParkingSlotId] ON [ParkingBookings] ([ParkingSlotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_ParkingBookings_RazorpayOrderId] ON [ParkingBookings] ([RazorpayOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_ParkingSlots_StationId] ON [ParkingSlots] ([StationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_WalletPaymentRequests_CustomerId] ON [WalletPaymentRequests] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    CREATE INDEX [IX_WalletPaymentRequests_SaleTransactionId] ON [WalletPaymentRequests] ([SaleTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501063426_AddWalletPaymentRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260501063426_AddWalletPaymentRequests', N'9.0.4');
END;

COMMIT;
GO

