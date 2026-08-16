-- Launch-list unsubscribe (PECR/GDPR): opt-out flag + one-click token — PostgreSQL, commercial database.
-- Idempotent ALTER of the existing launchsignups table. Lowercase identifiers for the shared repository SQL.

ALTER TABLE launchsignups ADD COLUMN IF NOT EXISTS unsubscribed boolean NOT NULL DEFAULT false;
ALTER TABLE launchsignups ADD COLUMN IF NOT EXISTS unsubscribetoken varchar(64) NULL;

-- The unsubscribe token is the lookup key for the one-click link; unique where present.
CREATE UNIQUE INDEX IF NOT EXISTS ux_launchsignups_unsub ON launchsignups (unsubscribetoken) WHERE unsubscribetoken IS NOT NULL;
