-- Console user ↔ site assignments (MC-21, UAT-011): a SiteManager sees only their assigned sites — SQL Server.
-- Idempotent. Backports the table the Dapper SiteAssignmentRepository reads/writes; it was only ever created by
-- the EF migration AddSiteAssignments, so a MigrationRunner-only database lacked it. Restores raw-script ↔ EF parity.

IF OBJECT_ID(N'dbo.SiteAssignments', N'U') IS NULL
CREATE TABLE dbo.SiteAssignments
(
    Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SiteAssignments PRIMARY KEY,
    UserId     UNIQUEIDENTIFIER NOT NULL,
    SiteId     UNIQUEIDENTIFIER NOT NULL,
    CreatedUtc DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SiteAssignments_UserId')
CREATE INDEX IX_SiteAssignments_UserId ON dbo.SiteAssignments (UserId);

-- One assignment row per (user, site).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SiteAssignments_UserId_SiteId')
CREATE UNIQUE INDEX IX_SiteAssignments_UserId_SiteId ON dbo.SiteAssignments (UserId, SiteId);
