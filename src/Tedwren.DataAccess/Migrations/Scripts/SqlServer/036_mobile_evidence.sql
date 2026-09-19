-- Operative field evidence captures (M5): offline-captured photo + note + location — SQL Server. Idempotent.
-- Backs the Dapper EvidenceItemRepository and keeps raw-script ↔ EF parity. Append-only (R4); photo via the
-- image store, no permanent public URL (R9).

IF OBJECT_ID(N'dbo.EvidenceItems', N'U') IS NULL
CREATE TABLE dbo.EvidenceItems
(
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EvidenceItems PRIMARY KEY,
    CompanyId      UNIQUEIDENTIFIER NOT NULL,
    PersonId       UNIQUEIDENTIFIER NOT NULL,
    Note           NVARCHAR(2000)   NULL,
    Latitude       FLOAT            NULL,
    Longitude      FLOAT            NULL,
    PhotoReference NVARCHAR(256)    NULL,
    CapturedUtc    DATETIMEOFFSET   NOT NULL,
    CreatedUtc     DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EvidenceItems_CompanyId')
CREATE INDEX IX_EvidenceItems_CompanyId ON dbo.EvidenceItems (CompanyId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EvidenceItems_PersonId')
CREATE INDEX IX_EvidenceItems_PersonId ON dbo.EvidenceItems (PersonId);
