-- GoCardless webhook events (Phase C): stored inbound events, deduped by GoCardless event id — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS webhookevents
(
    id                uuid          NOT NULL PRIMARY KEY,
    gocardlesseventid varchar(64)   NOT NULL,
    resourcetype      varchar(64)   NOT NULL,
    action            varchar(64)   NOT NULL,
    resourceid        varchar(64)   NULL,
    outcome           integer       NOT NULL,
    detail            varchar(500)  NULL,
    rawjson           text          NOT NULL,
    receivedutc       timestamptz   NOT NULL,
    processedutc      timestamptz   NULL
);

-- Dedupe: one stored row per GoCardless event id.
CREATE UNIQUE INDEX IF NOT EXISTS ux_webhookevents_eventid ON webhookevents (gocardlesseventid);
CREATE INDEX IF NOT EXISTS ix_webhookevents_received ON webhookevents (receivedutc);
