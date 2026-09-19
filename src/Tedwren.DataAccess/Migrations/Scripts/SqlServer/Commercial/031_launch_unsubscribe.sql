-- Launch-list unsubscribe (PECR/GDPR): opt-out flag + one-click token — SQL Server, commercial database.
-- Idempotent ALTER of the existing LaunchSignups table.

IF COL_LENGTH(N'dbo.LaunchSignups', N'Unsubscribed') IS NULL
ALTER TABLE dbo.LaunchSignups ADD Unsubscribed BIT NOT NULL CONSTRAINT DF_LaunchSignups_Unsubscribed DEFAULT 0;

IF COL_LENGTH(N'dbo.LaunchSignups', N'UnsubscribeToken') IS NULL
ALTER TABLE dbo.LaunchSignups ADD UnsubscribeToken NVARCHAR(64) NULL;

-- The unsubscribe token is the lookup key for the one-click link; unique where present. Created via EXEC so it
-- is compiled after the ALTER above has added the column (single-batch runner, no GO support — see 023). Without
-- this, SQL Server compiles the whole batch up front and fails with "Invalid column name 'UnsubscribeToken'".
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_LaunchSignups_Unsub')
EXEC(N'CREATE UNIQUE INDEX UX_LaunchSignups_Unsub ON dbo.LaunchSignups (UnsubscribeToken) WHERE UnsubscribeToken IS NOT NULL;');
