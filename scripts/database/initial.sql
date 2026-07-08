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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE TABLE [Drivers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [VehicleType] nvarchar(32) NOT NULL,
        [CurrentLat] float NOT NULL,
        [CurrentLong] float NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Drivers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [RestaurantId] uniqueidentifier NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [EstimatedDelivery] datetimeoffset NOT NULL,
        [ActualDelivery] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE TABLE [DriverAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [DriverId] uniqueidentifier NOT NULL,
        [AssignedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_DriverAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DriverAssignments_Drivers_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Drivers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DriverAssignments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [MenuItemId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DriverAssignments_DriverId_CompletedAt] ON [DriverAssignments] ([DriverId], [CompletedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DriverAssignments_OrderId] ON [DriverAssignments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Drivers_Status] ON [Drivers] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId_CreatedAt] ON [Orders] ([CustomerId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_RestaurantId_Status] ON [Orders] ([RestaurantId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status_CreatedAt] ON [Orders] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708181710_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260708181710_InitialCreate', N'8.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708183349_AddDriverGeography'
)
BEGIN
    ALTER TABLE [Drivers] ADD [Location] geography NOT NULL DEFAULT (geography::Point(0, 0, 4326));
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708183349_AddDriverGeography'
)
BEGIN
    CREATE SPATIAL INDEX [SIX_Drivers_Location] ON [Drivers]([Location]) USING GEOGRAPHY_AUTO_GRID WITH (CELLS_PER_OBJECT = 16)
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708183349_AddDriverGeography'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260708183349_AddDriverGeography', N'8.0.4');
END;
GO

COMMIT;
GO

