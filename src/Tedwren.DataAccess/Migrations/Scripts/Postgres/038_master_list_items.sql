-- Compliance master lists for subcontractor onboarding — SSIP schemes, configurable document headings and
-- per-operative "other requirements" (Subcontractor Onboarding spec §5–§8) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the repositories' unquoted SQL folds to these names. Global rows (companyid NULL)
-- are platform-owned; org rows are custom entries scoped to a main contractor (R15). Mirrors MasterListSeed.

CREATE TABLE IF NOT EXISTS masterlistitems
(
    id         uuid          NOT NULL PRIMARY KEY,
    listkey    varchar(64)   NOT NULL,
    companyid  uuid          NULL,
    value      varchar(256)  NOT NULL,
    sortorder  integer       NOT NULL,
    isactive   boolean       NOT NULL,
    createdutc timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_masterlistitems_listkey_companyid ON masterlistitems (listkey, companyid);

-- Idempotent seed of the shared global lists (companyid NULL): insert only values not already present.
INSERT INTO masterlistitems (id, listkey, companyid, value, sortorder, isactive, createdutc)
SELECT gen_random_uuid(), v.listkey, NULL, v.value, v.sortorder, true, now()
FROM (VALUES
    ('document-headings', 'Employer''s Liability Insurance', 0),
    ('document-headings', 'Public Liability Insurance', 1),
    ('document-headings', 'Professional Indemnity Insurance', 2),
    ('document-headings', 'SSIP Membership', 3),
    ('document-headings', 'Risk Assessments & Method Statements (RAMS)', 4),
    ('document-headings', 'COSHH Assessment', 5),
    ('document-headings', 'Company Health & Safety Policy', 6),
    ('document-headings', 'Construction Phase Plan', 7),
    ('document-headings', 'Lifting Plan (LOLER)', 8),
    ('document-headings', 'Temporary Works Design / Register', 9),
    ('document-headings', 'Permit to Work / Safe System of Work', 10),
    ('document-headings', 'Plant & Equipment Inspection Records', 11),
    ('document-headings', 'Manual Handling Assessment', 12),
    ('document-headings', 'Noise / HAVS Assessment', 13),
    ('document-headings', 'Environmental / Waste Management Plan', 14),
    ('document-headings', 'Toolbox Talk Records', 15),
    ('document-headings', 'Trade Accreditations', 16),
    ('ssip-schemes', 'CHAS', 0),
    ('ssip-schemes', 'SMAS Worksafe', 1),
    ('ssip-schemes', 'Alcumus SafeContractor', 2),
    ('ssip-schemes', 'Constructionline', 3),
    ('ssip-schemes', 'Acclaim Accreditation', 4),
    ('ssip-schemes', 'Avetta', 5),
    ('ssip-schemes', 'Eurosafe CDM Competent', 6),
    ('ssip-schemes', 'Fortius CDM Comply', 7),
    ('ssip-schemes', 'Greenlight Safety Assessment Scheme', 8),
    ('ssip-schemes', 'CQMS Safety-Scheme', 9),
    ('ssip-schemes', 'Safe-T-Cert', 10),
    ('ssip-schemes', 'NHBC Safemark', 11),
    ('ssip-schemes', 'Exor', 12),
    ('ssip-schemes', 'Vantify Supply Chain', 13),
    ('ssip-schemes', 'Achilles Building Confidence', 14),
    ('ssip-schemes', 'HAE SafeHire Certification', 15),
    ('ssip-schemes', 'Other (specify)', 16),
    ('other-requirements', 'Asbestos Awareness (UKATA / IATP)', 0),
    ('other-requirements', 'Face Fit Testing (RPE)', 1),
    ('other-requirements', 'First Aid at Work', 2),
    ('other-requirements', 'Manual Handling', 3),
    ('other-requirements', 'Working at Height', 4),
    ('other-requirements', 'Fire Marshal / Warden', 5),
    ('other-requirements', 'Abrasive Wheels', 6),
    ('other-requirements', 'Drug & Alcohol Policy / Test', 7),
    ('other-requirements', 'COSHH Awareness', 8),
    ('other-requirements', 'Safety-critical Medical', 9)
) AS v(listkey, value, sortorder)
WHERE NOT EXISTS (
    SELECT 1 FROM masterlistitems m
    WHERE m.listkey = v.listkey AND m.companyid IS NULL AND m.value = v.value
);
