-- Subcontractor onboarding configuration (Subcontractor Onboarding spec Stage 1 / §4) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the repositories' unquoted SQL folds to these names. One config per trade invite: the
-- required-document headings (Gate 1 set, JSON), access period, SSSTS/SMSTS, induction settings and RAMS review
-- cycle. Scoped to the inviting main contractor (R15). Access period + RAMS cycle captured, not yet enforced.

CREATE TABLE IF NOT EXISTS subcontractoronboardingconfigs
(
    id                     uuid          NOT NULL PRIMARY KEY,
    invitercompanyid       uuid          NOT NULL,
    subcontractorcompanyid uuid          NOT NULL,
    tradeinviteid          uuid          NOT NULL,
    accessperiodmonths     integer       NOT NULL,
    requireddocumentsjson  text          NOT NULL,
    ssstsrequired          boolean       NOT NULL,
    smstsrequired          boolean       NOT NULL,
    inductionvaliditydays  integer       NOT NULL,
    inductionpassmark      integer       NOT NULL,
    inductionattemptlimit  integer       NOT NULL,
    ramsreviewcyclemonths  integer       NULL,
    createdutc             timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_subcontractoronboardingconfigs_subcontractorcompanyid ON subcontractoronboardingconfigs (subcontractorcompanyid);
CREATE INDEX IF NOT EXISTS ix_subcontractoronboardingconfigs_tradeinviteid ON subcontractoronboardingconfigs (tradeinviteid);
CREATE INDEX IF NOT EXISTS ix_subcontractoronboardingconfigs_invitercompanyid ON subcontractoronboardingconfigs (invitercompanyid);
