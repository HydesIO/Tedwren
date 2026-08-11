-- Reference option lists for form dropdowns (trades, company types, permit types, regions) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS ReferenceValues
(
    Id        UUID         NOT NULL PRIMARY KEY,
    ListKey   VARCHAR(64)  NOT NULL,
    Value     VARCHAR(256) NOT NULL,
    SortOrder INT          NOT NULL
);

-- One row per (list, value).
CREATE UNIQUE INDEX IF NOT EXISTS UX_ReferenceValues_List_Value ON ReferenceValues (ListKey, Value);

-- Idempotent seed: insert only values not already present.
INSERT INTO ReferenceValues (Id, ListKey, Value, SortOrder)
SELECT gen_random_uuid(), seed.ListKey, seed.Value, seed.SortOrder
FROM (VALUES
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
) AS seed (ListKey, Value, SortOrder)
ON CONFLICT (ListKey, Value) DO NOTHING;
