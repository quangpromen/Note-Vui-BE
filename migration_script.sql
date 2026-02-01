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
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_providers] (
        [provider_id] int NOT NULL IDENTITY,
        [provider_code] varchar(50) NOT NULL,
        [provider_name] nvarchar(100) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_ai_providers] PRIMARY KEY ([provider_id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [plans] (
        [plan_id] int NOT NULL IDENTITY,
        [plan_code] varchar(50) NOT NULL,
        [plan_name] nvarchar(100) NOT NULL,
        [max_notes] int NOT NULL DEFAULT 50,
        [daily_ai_limit] int NOT NULL DEFAULT 5,
        [price] decimal(18,2) NOT NULL DEFAULT 0.0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_plans] PRIMARY KEY ([plan_id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [AvatarUrl] nvarchar(max) NULL,
        [RefreshToken] nvarchar(max) NULL,
        [RefreshTokenExpiryTime] datetime2 NULL,
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
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_models] (
        [model_id] int NOT NULL IDENTITY,
        [provider_id] int NOT NULL,
        [model_code] varchar(100) NOT NULL,
        [cost_input] decimal(18,10) NOT NULL,
        [cost_output] decimal(18,10) NOT NULL,
        CONSTRAINT [PK_ai_models] PRIMARY KEY ([model_id]),
        CONSTRAINT [FK_ai_models_ai_providers_provider_id] FOREIGN KEY ([provider_id]) REFERENCES [ai_providers] ([provider_id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [notes] (
        [note_id] int NOT NULL IDENTITY,
        [user_id] nvarchar(450) NOT NULL,
        [title] nvarchar(255) NOT NULL,
        [short_preview] nvarchar(500) NULL,
        [is_pinned] bit NOT NULL DEFAULT CAST(0 AS bit),
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_notes] PRIMARY KEY ([note_id]),
        CONSTRAINT [FK_notes_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [subscriptions] (
        [sub_id] int NOT NULL IDENTITY,
        [user_id] nvarchar(450) NOT NULL,
        [plan_id] int NOT NULL,
        [start_date] datetime2 NOT NULL DEFAULT (GETDATE()),
        [end_date] datetime2 NULL,
        [status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
        CONSTRAINT [PK_subscriptions] PRIMARY KEY ([sub_id]),
        CONSTRAINT [FK_subscriptions_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_subscriptions_plans_plan_id] FOREIGN KEY ([plan_id]) REFERENCES [plans] ([plan_id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE TABLE [note_contents] (
        [NoteId] int NOT NULL,
        [full_content] nvarchar(max) NULL,
        CONSTRAINT [PK_note_contents] PRIMARY KEY ([NoteId]),
        CONSTRAINT [FK_note_contents_notes_NoteId] FOREIGN KEY ([NoteId]) REFERENCES [notes] ([note_id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'AvatarUrl', N'ConcurrencyStamp', N'Email', N'EmailConfirmed', N'FullName', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'RefreshToken', N'RefreshTokenExpiryTime', N'SecurityStamp', N'TwoFactorEnabled', N'UserName') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] ON;
    EXEC(N'INSERT INTO [Users] ([Id], [AccessFailedCount], [AvatarUrl], [ConcurrencyStamp], [Email], [EmailConfirmed], [FullName], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [RefreshToken], [RefreshTokenExpiryTime], [SecurityStamp], [TwoFactorEnabled], [UserName])
    VALUES (N''11111111-1111-1111-1111-111111111111'', 0, NULL, N''4dba5f71-3c32-4d29-880b-7877e570d2c0'', N''test@notevui.com'', CAST(1 AS bit), N''Test User'', CAST(0 AS bit), NULL, N''TEST@NOTEVUI.COM'', N''TEST@NOTEVUI.COM'', N''AQAAAAIAAYagAAAAEK9l3c5s/Ll1Y0DwrBU7Rah+N2WbN9v+5RYaQXJtK3qIr5tqsymfHMoT5wNxPvQCgQ=='', NULL, CAST(0 AS bit), NULL, NULL, N''2E2B8BB1-8BE4-4E40-8C8E-8E8E8E8E8E8E'', CAST(0 AS bit), N''test@notevui.com'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'AvatarUrl', N'ConcurrencyStamp', N'Email', N'EmailConfirmed', N'FullName', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'RefreshToken', N'RefreshTokenExpiryTime', N'SecurityStamp', N'TwoFactorEnabled', N'UserName') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'provider_id', N'is_active', N'provider_code', N'provider_name') AND [object_id] = OBJECT_ID(N'[ai_providers]'))
        SET IDENTITY_INSERT [ai_providers] ON;
    EXEC(N'INSERT INTO [ai_providers] ([provider_id], [is_active], [provider_code], [provider_name])
    VALUES (1, CAST(1 AS bit), ''openai'', N''OpenAI''),
    (2, CAST(1 AS bit), ''google'', N''Google AI''),
    (3, CAST(1 AS bit), ''anthropic'', N''Anthropic'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'provider_id', N'is_active', N'provider_code', N'provider_name') AND [object_id] = OBJECT_ID(N'[ai_providers]'))
        SET IDENTITY_INSERT [ai_providers] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'plan_id', N'daily_ai_limit', N'is_active', N'max_notes', N'plan_code', N'plan_name') AND [object_id] = OBJECT_ID(N'[plans]'))
        SET IDENTITY_INSERT [plans] ON;
    EXEC(N'INSERT INTO [plans] ([plan_id], [daily_ai_limit], [is_active], [max_notes], [plan_code], [plan_name])
    VALUES (1, 5, CAST(1 AS bit), 50, ''free'', N''Free Plan'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'plan_id', N'daily_ai_limit', N'is_active', N'max_notes', N'plan_code', N'plan_name') AND [object_id] = OBJECT_ID(N'[plans]'))
        SET IDENTITY_INSERT [plans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'plan_id', N'daily_ai_limit', N'is_active', N'max_notes', N'plan_code', N'plan_name', N'price') AND [object_id] = OBJECT_ID(N'[plans]'))
        SET IDENTITY_INSERT [plans] ON;
    EXEC(N'INSERT INTO [plans] ([plan_id], [daily_ai_limit], [is_active], [max_notes], [plan_code], [plan_name], [price])
    VALUES (2, 50, CAST(1 AS bit), 500, ''pro'', N''Pro Plan'', 9.99),
    (3, -1, CAST(1 AS bit), -1, ''premium'', N''Premium Plan'', 19.99)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'plan_id', N'daily_ai_limit', N'is_active', N'max_notes', N'plan_code', N'plan_name', N'price') AND [object_id] = OBJECT_ID(N'[plans]'))
        SET IDENTITY_INSERT [plans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'model_id', N'cost_input', N'cost_output', N'model_code', N'provider_id') AND [object_id] = OBJECT_ID(N'[ai_models]'))
        SET IDENTITY_INSERT [ai_models] ON;
    EXEC(N'INSERT INTO [ai_models] ([model_id], [cost_input], [cost_output], [model_code], [provider_id])
    VALUES (1, 0.0000025, 0.00001, ''gpt-4o'', 1),
    (2, 0.00000015, 0.0000006, ''gpt-4o-mini'', 1),
    (3, 0.0000005, 0.0000015, ''gpt-3.5-turbo'', 1),
    (4, 0.00000125, 0.000005, ''gemini-1.5-pro'', 2),
    (5, 0.000000075, 0.0000003, ''gemini-1.5-flash'', 2),
    (6, 0.000003, 0.000015, ''claude-3-5-sonnet'', 3),
    (7, 0.00000025, 0.00000125, ''claude-3-haiku'', 3)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'model_id', N'cost_input', N'cost_output', N'model_code', N'provider_id') AND [object_id] = OBJECT_ID(N'[ai_models]'))
        SET IDENTITY_INSERT [ai_models] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiModels_Provider] ON [ai_models] ([provider_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiModels_Provider_ModelCode] ON [ai_models] ([provider_id], [model_code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiProviders_ProviderCode] ON [ai_providers] ([provider_code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notes_User_Active] ON [notes] ([user_id], [is_deleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notes_User_Pinned] ON [notes] ([user_id], [is_pinned]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_PlanCode] ON [plans] ([plan_code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_subscriptions_plan_id] ON [subscriptions] ([plan_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_User] ON [subscriptions] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121192840_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121192840_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128161851_AddClientIdToNotes'
)
BEGIN
    ALTER TABLE [notes] ADD [client_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128161851_AddClientIdToNotes'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [ConcurrencyStamp] = N''90bcf484-899a-4238-9b07-a848d55d51fc'', [PasswordHash] = N''AQAAAAIAAYagAAAAEAhaBD87ykivzVlg707EYeGrzGyHhF5wkeFz3/CdcyQ3Eb1KF9O1ETRXN6BICEj6CA==''
    WHERE [Id] = N''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128161851_AddClientIdToNotes'
)
BEGIN
    CREATE INDEX [IX_Notes_User_ClientId] ON [notes] ([user_id], [client_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128161851_AddClientIdToNotes'
)
BEGIN
    CREATE INDEX [IX_Notes_User_UpdatedAt] ON [notes] ([user_id], [updated_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128161851_AddClientIdToNotes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128161851_AddClientIdToNotes', N'8.0.0');
END;
GO

COMMIT;
GO

