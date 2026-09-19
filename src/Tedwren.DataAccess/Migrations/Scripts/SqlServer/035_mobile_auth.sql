-- Operative mobile auth (M2): device binding + one-time-code challenges — SQL Server. Idempotent.
-- Backs the Dapper OperativeDeviceRepository / OtpChallengeRepository and keeps raw-script ↔ EF parity.

IF OBJECT_ID(N'dbo.OperativeDevices', N'U') IS NULL
CREATE TABLE dbo.OperativeDevices
(
    Id                     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperativeDevices PRIMARY KEY,
    PersonId               UNIQUEIDENTIFIER NOT NULL,
    CompanyId              UNIQUEIDENTIFIER NOT NULL,
    DeviceId               NVARCHAR(128)    NOT NULL,
    DeviceName             NVARCHAR(256)    NULL,
    Status                 INT              NOT NULL,   -- 0 Active, 1 Revoked
    RefreshTokenHash       NVARCHAR(512)    NULL,
    RefreshTokenExpiresUtc DATETIMEOFFSET   NULL,
    EnrolledUtc            DATETIMEOFFSET   NOT NULL,
    LastSeenUtc            DATETIMEOFFSET   NOT NULL
);

-- One binding per device install; unique.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperativeDevices_DeviceId')
CREATE UNIQUE INDEX IX_OperativeDevices_DeviceId ON dbo.OperativeDevices (DeviceId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperativeDevices_PersonId')
CREATE INDEX IX_OperativeDevices_PersonId ON dbo.OperativeDevices (PersonId);

IF OBJECT_ID(N'dbo.OtpChallenges', N'U') IS NULL
CREATE TABLE dbo.OtpChallenges
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OtpChallenges PRIMARY KEY,
    PhoneNumber NVARCHAR(32)     NOT NULL,
    CodeHash    NVARCHAR(512)    NOT NULL,
    ExpiresUtc  DATETIMEOFFSET   NOT NULL,
    Attempts    INT              NOT NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OtpChallenges_PhoneNumber')
CREATE INDEX IX_OtpChallenges_PhoneNumber ON dbo.OtpChallenges (PhoneNumber);
