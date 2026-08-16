-- Self-service profile fields on the console user: mobile number + avatar image reference (SF-20). PostgreSQL. Idempotent.

ALTER TABLE Users ADD COLUMN IF NOT EXISTS Mobile VARCHAR(32) NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS AvatarImageReference TEXT NULL;
