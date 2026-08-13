-- Forms Library: recurring-reminder idempotency marker on form assignments (PRD-Phase 2, R12) — SQL Server. Idempotent.
-- LastReminderUtc records when the recurring-reminder job last emailed a "form due" reminder, so a cadence is
-- reminded at most once per period.

IF COL_LENGTH(N'dbo.FormAssignments', N'LastReminderUtc') IS NULL
ALTER TABLE dbo.FormAssignments ADD LastReminderUtc DATETIMEOFFSET NULL;
