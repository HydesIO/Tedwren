-- Compliance master lists for subcontractor onboarding — SSIP schemes, configurable document headings and
-- per-operative "other requirements" (Subcontractor Onboarding spec §5–§8) — SQL Server. Idempotent.
-- Global rows (CompanyId NULL) are platform-owned and inherited by every tenant; org rows are custom entries
-- scoped to a main contractor (R15). Seeds the shared national lists (mirrors Application MasterListSeed).

IF OBJECT_ID(N'dbo.MasterListItems', N'U') IS NULL
CREATE TABLE dbo.MasterListItems
(
    Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MasterListItems PRIMARY KEY,
    ListKey    NVARCHAR(64)     NOT NULL,
    CompanyId  UNIQUEIDENTIFIER NULL,
    Value      NVARCHAR(256)    NOT NULL,
    SortOrder  INT              NOT NULL,
    IsActive   BIT              NOT NULL,
    CreatedUtc DATETIMEOFFSET   NOT NULL
);

-- Lookups fetch a list scoped to the global rows plus one tenant's own (R15).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MasterListItems_ListKey_CompanyId')
CREATE INDEX IX_MasterListItems_ListKey_CompanyId ON dbo.MasterListItems (ListKey, CompanyId);

-- Idempotent seed of the shared global lists (CompanyId NULL): insert only values not already present.
MERGE dbo.MasterListItems AS target
USING (VALUES
    ('document-headings', N'Employer''s Liability Insurance', 0),
    ('document-headings', N'Public Liability Insurance', 1),
    ('document-headings', N'Professional Indemnity Insurance', 2),
    ('document-headings', N'SSIP Membership', 3),
    ('document-headings', N'Risk Assessments & Method Statements (RAMS)', 4),
    ('document-headings', N'COSHH Assessment', 5),
    ('document-headings', N'Company Health & Safety Policy', 6),
    ('document-headings', N'Construction Phase Plan', 7),
    ('document-headings', N'Lifting Plan (LOLER)', 8),
    ('document-headings', N'Temporary Works Design / Register', 9),
    ('document-headings', N'Permit to Work / Safe System of Work', 10),
    ('document-headings', N'Plant & Equipment Inspection Records', 11),
    ('document-headings', N'Manual Handling Assessment', 12),
    ('document-headings', N'Noise / HAVS Assessment', 13),
    ('document-headings', N'Environmental / Waste Management Plan', 14),
    ('document-headings', N'Toolbox Talk Records', 15),
    ('document-headings', N'Trade Accreditations', 16),
    ('ssip-schemes', N'CHAS', 0),
    ('ssip-schemes', N'SMAS Worksafe', 1),
    ('ssip-schemes', N'Alcumus SafeContractor', 2),
    ('ssip-schemes', N'Constructionline', 3),
    ('ssip-schemes', N'Acclaim Accreditation', 4),
    ('ssip-schemes', N'Avetta', 5),
    ('ssip-schemes', N'Eurosafe CDM Competent', 6),
    ('ssip-schemes', N'Fortius CDM Comply', 7),
    ('ssip-schemes', N'Greenlight Safety Assessment Scheme', 8),
    ('ssip-schemes', N'CQMS Safety-Scheme', 9),
    ('ssip-schemes', N'Safe-T-Cert', 10),
    ('ssip-schemes', N'NHBC Safemark', 11),
    ('ssip-schemes', N'Exor', 12),
    ('ssip-schemes', N'Vantify Supply Chain', 13),
    ('ssip-schemes', N'Achilles Building Confidence', 14),
    ('ssip-schemes', N'HAE SafeHire Certification', 15),
    ('ssip-schemes', N'Other (specify)', 16),
    ('other-requirements', N'Asbestos Awareness (UKATA / IATP)', 0),
    ('other-requirements', N'Face Fit Testing (RPE)', 1),
    ('other-requirements', N'First Aid at Work', 2),
    ('other-requirements', N'Manual Handling', 3),
    ('other-requirements', N'Working at Height', 4),
    ('other-requirements', N'Fire Marshal / Warden', 5),
    ('other-requirements', N'Abrasive Wheels', 6),
    ('other-requirements', N'Drug & Alcohol Policy / Test', 7),
    ('other-requirements', N'COSHH Awareness', 8),
    ('other-requirements', N'Safety-critical Medical', 9)
) AS source (ListKey, Value, SortOrder)
ON target.ListKey = source.ListKey AND target.CompanyId IS NULL AND target.Value = source.Value
WHEN NOT MATCHED THEN
    INSERT (Id, ListKey, CompanyId, Value, SortOrder, IsActive, CreatedUtc)
    VALUES (NEWID(), source.ListKey, NULL, source.Value, source.SortOrder, 1, SYSDATETIMEOFFSET());
