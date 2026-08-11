-- Reference option lists for form dropdowns (trades, company types, permit types, regions) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.ReferenceValues', N'U') IS NULL
CREATE TABLE dbo.ReferenceValues
(
    Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReferenceValues PRIMARY KEY,
    ListKey   NVARCHAR(64)     NOT NULL,
    Value     NVARCHAR(256)    NOT NULL,
    SortOrder INT              NOT NULL
);

-- One row per (list, value).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ReferenceValues_List_Value')
CREATE UNIQUE INDEX UX_ReferenceValues_List_Value ON dbo.ReferenceValues (ListKey, Value);

-- Idempotent seed: insert only values not already present.
MERGE dbo.ReferenceValues AS target
USING (VALUES
    ('company-types', 'Main Contractor', 0),
    ('company-types', 'Subcontractor', 1),
    ('company-types', 'Labour Agency', 2),
    ('company-types', 'Consultant', 3),
    ('trades', 'General Build', 0),
    ('trades', 'Groundworks', 1),
    ('trades', 'Mechanical & Electrical', 2),
    ('trades', 'Scaffolding', 3),
    ('trades', 'Fit-Out', 4),
    ('trades', 'Civil Engineering', 5),
    ('trades', 'Roofing', 6),
    ('trades', 'Demolition', 7),
    ('trades', 'Cladding', 8),
    ('permit-types', 'Hot Works', 0),
    ('permit-types', 'Confined Space', 1),
    ('permit-types', 'Working at Height', 2),
    ('permit-types', 'Excavation', 3),
    ('permit-types', 'Electrical Isolation', 4),
    ('permit-types', 'Lifting Operation', 5),
    ('regions', 'London', 0),
    ('regions', 'Manchester', 1),
    ('regions', 'Leeds', 2),
    ('regions', 'Bristol', 3),
    ('regions', 'Birmingham', 4),
    ('regions', 'Glasgow', 5),
    ('regions', 'Cardiff', 6)
) AS source (ListKey, Value, SortOrder)
ON target.ListKey = source.ListKey AND target.Value = source.Value
WHEN NOT MATCHED THEN
    INSERT (Id, ListKey, Value, SortOrder)
    VALUES (NEWID(), source.ListKey, source.Value, source.SortOrder);
