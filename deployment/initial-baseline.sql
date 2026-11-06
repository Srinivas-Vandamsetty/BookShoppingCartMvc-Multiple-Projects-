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
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

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

CREATE TABLE [Genre] (
    [Id] int NOT NULL IDENTITY,
    [GenreName] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_Genre] PRIMARY KEY ([Id])
);

CREATE TABLE [OrderStatus] (
    [Id] int NOT NULL IDENTITY,
    [StatusId] int NOT NULL,
    [StatusName] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_OrderStatus] PRIMARY KEY ([Id])
);

CREATE TABLE [ShoppingCart] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ShoppingCart] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

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

CREATE TABLE [Stock] (
    [Id] int NOT NULL IDENTITY,
    [BookId] int NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_Stock] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stock_Book_BookId] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]) ON DELETE CASCADE
);

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

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_Book_GenreId] ON [Book] ([GenreId]);

CREATE INDEX [IX_CartDetail_BookId] ON [CartDetail] ([BookId]);

CREATE INDEX [IX_CartDetail_ShoppingCartId] ON [CartDetail] ([ShoppingCartId]);

CREATE INDEX [IX_Order_OrderStatusId] ON [Order] ([OrderStatusId]);

CREATE INDEX [IX_OrderDetail_BookId] ON [OrderDetail] ([BookId]);

CREATE INDEX [IX_OrderDetail_OrderId] ON [OrderDetail] ([OrderId]);

CREATE UNIQUE INDEX [IX_Stock_BookId] ON [Stock] ([BookId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250331111632_InitialCreate', N'9.0.1');

COMMIT;
GO

