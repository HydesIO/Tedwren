-- Console user authentication: password hash + one-time invite token (D1) — SQL Server. Idempotent.

IF COL_LENGTH('dbo.Users', 'PasswordHash') IS NULL
    ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(512) NULL;

IF COL_LENGTH('dbo.Users', 'PasswordSetUtc') IS NULL
    ALTER TABLE dbo.Users ADD PasswordSetUtc DATETIMEOFFSET NULL;

IF COL_LENGTH('dbo.Users', 'InviteToken') IS NULL
    ALTER TABLE dbo.Users ADD InviteToken NVARCHAR(128) NULL;

IF COL_LENGTH('dbo.Users', 'InviteTokenExpiresUtc') IS NULL
    ALTER TABLE dbo.Users ADD InviteTokenExpiresUtc DATETIMEOFFSET NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_InviteToken')
    CREATE INDEX IX_Users_InviteToken ON dbo.Users (InviteToken);
