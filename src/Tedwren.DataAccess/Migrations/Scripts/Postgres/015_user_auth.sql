-- Console user authentication: password hash + one-time invite token (D1) — PostgreSQL. Idempotent.

ALTER TABLE Users ADD COLUMN IF NOT EXISTS PasswordHash VARCHAR(512) NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS PasswordSetUtc TIMESTAMPTZ NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS InviteToken VARCHAR(128) NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS InviteTokenExpiresUtc TIMESTAMPTZ NULL;

CREATE INDEX IF NOT EXISTS IX_Users_InviteToken ON Users (InviteToken);
