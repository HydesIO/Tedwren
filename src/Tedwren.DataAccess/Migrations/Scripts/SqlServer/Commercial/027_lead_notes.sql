-- Lead activity log (Web Plan §7): append-only notes per lead, including automatic status-change entries —
-- SQL Server, commercial database. Idempotent.

IF OBJECT_ID(N'dbo.LeadNotes', N'U') IS NULL
CREATE TABLE dbo.LeadNotes
(
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeadNotes PRIMARY KEY,
    LeadId       UNIQUEIDENTIFIER NOT NULL,
    Author       NVARCHAR(160)    NULL,
    Body         NVARCHAR(MAX)    NOT NULL,
    StatusChange NVARCHAR(64)     NULL,
    CreatedUtc   DATETIMEOFFSET   NOT NULL
);

-- Fetch a lead's log newest-first.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LeadNotes_Lead')
CREATE INDEX IX_LeadNotes_Lead ON dbo.LeadNotes (LeadId, CreatedUtc DESC);
