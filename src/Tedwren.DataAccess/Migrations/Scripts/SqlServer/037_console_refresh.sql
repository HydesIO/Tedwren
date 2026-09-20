-- Console user refresh tokens (M8): renew an expired console access token without re-login — SQL Server. Idempotent.
-- Backs the Dapper UserRefreshTokenRepository and keeps raw-script ↔ EF parity.

IF OBJECT_ID(N'dbo.UserRefreshTokens', N'U') IS NULL
CREATE TABLE dbo.UserRefreshTokens
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserRefreshTokens PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL,
    CompanyId   UNIQUEIDENTIFIER NOT NULL,
    TokenHash   NVARCHAR(512)    NOT NULL,
    ExpiresUtc  DATETIMEOFFSET   NOT NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL,
    LastUsedUtc DATETIMEOFFSET   NOT NULL,
    RevokedUtc  DATETIMEOFFSET   NULL
);

-- Renew/revoke a user's sessions.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserRefreshTokens_UserId')
CREATE INDEX IX_UserRefreshTokens_UserId ON dbo.UserRefreshTokens (UserId);
