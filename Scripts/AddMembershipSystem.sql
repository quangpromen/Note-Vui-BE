-- Migration: AddMembershipSystem
-- Tạo 2 tables mới cho Membership System

-- 1. Table: payment_transactions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'payment_transactions')
BEGIN
    CREATE TABLE [payment_transactions] (
        [Id] int NOT NULL IDENTITY,
        [user_id] nvarchar(450) NOT NULL,
        [amount] decimal(18,2) NOT NULL,
        [currency] nvarchar(10) NOT NULL DEFAULT N'VND',
        [transaction_code] nvarchar(100) NOT NULL,
        [provider] nvarchar(50) NOT NULL,
        [status] int NOT NULL DEFAULT 0,
        [description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_payment_transactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_payment_transactions_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE UNIQUE INDEX [IX_PaymentTransactions_TransactionCode] ON [payment_transactions] ([transaction_code]);
    CREATE INDEX [IX_PaymentTransactions_UserId] ON [payment_transactions] ([user_id]);
    CREATE INDEX [IX_PaymentTransactions_User_Status] ON [payment_transactions] ([user_id], [status]);
    
    PRINT 'Table payment_transactions created successfully.';
END
ELSE
BEGIN
    PRINT 'Table payment_transactions already exists.';
END
GO

-- 2. Table: user_subscriptions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_subscriptions')
BEGIN
    CREATE TABLE [user_subscriptions] (
        [Id] int NOT NULL IDENTITY,
        [user_id] nvarchar(450) NOT NULL,
        [plan_type] int NOT NULL DEFAULT 0,
        [status] int NOT NULL DEFAULT 0,
        [start_date] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [end_date] datetime2 NOT NULL,
        [is_auto_renew] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_user_subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_subscriptions_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE UNIQUE INDEX [IX_UserSubscriptions_UserId] ON [user_subscriptions] ([user_id]);
    CREATE INDEX [IX_UserSubscriptions_User_Active] ON [user_subscriptions] ([user_id], [status], [end_date]);
    
    PRINT 'Table user_subscriptions created successfully.';
END
ELSE
BEGIN
    PRINT 'Table user_subscriptions already exists.';
END
GO

-- 3. Insert migration history record
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260130150352_AddMembershipSystem')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260130150352_AddMembershipSystem', N'8.0.0');
    PRINT 'Migration history record added.';
END
GO

PRINT 'Membership System migration completed!';
