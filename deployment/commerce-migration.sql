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
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [Genre] (
        [Id] int NOT NULL IDENTITY,
        [GenreName] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_Genre] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderStatus] (
        [Id] int NOT NULL IDENTITY,
        [StatusId] int NOT NULL,
        [StatusName] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_OrderStatus] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [ShoppingCart] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ShoppingCart] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [Book] (
        [Id] int NOT NULL IDENTITY,
        [BookName] nvarchar(40) NOT NULL,
        [AuthorName] nvarchar(40) NOT NULL,
        [Price] float NOT NULL,
        [Image] nvarchar(max) NULL,
        [GenreId] int NOT NULL,
        CONSTRAINT [PK_Book] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Book_Genre_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genre] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [Order] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [CreateDate] datetime2 NOT NULL,
        [OrderStatusId] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [Name] nvarchar(30) NOT NULL,
        [Email] nvarchar(30) NOT NULL,
        [MobileNumber] nvarchar(max) NOT NULL,
        [Address] nvarchar(200) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [IsPaid] bit NOT NULL,
        CONSTRAINT [PK_Order] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Order_OrderStatus_OrderStatusId] FOREIGN KEY ([OrderStatusId]) REFERENCES [OrderStatus] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [CartDetail] (
        [Id] int NOT NULL IDENTITY,
        [ShoppingCartId] int NOT NULL,
        [BookId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] float NOT NULL,
        CONSTRAINT [PK_CartDetail] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartDetail_Book_BookId] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartDetail_ShoppingCart_ShoppingCartId] FOREIGN KEY ([ShoppingCartId]) REFERENCES [ShoppingCart] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [Stock] (
        [Id] int NOT NULL IDENTITY,
        [BookId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_Stock] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stock_Book_BookId] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderDetail] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [BookId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] float NOT NULL,
        CONSTRAINT [PK_OrderDetail] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderDetail_Book_BookId] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderDetail_Order_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Book_GenreId] ON [Book] ([GenreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartDetail_BookId] ON [CartDetail] ([BookId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartDetail_ShoppingCartId] ON [CartDetail] ([ShoppingCartId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Order_OrderStatusId] ON [Order] ([OrderStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderDetail_BookId] ON [OrderDetail] ([BookId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderDetail_OrderId] ON [OrderDetail] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stock_BookId] ON [Stock] ([BookId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250331111632_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250331111632_InitialCreate', N'9.0.1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [ShoppingCart] WHERE [UserId] IS NULL OR LEN([UserId]) = 0 OR LEN([UserId]) > 450)
        THROW 51001, 'Resolve missing or oversized cart owners before migration.', 1;
    IF EXISTS (SELECT [UserId] FROM [ShoppingCart] GROUP BY [UserId] HAVING COUNT(*) > 1)
        THROW 51002, 'Resolve duplicate carts before migration.', 1;
    IF EXISTS (SELECT [ShoppingCartId], [BookId] FROM [CartDetail] GROUP BY [ShoppingCartId], [BookId] HAVING COUNT(*) > 1)
        THROW 51003, 'Resolve duplicate cart lines before migration.', 1;
    IF EXISTS (SELECT [NormalizedEmail] FROM [AspNetUsers] WHERE [NormalizedEmail] IS NOT NULL GROUP BY [NormalizedEmail] HAVING COUNT(*) > 1)
        THROW 51004, 'Resolve duplicate normalized account emails before migration.', 1;
    IF EXISTS (SELECT UPPER(LTRIM(RTRIM([GenreName]))) COLLATE SQL_Latin1_General_CP1_CI_AS FROM [Genre]
        GROUP BY UPPER(LTRIM(RTRIM([GenreName]))) COLLATE SQL_Latin1_General_CP1_CI_AS HAVING COUNT(*) > 1)
        THROW 51005, 'Resolve duplicate genre names before migration.', 1;
    IF EXISTS (SELECT 1 FROM [Stock] WHERE [Quantity] < 0)
     OR EXISTS (SELECT 1 FROM [CartDetail] WHERE [Quantity] NOT BETWEEN 1 AND 1000)
     OR EXISTS (SELECT 1 FROM [OrderDetail] WHERE [Quantity] NOT BETWEEN 1 AND 1000)
        THROW 51006, 'Resolve invalid stock/cart/order quantities before migration.', 1;
    IF EXISTS (SELECT 1 FROM [Book] WHERE TRY_CONVERT(decimal(18,2),[Price]) IS NULL OR TRY_CONVERT(decimal(18,2),[Price]) <= 0)
     OR EXISTS (SELECT 1 FROM [Order] WHERE LEN([UserId]) > 450)
        THROW 51007, 'Resolve invalid currency values or oversized order owners before migration.', 1;
    IF EXISTS (SELECT [StatusName] FROM [OrderStatus] GROUP BY [StatusName] HAVING COUNT(*) > 1)
        THROW 51008, 'Resolve duplicate order status names before migration.', 1;
    UPDATE [Genre] SET [GenreName] = LTRIM(RTRIM([GenreName]));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Book] DROP CONSTRAINT [FK_Book_Genre_GenreId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [OrderDetail] DROP CONSTRAINT [FK_OrderDetail_Book_BookId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DROP INDEX [IX_CartDetail_ShoppingCartId] ON [CartDetail];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DROP INDEX [EmailIndex] ON [AspNetUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ShoppingCart]') AND [c].[name] = N'UserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ShoppingCart] DROP CONSTRAINT [' + @var + '];');
    EXEC(N'UPDATE [ShoppingCart] SET [UserId] = N'''' WHERE [UserId] IS NULL');
    ALTER TABLE [ShoppingCart] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
    ALTER TABLE [ShoppingCart] ADD DEFAULT N'' FOR [UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderDetail]') AND [c].[name] = N'UnitPrice');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [OrderDetail] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [OrderDetail] ALTER COLUMN [UnitPrice] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [OrderDetail] ADD [AuthorName] nvarchar(40) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [OrderDetail] ADD [BookName] nvarchar(40) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [OrderDetail] ADD [GenreName] nvarchar(40) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Order]') AND [c].[name] = N'UserId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Order] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Order] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [CheckoutKey] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [CouponCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [Discount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [Shipping] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Order] ADD [Total] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Genre]') AND [c].[name] = N'GenreName');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Genre] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Genre] ALTER COLUMN [GenreName] nvarchar(40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CartDetail]') AND [c].[name] = N'UnitPrice');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [CartDetail] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [CartDetail] ALTER COLUMN [UnitPrice] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Book]') AND [c].[name] = N'Price');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Book] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Book] ALTER COLUMN [Price] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Book]') AND [c].[name] = N'Image');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Book] DROP CONSTRAINT [' + @var6 + '];');
    EXEC(N'UPDATE [Book] SET [Image] = N'''' WHERE [Image] IS NULL');
    ALTER TABLE [Book] ALTER COLUMN [Image] nvarchar(max) NOT NULL;
    ALTER TABLE [Book] ADD DEFAULT N'' FOR [Image];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Book] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'ALTER TABLE [Stock] ADD CONSTRAINT [CK_Stock_Quantity] CHECK ([Quantity] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShoppingCart_UserId] ON [ShoppingCart] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'ALTER TABLE [OrderDetail] ADD CONSTRAINT [CK_OrderDetail_Quantity] CHECK ([Quantity] BETWEEN 1 AND 1000)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Order_UserId_CheckoutKey] ON [Order] ([UserId], [CheckoutKey]) WHERE [CheckoutKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    CREATE INDEX [IX_Order_UserId_CreateDate] ON [Order] ([UserId], [CreateDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Genre_GenreName] ON [Genre] ([GenreName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartDetail_ShoppingCartId_BookId] ON [CartDetail] ([ShoppingCartId], [BookId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'ALTER TABLE [CartDetail] ADD CONSTRAINT [CK_CartDetail_Quantity] CHECK ([Quantity] BETWEEN 1 AND 1000)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'ALTER TABLE [Book] ADD CONSTRAINT [CK_Book_Price] CHECK ([Price] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [Book] ADD CONSTRAINT [FK_Book_Genre_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genre] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    ALTER TABLE [OrderDetail] ADD CONSTRAINT [FK_OrderDetail_Book_BookId] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    EXEC(N'UPDATE d SET d.[BookName]=b.[BookName], d.[AuthorName]=b.[AuthorName], d.[GenreName]=g.[GenreName]
    FROM [OrderDetail] d JOIN [Book] b ON d.[BookId]=b.[Id] JOIN [Genre] g ON b.[GenreId]=g.[Id];');
    EXEC(N'UPDATE o SET o.[Subtotal]=COALESCE(t.[Amount],0), o.[Total]=COALESCE(t.[Amount],0)
    FROM [Order] o LEFT JOIN
        (SELECT [OrderId], SUM([UnitPrice]*[Quantity]) AS [Amount] FROM [OrderDetail] GROUP BY [OrderId]) t
        ON o.[Id]=t.[OrderId];');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Pending')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (1,'Pending');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Shipped')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (2,'Shipped');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Delivered')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (3,'Delivered');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Cancelled')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (4,'Cancelled');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Returned')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (5,'Returned');
    IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Refund')
        INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (6,'Refund');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105082638_HardenCommerce'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105082638_HardenCommerce', N'9.0.1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE TABLE [ApiSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [SecurityStampHash] varchar(64) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ApiSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApiSessions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE TABLE [ApiRefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [ConsumedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ApiRefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApiRefreshTokens_ApiSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ApiSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE INDEX [IX_ApiRefreshTokens_SessionId] ON [ApiRefreshTokens] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApiRefreshTokens_TokenHash] ON [ApiRefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE INDEX [IX_ApiSessions_ExpiresAtUtc] ON [ApiSessions] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    CREATE INDEX [IX_ApiSessions_UserId_ExpiresAtUtc] ON [ApiSessions] ([UserId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105102022_AddApiAuthenticationSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105102022_AddApiAuthenticationSessions', N'9.0.1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105121209_ProtectBrowserAuthenticationTokens'
)
BEGIN
    ALTER TABLE [ApiSessions] ADD [ProtectedBrowserTokens] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105121209_ProtectBrowserAuthenticationTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105121209_ProtectBrowserAuthenticationTokens', N'9.0.1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106062559_AddCommerceOperationAudit'
)
BEGIN
    CREATE TABLE [CommerceAuditEvents] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [EntityId] int NOT NULL,
        [ActorId] nvarchar(450) NOT NULL,
        [RequestHash] varchar(64) NOT NULL,
        [Before] nvarchar(64) NOT NULL,
        [After] nvarchar(64) NOT NULL,
        [Reason] nvarchar(300) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CommerceAuditEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106062559_AddCommerceOperationAudit'
)
BEGIN
    CREATE INDEX [IX_CommerceAuditEvents_Kind_EntityId_CreatedAtUtc] ON [CommerceAuditEvents] ([Kind], [EntityId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106062559_AddCommerceOperationAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106062559_AddCommerceOperationAudit', N'9.0.1');
END;

COMMIT;
GO

